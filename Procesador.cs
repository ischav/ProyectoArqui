using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.IO;

namespace ProcesadorMIPS
{
    /*Esta clase contiene los datos que son compartidos por ambos nucleos
     * Los datos independientos como lo son las caché L1 son controladas por cada núcleo
     */
    class Procesador
    {
        //variables que pertenecen a procesador
        public int aumento_reloj;
        public BloqueDatos[] memoria_datos;
        public CacheDatos cache_L2;
        public BloqueInstrucciones[] memoria_instrucciones;

        Reloj reloj;
        int  quantum, cantidad_hilillos;
        
        /* Estados de los bloques en la caché
         * - Inválido
         * - Compartido
         * - Modificado
         */
        const int INVALIDO = -1;
        const int COMPARTIDO = 0;
        const int MODIFICADO = 1;
        const bool DEBUG = false;

        Nucleo []nucleos; //contiene ambos nucleos de la maquina
        Queue<Hilillo> cola_hilillos; //cola de donde los nucleos obtienen hilillos para ejecutar
        Queue<Hilillo>  cola_hilillos_finalizados;

        //Barreras de la simulación
        public static Barrier barrera_inicio_instruccion;
        public static Barrier barrera_fin_instruccion;
        public static Barrier barrera_inicio_aumento_reloj;
        public static Barrier barrera_fin_aumento_reloj;

        //variables que pertenecen al nucleo
        public CacheDatos[] cache_L1_datos;
        public CacheInstrucciones[] cache_L1_instr;

        /*
         * Contructor de la clase
         * Inicializa las variables
        */
        public Procesador()
        {
            reloj = new Reloj();
            aumento_reloj = -1;
            quantum = 0;

            /* La memoria principal se define como dos vectores
             * - Memoria de instrucciones = Vector de 40 bloques de instrucciones,
             *   donde cada posición del vector almacena un vector correspondiente
             *  al bloque.
             * - Memoria de datos = Vector de 24 bloques, donde cada bloque contiene
             *   4 palabras.
             */
            memoria_instrucciones = new BloqueInstrucciones[40];
            memoria_datos = new BloqueDatos[24];

            /* Caché Nivel 2 Compartida
             * Contiene 8 bloques
             */
            cache_L2 = new CacheDatos(8);
            
            // Se crean los nucleos y sus caches
            nucleos = new Nucleo[2];
            cache_L1_datos = new CacheDatos[2];
            cache_L1_instr = new CacheInstrucciones[2];

            // Se inicializa la cola donde serán almacenados los hilillos
            cola_hilillos = new Queue<Hilillo>();
            cola_hilillos_finalizados = new Queue<Hilillo>();

            /* Barreras que controlan el quantum, ya que una instrucción 
             * equivale a un elemento del quantum
             */
            barrera_inicio_instruccion = new Barrier(participantCount: 2);
            barrera_fin_instruccion = new Barrier(participantCount: 2);
            barrera_inicio_aumento_reloj = new Barrier(participantCount: 2);
            barrera_fin_aumento_reloj = new Barrier(participantCount: 2);
        }

        /*
         * Metodos para obtener los núcleos luego de la ejecución
        */
        public Nucleo[] obtenerNucleos()
        {
            return nucleos;
        }

        /*
         * Indica la cantidad total de hilillos que corren en la simulacion
        */
        public void asignarNumeroHilillos(int num_hilillos)
        {
            this.cantidad_hilillos = num_hilillos;
        }
        
        public void asignarQuantum(int q)
        {
            this.quantum = q;
        }
        

        /*
         * Método que inicializa la memoria principal(datos e instrucciones) en 1.
        */
        public void IniciarMemoria() {
            
            for (int i = 0; i < 40; i++) {
                memoria_instrucciones[i] = new BloqueInstrucciones();
                // Por ahora está en cero para probar hilillos "Simples"
                // Se inicializa la memoria de instrucciones en 1. TODO
            }
            
            for (int i = 0; i < 24; i++){
                memoria_datos[i] = new BloqueDatos();
                //Se inicializa la memoria de datos en 1. TODO
            }
        }


        /*
         * Metodo para inicializar las memorias caché
         */
        public void IniciarNucleos()
        {
            for (int i = 0; i < 2; i++)
            {
                //se inicializan los núcleos
                nucleos[i] = new Nucleo();
                cache_L1_datos[i] = new CacheDatos(4);
                cache_L1_instr[i] = new CacheInstrucciones();
            }
        }

        /*
         * Método que carga las instrucciones de los hilillos en memoria de instrucciones
         * Recibe por parametro un vector con los nombres de los archivos que contienen las instrucciones
        */
        public void CargarInstrucciones(String[] rutas)
        {
            string line;
            String[] instruccion_string;
            int[] instruccion = new int[4];
            int bloque, palabra;
            int dir_memoria = 0;
            int inicio = 0;
            int fin = 0;
            System.IO.StreamReader file;
            for (int i = 0; i < rutas.Length; i++) {
                //Contexto[i, 32] = counter;
                file = new System.IO.StreamReader(rutas[i]); // accede al texto.
                inicio = dir_memoria;
                while ((line = file.ReadLine()) != null)
                {
                    bloque = dir_memoria / 16; //número de bloque.
                    palabra = (dir_memoria % 16) / 4; //numero de palabra.
                    instruccion_string = line.Split(' '); //Separa la instruccion en los 4 números

                    for (int j = 0; j < 4; j++) {
                        instruccion[j] = Convert.ToInt32(instruccion_string[j]); //asigna el numero en el vector.
                    }
                    memoria_instrucciones[bloque].setInstruccion(instruccion,palabra);
                    dir_memoria += 4; // suma el contador para el PC.
                }
                fin = dir_memoria;
                crearHilillo(i,inicio,fin);
            }
        }

        /*Metodo que crea un hilillo
         * le asigna el espacio de direcciones de las instrucciones (inicio, fin)
         * Se inserta en la cola de hilillos a la espera de ser procesados
        */
        public void crearHilillo(int id,int inicio, int fin)
        {
            Hilillo hilillo = new Hilillo(id);
            hilillo.asignarInicioHilillo(inicio);
            hilillo.asignarFinHilillo(fin);
            hilillo.asignarPC(inicio);
            cola_hilillos.Enqueue(hilillo);
        }


        /*
         * Retorna verdadero si se pudo desencolar un hilillo
         * En caso de retornar verdero, el nucleo contiene los datos del nuevo hilillo
        */ 
        public bool desencolarHilillo(int id_nucleo)
        {
            bool pudo_desencolar = false;
            while (true)
            {
                if (Monitor.TryEnter(cola_hilillos))
                {
                    //sección critica
                    if (cola_hilillos.Count > 0)
                    {
                        Hilillo hilo_desencolado = cola_hilillos.Dequeue();
                        int[] contexto_hilo_desencolado = hilo_desencolado.obtenerContexto();
                        nucleos[id_nucleo].asignarContexto(contexto_hilo_desencolado);
                        nucleos[id_nucleo].setFinalizado(false);
                        nucleos[id_nucleo].asignarIdentificadorHilillo(hilo_desencolado.obtenerIdentificadorHilillo());
                        pudo_desencolar = true;
                    }
                    Monitor.Exit(cola_hilillos);
                    return pudo_desencolar;
                }
            }
        }

        /*
         * Encola en la cola de hilillos
         * 
        */
        public void encolarHilillo(int id_nucleo)
        {
            int []registros=nucleos[id_nucleo].obtenerRegistros();
            Hilillo nuevo_hilillo = new Hilillo(nucleos[id_nucleo].obtenerIdentificadorHilillo());
            nuevo_hilillo.asignarContexto(registros);
            bool encolado = false;
            while (!encolado)
            {
                if (Monitor.TryEnter(cola_hilillos))
                {
                    //sección critica
                    cola_hilillos.Enqueue(nuevo_hilillo);
                    Monitor.Exit(cola_hilillos);
                    encolado = true;
                }
            }
        }

        /* Metodo principal de la simulación 
         * Una vez todo cargado en el procesador inicia la ejecución
         * recibe por parámetro el número de nucleo que se está llamando
        */
        public void inicializar(object nucleo)
        {
            
            int id_nucleo = Convert.ToInt32(nucleo);
            Console.WriteLine("Iniciando el núcleo: "+ Convert.ToString(id_nucleo));
            /*
             * while:Mientras no termine el quantum o no haya finalizado 
             * obtiene el PC
             * carga instrucción
             * ejecuta instrucción
             * fin ciclo while
             * Si el hilillo en el nucleo (no ha finalizado) entonces encolar.
             * vuelve al inicio
            */
            while (desencolarHilillo(id_nucleo)) //mientras existan hilillos para desencolar
            {
                imprimirMensaje("INICIO DESENCOLADO", id_nucleo);
                int current_quantum = 0;
                //mientras no se le termine el quantum o no haya completado todas las instrucciones->continuar
                while (current_quantum < quantum && nucleos[id_nucleo].getFinalizado() == false)
                {
                    imprimirMensaje("1 Main Entrando en ciclo quantum o no finalizado",id_nucleo);
                    int [] instruccion = this.obtener_instruccion(id_nucleo);
                    imprimirMensaje("2 Main Posterior a obtener la instrucción ", id_nucleo);
                    //se espera que ambos esten listos para ejecutar la instrucción
                    imprimirMensaje("3 Main Previo a barrera_inicio_instruccion ", id_nucleo);
                    barrera_inicio_instruccion.SignalAndWait();
                    imprimirMensaje("4 Main Posterior a barrera_inicio_instruccion, entrando en obtener instrucción ", id_nucleo);
                    //ejeución de la instrucción
                    this.ejecutarInstruccion(id_nucleo, instruccion[0], instruccion[1], instruccion[2], instruccion[3]);
                    imprimirMensaje("5 Main Posterior a obtener instrucción, aumentando el reloj ", id_nucleo);
                    //aumento del reloj para la instrucción
                    aumentarReloj(id_nucleo);
                    imprimirMensaje("6 Main Posterior a aumentar el reloj, entrando en barrera barrera_fin_instruccion", id_nucleo);
                    //se espera que ambos lleguen al final de la instrucción
                    barrera_fin_instruccion.SignalAndWait();
                    imprimirMensaje("7 Main Posterior a barrera barrera_fin_instruccion, reiniciando ciclo", id_nucleo);
                    imprimirMensaje("8 Main instrucción numero", current_quantum);
                    current_quantum++;
                }
                //si no es finalizado entonces copiar contexto y encole.
                if (nucleos[id_nucleo].getFinalizado() == false)
                {
                    //Aquí copiar contexto y encolar
                    encolarHilillo(id_nucleo);
                    imprimirMensaje("FIN DE QUANTUM ", id_nucleo);
                }
            }
            //Remove participant
            barrera_inicio_instruccion.RemoveParticipant();
            barrera_fin_instruccion.RemoveParticipant();
            barrera_inicio_aumento_reloj.RemoveParticipant();
            barrera_fin_aumento_reloj.RemoveParticipant();
        }

        public int[] obtener_instruccion(int id_nucleo)
        {
            imprimirMensaje("1 obtener_instruccion INICIO",id_nucleo);
            int pc = nucleos[id_nucleo].obtenerPc();
            int num_bloque = pc / 16;
            int num_palabra = (pc % 16) / 4;
            int ind_cache = num_bloque % 4;
            int[] instruccion = new int[4];
            bool hit = cache_L1_instr[id_nucleo].hit(num_bloque, ind_cache);
            if (hit)//significa que el bloque ya está en caché
            {
                imprimirMensaje("2 obtener_instruccion HIT inicio", id_nucleo);
                Instruccion inst_temp = cache_L1_instr[id_nucleo].getInstruccion(num_palabra,ind_cache);
                for (int i = 0; i < 4; i++)
                    instruccion[i] = inst_temp.getParteInstruccion(i);
                imprimirMensaje("3 obtener_instruccion HIT fin", id_nucleo);

            }
            else//el bloque no está en caché, hay que subirlo desde memoria. 40 ciclos. 
            {
                imprimirMensaje("4 obtener_instruccion MISS inicio", id_nucleo);
                BloqueInstrucciones temp_bloque_mem = memoria_instrucciones[num_bloque];
                cache_L1_instr[id_nucleo].setBloque(temp_bloque_mem,num_bloque,ind_cache);
                for (int i = 0; i < 4; i++)
                    instruccion[i] = temp_bloque_mem.getInstruccion(num_palabra).getParteInstruccion(i);

                imprimirMensaje("5 obtener_instruccion aumento reloj inicio", id_nucleo);
                for (int i = 0; i < 40; i++)
                {
                    imprimirMensaje("6 obtener_instruccion Previo a barrera_inicio_instruccion ", id_nucleo);
                    barrera_inicio_instruccion.SignalAndWait();
                    aumentarReloj(id_nucleo);
                    imprimirMensaje("7 obtener_instruccion posterior a barrera_fin_instruccion ", id_nucleo);
                    barrera_fin_instruccion.SignalAndWait();
                }
                imprimirMensaje("8 obtener_instruccion aumento reloj fin", id_nucleo);
                //aquí hay que simular el aumento de reloj
                imprimirMensaje("9 obtener_instruccion MISS fin", id_nucleo);

            }

            return instruccion;
        }




            // Método "Ejecutarse" en el diseño
        public void ejecutarInstruccion(int id_nucleo, int codigo_operacion, int op1, int op2, int op3) {
            nucleos[id_nucleo].aumentarPc();
            switch (codigo_operacion)
            {
                /* DADDI
                 * Si(op2 == 0):
                 *  “El registro 0 es inválido como destino”
                 *  R[op2] = R[op1] + op3
                 */
                case 8:
                    //ejecución
                    if (op2 == 0)
                    {
                        Console.WriteLine("El registro 0 es inválido como destino.");
                    }
                    else
                    {
                        nucleos[id_nucleo].asignarRegistro(nucleos[id_nucleo].obtenerRegistro(op1) + op3, op2);

                    }
                    break;
                /* DADD 
                 * Si(op2 == 0): 
                 *  “El registro 0 es inválido como destino”
                 *  R[op3] = R[op1] + R[op2]
                 */
                case 32:
                    if (op3 == 0)
                    {
                        Console.WriteLine("El registro 0 es inválido como destino.");
                    }
                    else
                    {
                        nucleos[id_nucleo].asignarRegistro(nucleos[id_nucleo].obtenerRegistro(op1) + nucleos[id_nucleo].obtenerRegistro(op2), op3);
                    }
                    break;
                /* DSUB
                 * Si(op2 == 0):
                 *  “El registro 0 es inválido como destino”
                 *  R[op3] = R[op1] - R[op2]
                 */
                case 34:
                    if (op3 == 0)
                    {
                        Console.WriteLine("El registro 0 es inválido como destino.");
                    }
                    else
                    {
                        nucleos[id_nucleo].asignarRegistro(nucleos[id_nucleo].obtenerRegistro(op1) - nucleos[id_nucleo].obtenerRegistro(op2), op3);
                    }
                    break;
                /* DMUL
                 * Si(op2 == 0):
                 *  “El registro 0 es inválido como destino”
                 *  R[op3] = R[op1] * R[op2]
                 */
                case 12:
                    if (op3 == 0)
                    {
                        Console.WriteLine("El registro 0 es inválido como destino.");
                    }
                    else
                    {
                        nucleos[id_nucleo].asignarRegistro(nucleos[id_nucleo].obtenerRegistro(op1) * nucleos[id_nucleo].obtenerRegistro(op2), op3);
                    }
                    break;
                /* DDIV
                 * Si(op2 == 0):
                 *  “El registro 0 es inválido.”
                 *  R[op3] = R[op1] / R[op2]
                 */
                case 14://se guarda un resultado entero?
                    if (op3 == 0)
                    {
                        Console.WriteLine("El registro 0 es inválido como destino.");
                    }
                    else
                    {
                        nucleos[id_nucleo].asignarRegistro(nucleos[id_nucleo].obtenerRegistro(op1) / nucleos[id_nucleo].obtenerRegistro(op2), op3);
                    }
                    break;
                /* BEQZ
                 * Si(R[op1] == 0):
                 *  PC += op3 * 4
                 */
                case 4:
                    if (nucleos[id_nucleo].obtenerRegistro(op1)==0)
                    {
                        nucleos[id_nucleo].asignarPc(nucleos[id_nucleo].obtenerPc()+(op3*4));
                    }
                    break;
                /* BNEZ
                 * Si(R[op1] != 0):
                 *  PC += op3 * 4
                 */
                case 5:
                    if (nucleos[id_nucleo].obtenerRegistro(op1) != 0)
                    {
                        nucleos[id_nucleo].asignarPc(nucleos[id_nucleo].obtenerPc() + (op3 * 4));
                    }
                    break;
                /* JAL
                 * R[31] = PC
                 * PC += op3
                 */
                case 3:
                    nucleos[id_nucleo].asignarRegistro(nucleos[id_nucleo].obtenerPc(), 31);
                    nucleos[id_nucleo].asignarPc(nucleos[id_nucleo].obtenerPc()+op3);
                    break;
                /* JR
                 * PC = R[op1];
                 */
                case 2:
                    nucleos[id_nucleo].asignarPc(nucleos[id_nucleo].obtenerRegistro(op1));
                    break;
                /* FIN */
                case 63:
                    nucleos[id_nucleo].setFinalizado(true);
                    int[] registros = nucleos[id_nucleo].obtenerRegistros();
                    Hilillo nuevo_hilillo = new Hilillo(nucleos[id_nucleo].obtenerIdentificadorHilillo());
                    nuevo_hilillo.asignarContexto(registros);
                    bool encolado = false;
                    while (!encolado)
                    {
                        if (Monitor.TryEnter(cola_hilillos_finalizados))
                        {
                            //sección critica
                            cola_hilillos_finalizados.Enqueue(nuevo_hilillo);
                            Monitor.Exit(cola_hilillos_finalizados);
                            encolado = true;
                        }
                    }
                    Console.WriteLine("FINALIZADO "+id_nucleo);
                    //nucleos[id_nucleo].setFinalizado(true);
                    //barrera_inicio_instruccion.RemoveParticipant();
                    //barrera_fin_instruccion.RemoveParticipant();
                    //barrera_inicio_aumento_reloj.RemoveParticipant();
                    //barrera_fin_aumento_reloj.RemoveParticipant();
                    break;
                default:
                    break;
            }
        }

        /*
 * Aumenta los ciclos del reloj; esperando a que el nucleo llegue a este punto
 * Solamente uno aumenta y libera
*/
        public void aumentarReloj(int id_nucleo)
        {
            barrera_inicio_aumento_reloj.SignalAndWait();
            if (Monitor.TryEnter(reloj))
            {
                aumento_reloj = id_nucleo;
                reloj.aumentarReloj();
            }
            barrera_fin_aumento_reloj.SignalAndWait();
            if (aumento_reloj == id_nucleo)
            {
                aumento_reloj = -1;
                Monitor.Exit(reloj);
            }
        }

        public void imprimirMensaje(String mensaje, int nucleo_id)
        {
            if (DEBUG)
            {
                Console.WriteLine(nucleo_id + ": " + mensaje);
            }
        }

        /* ------------------------------------------------------------------------------------------
         *                               Métodos para imprimir 
         *             Los archivos generados se encuentra en ...\ProyectoArqui\bin\Debug
         * ------------------------------------------------------------------------------------------ */

        public void imprimirRegistros()
        {
            string fileName = Path.GetFullPath(Directory.GetCurrentDirectory() + @"\Nucleos.txt");
            Console.WriteLine(fileName);
            using (StreamWriter archivo = new StreamWriter(fileName))
            {
                {
                    for (int i = 0; i < 2; i++)
                    {
                        archivo.WriteLine("\n================== Estado del Núcleo " + i + " ==================\n");
                        archivo.WriteLine("Contador de programa: " + nucleos[i].obtenerPc());
                        archivo.WriteLine("Estado de los registros");
                        for (int k = 0; k < 32; k++)
                        {
                            archivo.WriteLine("R[" + k + "] : " + nucleos[i].obtenerRegistro(k));
                        }
                    }
                }
            }

        }

        public void imprimirColaHilillos()
        {
            string fileName = Path.GetFullPath(Directory.GetCurrentDirectory() + @"\ColaHilillos.txt");
            Console.WriteLine(fileName);
            using (StreamWriter archivo = new StreamWriter(fileName))
            {
                Queue<Hilillo> cola_aux = new Queue<Hilillo>(cola_hilillos);
                Hilillo aux = null;

                while (cola_aux.Count > 0)
                {
                    aux = cola_aux.Dequeue();
                    archivo.WriteLine("Numero hilillo: " + aux.obtenerIdentificadorHilillo());
                    Console.WriteLine("Numero hilillo: " + aux.obtenerIdentificadorHilillo());
                    archivo.WriteLine("El hilillo inicia en la dirección: M[" + aux.obtenerInicioHilillo() + "]");
                    archivo.WriteLine("El hilillo finaliza en la dirección: M[" + aux.obtenerFinHilillo() + "]");
                    archivo.WriteLine("Finalizado: " + aux.obtenerFinalizado());
                    archivo.WriteLine("Program Counter: " + aux.obtenerPC() + "\n");

                    for (int i = 0; i < 32; i++)
                        archivo.WriteLine("R[" + i + "] = " + aux.obtenerRegistros()[i] + "\n");
                }
            }
        }

        public void imprimirColaHilillosFinalizados()
        {
            Console.WriteLine("Entró aquí");
            string fileName = Path.GetFullPath(Directory.GetCurrentDirectory() + @"\ColaHilillosFinalizados.txt");
            Console.WriteLine(fileName);
            using (StreamWriter archivo = new StreamWriter(fileName))
            {
                Queue<Hilillo> cola_aux = new Queue<Hilillo>(cola_hilillos_finalizados);
                Hilillo aux = null;
                archivo.WriteLine("Esto lo escribe...");
                while (cola_aux.Count > 0)
                {
                    aux = cola_aux.Dequeue();
                    archivo.WriteLine("Numero hilillo: " + aux.obtenerIdentificadorHilillo());
                    archivo.WriteLine("El hilillo inicia en la dirección: M[" + aux.obtenerInicioHilillo() + "]");
                    archivo.WriteLine("El hilillo finaliza en la dirección: M[" + aux.obtenerFinHilillo() + "]");
                    archivo.WriteLine("Finalizado: " + aux.obtenerFinalizado());
                    archivo.WriteLine("Program Counter: " + aux.obtenerPC() + "\n");
                    archivo.WriteLine("Ciclos de reloj: " + aux.obtenerCiclosReloj() + "\n");

                    for (int i = 0; i < 32; i++)
                        archivo.WriteLine("R[" + i + "] = " + aux.obtenerRegistros()[i] + "\n");
                }
            }
        }

        public void imprimirMemoriaInstrucciones()
        {

        }

        public void imprimirMemoriaDatos()
        {

        }

    }
}
