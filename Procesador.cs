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
        public bool modo;

        Reloj reloj;
        int quantum, cantidad_hilillos;

        /* Estados de los bloques en la caché
         * - Inválido
         * - Compartido
         * - Modificado
         */
        const int INVALIDO = -1;
        const int COMPARTIDO = 0;
        const int MODIFICADO = 1;
        const bool DEBUG = false;

        Nucleo[] nucleos; //contiene ambos nucleos de la maquina
        Queue<Hilillo> cola_hilillos; //cola de donde los nucleos obtienen hilillos para ejecutar
        Queue<Hilillo> cola_hilillos_finalizados;

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

            modo = false;



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

        public void asignarModo(bool nuevo_modo) {
            this.modo = nuevo_modo;
        }


        /*
         * Método que inicializa la memoria principal(datos e instrucciones) en 1.
        */
        public void IniciarMemoria()
        {

            for (int i = 0; i < 40; i++)
            {
                memoria_instrucciones[i] = new BloqueInstrucciones();
                // Por ahora está en cero para probar hilillos "Simples"
                // Se inicializa la memoria de instrucciones en 1. TODO
            }

            for (int i = 0; i < 24; i++)
            {
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
            for (int i = 0; i < rutas.Length; i++)
            {
                //Contexto[i, 32] = counter;
                file = new System.IO.StreamReader(rutas[i]); // accede al texto.
                inicio = dir_memoria;
                while ((line = file.ReadLine()) != null)
                {
                    bloque = dir_memoria / 16; //número de bloque.
                    palabra = (dir_memoria % 16) / 4; //numero de palabra.
                    instruccion_string = line.Split(' '); //Separa la instruccion en los 4 números

                    for (int j = 0; j < 4; j++)
                    {
                        instruccion[j] = Convert.ToInt32(instruccion_string[j]); //asigna el numero en el vector.
                    }
                    memoria_instrucciones[bloque].setInstruccion(instruccion, palabra);
                    dir_memoria += 4; // suma el contador para el PC.
                }
                fin = dir_memoria;
                String[] ruta_hilillo = rutas[i].Split('\\');
                String name_hilillo = ruta_hilillo[ruta_hilillo.Length - 1];
                crearHilillo(name_hilillo, inicio, fin);
            }
        }

        /*Metodo que crea un hilillo
         * le asigna el espacio de direcciones de las instrucciones (inicio, fin)
         * Se inserta en la cola de hilillos a la espera de ser procesados
        */
        public void crearHilillo(String id, int inicio, int fin)
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
                        Console.WriteLine("Hilo en ejecución: " + hilo_desencolado.obtenerIdentificadorHilillo() + ". En el nucleo: " + id_nucleo);
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
            int[] registros = nucleos[id_nucleo].obtenerRegistros();
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
            Console.WriteLine("Iniciando el núcleo: " + Convert.ToString(id_nucleo));
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
                //imprimirMemorias();
                imprimirMensaje("INICIO DESENCOLADO", id_nucleo);
                int current_quantum = 0;
                //mientras no se le termine el quantum o no haya completado todas las instrucciones->continuar
                while (current_quantum < quantum && nucleos[id_nucleo].getFinalizado() == false)
                {
                    imprimirMensaje("1 Main Entrando en ciclo quantum o no finalizado", id_nucleo);
                    int[] instruccion = this.obtener_instruccion(id_nucleo);
                    imprimirMensaje("2 Main Posterior a obtener la instrucción ", id_nucleo);
                    //se espera que ambos esten listos para ejecutar la instrucción
                    imprimirMensaje("3 Main Previo a barrera_inicio_instruccion ", id_nucleo);
                    //barrera_inicio_instruccion.SignalAndWait();
                    imprimirMensaje("4 Main Posterior a barrera_inicio_instruccion, entrando en obtener instrucción ", id_nucleo);
                    //ejeución de la instrucción
                    this.ejecutarInstruccion(id_nucleo, instruccion[0], instruccion[1], instruccion[2], instruccion[3]);
                    imprimirMensaje("5 Main Posterior a obtener instrucción, aumentando el reloj ", id_nucleo);
                    //aumento del reloj para la instrucción
                    //aumentarReloj(id_nucleo);
                    aumentarReloj(id_nucleo);
                    imprimirMensaje("6 Main Posterior a aumentar el reloj, entrando en barrera barrera_fin_instruccion", id_nucleo);
                    //se espera que ambos lleguen al final de la instrucción
                    //barrera_fin_instruccion.SignalAndWait();
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
            imprimirMensaje("1 obtener_instruccion INICIO", id_nucleo);
            int pc = nucleos[id_nucleo].obtenerPc();
            int num_bloque = pc / 16;
            int num_palabra = (pc % 16) / 4;
            int ind_cache = num_bloque % 4;
            int[] instruccion = new int[4];
            bool hit = cache_L1_instr[id_nucleo].hit(num_bloque, ind_cache);
            if (hit)//significa que el bloque ya está en caché
            {
                imprimirMensaje("2 obtener_instruccion HIT inicio", id_nucleo);
                Instruccion inst_temp = cache_L1_instr[id_nucleo].getInstruccion(num_palabra, ind_cache);
                for (int i = 0; i < 4; i++)
                    instruccion[i] = inst_temp.getParteInstruccion(i);
                imprimirMensaje("3 obtener_instruccion HIT fin", id_nucleo);

            }
            else//el bloque no está en caché, hay que subirlo desde memoria. 40 ciclos. 
            {
                imprimirMensaje("4 obtener_instruccion MISS inicio", id_nucleo);
                BloqueInstrucciones temp_bloque_mem = memoria_instrucciones[num_bloque];
                cache_L1_instr[id_nucleo].setBloque(temp_bloque_mem, num_bloque, ind_cache);
                for (int i = 0; i < 4; i++)
                    instruccion[i] = temp_bloque_mem.getInstruccion(num_palabra).getParteInstruccion(i);

                imprimirMensaje("5 obtener_instruccion aumento reloj inicio", id_nucleo);
                for (int i = 0; i < 40; i++)
                {
                    imprimirMensaje("6 obtener_instruccion Previo a barrera_inicio_instruccion ", id_nucleo);
                    //barrera_inicio_instruccion.SignalAndWait();
                    //aumentarReloj(id_nucleo);
                    imprimirMensaje("7 obtener_instruccion posterior a barrera_fin_instruccion ", id_nucleo);
                    aumentarReloj(id_nucleo);
                    //barrera_fin_instruccion.SignalAndWait();
                }
                imprimirMensaje("8 obtener_instruccion aumento reloj fin", id_nucleo);
                //aquí hay que simular el aumento de reloj
                imprimirMensaje("9 obtener_instruccion MISS fin", id_nucleo);

            }

            return instruccion;
        }




        // Método "Ejecutarse" en el diseño
        public void ejecutarInstruccion(int id_nucleo, int codigo_operacion, int op1, int op2, int op3)
        {
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
                /* LW
                * 
                * 
                *                       
                */
                case 35:

                    //imprimirMemorias();
                    operacion_LW(id_nucleo, op1, op2, op3);
                    //imprimirMemorias();
                    break;

                /* SW
                * 
                * 
                *                       
                */
                case 43:
                    //imprimirMemorias();
                    operacion_SW(id_nucleo, op1, op2, op3);
                    //imprimirMemorias();
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
                    if (nucleos[id_nucleo].obtenerRegistro(op1) == 0)
                    {
                        nucleos[id_nucleo].asignarPc(nucleos[id_nucleo].obtenerPc() + (op3 * 4));
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
                    nucleos[id_nucleo].asignarPc(nucleos[id_nucleo].obtenerPc() + op3);
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
                    nuevo_hilillo.asignarFinalizado();
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
                    Console.WriteLine("FINALIZADO " + id_nucleo);
                    break;
                default:
                    break;
            }
        }

        public void operacion_SW(int id_nucleo, int reg_fuente, int reg_a_guardar, int inmediato)
        {
            bool guardado = false;
            int direccion = nucleos[id_nucleo].obtenerRegistro(reg_fuente) + inmediato; //Obtiene la dirección de memoria a cargar.
            int num_bloque = direccion / 16; //numero de bloque
            int num_palabra = (direccion % 16) / 4; // numero de palabra
            int ind_cache = num_bloque % 4; //indice de cache para la chaché L1 de datos.
            int valor_registro_guardar = nucleos[id_nucleo].obtenerRegistro(reg_a_guardar);

            //Console.WriteLine("SW Nucleo: " + id_nucleo + " va a escribir el valor " + valor_registro_guardar + "en la palabra " + num_palabra + " del bloque " + num_bloque + " de la direccion " + direccion);

            while (!guardado)
            {
                //Console.WriteLine("Antes de bloquear mi caché L1 de datos. Nucleo: " + id_nucleo);
                if (Monitor.TryEnter(cache_L1_datos[id_nucleo])) //intento bloquear caché
                {
                    //Console.WriteLine("Tengo mi caché L1 de datos. Nucleo: " + id_nucleo);
                    if (cache_L1_datos[id_nucleo].hit(num_bloque, ind_cache)) //Si el bloque está en cache y está modificado o compartido, puedo escribir.
                    {
                        if (cache_L1_datos[id_nucleo].getEstado(ind_cache) == MODIFICADO)
                        { //Está modificado, puedo escribir en el.
                            //Console.WriteLine("Lo tengo y está modificado");
                            cache_L1_datos[id_nucleo].escribirPalabra(ind_cache, valor_registro_guardar, num_palabra); //Escribo la palabra en el bloque.
                            Monitor.Exit(cache_L1_datos[id_nucleo]); //Libero mi caché.
                            guardado = true; // Pongo guardado en true para que no entre al while y termine la instrucción.
                        }
                        else
                        { //Está compartido.
                            //Console.WriteLine("Lo tengo y está compartido.");
                            //Console.WriteLine("Antes de bloquear caché L2 de datos. Nucleo: " + id_nucleo);
                            if (Monitor.TryEnter(cache_L2))
                            { //Pido candado para Cache L2, lo que significa que obtengo el bus y puedo trbajar con Cache L2 y memoria.
                                //Console.WriteLine("Tengo caché L2 de datos. Nucleo: " + id_nucleo);
                                int otro_nucleo = id_nucleo == 1 ? 0 : 1; //Para saber cual es el id del otro nucleo.
                                //Console.WriteLine("Antes de bloquear caché L1 de datos del otro nucleo. Nucleo: " + id_nucleo + ". Otro Nucleo: " + otro_nucleo);
                                if (Monitor.TryEnter(cache_L1_datos[otro_nucleo]))
                                { //Pido candado para la otra cache L1;
                                    //Console.WriteLine("Tengo caché L1 de datos del otro nucleo. Nucleo: " + id_nucleo + ". Otro Nucleo: " + otro_nucleo);
                                    if (cache_L1_datos[otro_nucleo].getNumBloque(ind_cache) == num_bloque && cache_L1_datos[otro_nucleo].getEstado(ind_cache) == COMPARTIDO) //Si está el bloque y está Compartido.
                                    {
                                        //Console.WriteLine("La otra cache lo tiene y está compartido");
                                        cache_L1_datos[otro_nucleo].setEstado(ind_cache, INVALIDO); //Invalido el bloque.
                                    }
                                    if (nucleos[otro_nucleo].obtenerRL() == num_bloque)
                                    { //Si el RL es igual a mi direccion de memoria entonces se pone en -1
                                        nucleos[otro_nucleo].asignarRL(INVALIDO);
                                    }
                                    //Console.WriteLine("Antes de liberar caché L1 de datos del otro nucleo. Nucleo: " + id_nucleo + ". Otro Nucleo: " + otro_nucleo);
                                    Monitor.Exit(cache_L1_datos[otro_nucleo]); //Libero la otra caché
                                    //Console.WriteLine("Despues de liberar caché L1 de datos del otro nucleo. Nucleo: " + id_nucleo + ". Otro Nucleo: " + otro_nucleo);
                                    int ind_cache_L2 = num_bloque % 8; //indice de cache para la chaché L2 de datos.
                                    if (cache_L2.getNumBloque(ind_cache_L2) == num_bloque && cache_L2.getEstado(ind_cache_L2) == COMPARTIDO)
                                    { //Si en L2 está el bloque en compartido se invalida.
                                        //Console.WriteLine("Cache L2 lo tiene compartido");
                                        cache_L2.setEstado(ind_cache_L2, INVALIDO); //Invalido el bloque.
                                    }
                                    //Console.WriteLine("Antes de liberar caché L2 de datos. Nucleo: " + id_nucleo);
                                    Monitor.Exit(cache_L2); //Libero la caché L2, osea, el Bus.
                                    //Console.WriteLine("Despues de liberar caché L2 de datos. Nucleo: " + id_nucleo);
                                    cache_L1_datos[id_nucleo].setEstado(ind_cache, MODIFICADO); //Cambio el bloque a Modificado en mi caché.
                                    //Console.WriteLine("Escribo el valor "+ valor_registro_guardar + " En la palabra "+num_palabra+ " en el bloque " + num_bloque);
                                    cache_L1_datos[id_nucleo].escribirPalabra(ind_cache, valor_registro_guardar, num_palabra); //Escribo la palabra en el bloque.
                                    //Console.WriteLine("Antes de liberar mi caché L1 de datos. Nucleo: " + id_nucleo);
                                    Monitor.Exit(cache_L1_datos[id_nucleo]); //Libero mi caché.
                                    //Console.WriteLine("Despues de liberar mi caché L1 de datos. Nucleo: " + id_nucleo);
                                    guardado = true; // Pongo guardado en true para que no entre al while y termine la instrucción.
                                }
                                else
                                {
                                    //Console.WriteLine("No puedo obtener la otra caché");
                                    //Console.WriteLine("Antes de liberar caché L2 de datos. Nucleo: " + id_nucleo);
                                    Monitor.Exit(cache_L2); //Libero la caché L2, osea, el Bus.
                                    //Console.WriteLine("Despues de liberar caché L2 de datos. Nucleo: " + id_nucleo);
                                    //Console.WriteLine("Antes de liberar mi caché L1 de datos. Nucleo: " + id_nucleo);
                                    Monitor.Exit(cache_L1_datos[id_nucleo]); //Libero mi caché.
                                    //Console.WriteLine("Despues de liberar mi caché L1 de datos. Nucleo: " + id_nucleo);
                                    aumentarReloj(id_nucleo); //aumento reloj
                                }
                            }
                            else
                            {
                                //Console.WriteLine("No puedo obtener el bus");
                                //Console.WriteLine("Antes de liberar mi caché L1 de datos. Nucleo: " + id_nucleo);
                                Monitor.Exit(cache_L1_datos[id_nucleo]); //Libero mi caché.
                                //Console.WriteLine("Despues de liberar mi caché L1 de datos. Nucleo: " + id_nucleo);
                                aumentarReloj(id_nucleo); //aumento reloj
                            }
                        }

                    }
                    else
                    { //Fallo
                        //Console.WriteLine("No tengo el bloque.");
                        int estado_bloque_a_caer = cache_L1_datos[id_nucleo].getEstado(ind_cache); //Obtengo el estado del bloque al que le voy a caer encima
                        //Console.WriteLine("Antes de bloquear caché L2 de datos. Nucleo: " + id_nucleo);
                        if (Monitor.TryEnter(cache_L2))
                        { //Pido candado para Cache L2, lo que significa que obtengo el bus y puedo trbajar con Cache L2 y memoria.
                            //Console.WriteLine("Tengo caché L2 de datos. Nucleo: " + id_nucleo);
                            if (estado_bloque_a_caer == MODIFICADO)
                            { // si está modificado hay que mandarlo a escribir a la siguiente estructura.
                                //Console.WriteLine("Al bloque al que le voy a caer está modificado");
                                BloqueDatos bloque_guardar = cache_L1_datos[id_nucleo].getBloque(ind_cache); //Obtengo el bloque que tengo que mandar a escribir.
                                int ind_bloque_guardar=cache_L1_datos[id_nucleo].getNumBloque(ind_cache);
                                cache_L1_datos[id_nucleo].setEstado(ind_cache, INVALIDO); //Invalido el bloque en cache L1.
                                //Se manda a escribir a L2 pero como L2 es No Write Allocate entonces se envia a escribir al siguiente nivel, osea, memoria.
                                memoria_datos[ind_bloque_guardar].setPalabras(bloque_guardar.getPalabras()); //Guardo el bloque en memoria.
                                for (int i = 0; i < 48; i++)
                                { //Se espera por los 8 tiempos que dura enviar a escribir a L2 y los 40 que dura en escribir en memoria.
                                    aumentarReloj(id_nucleo); //aumento reloj
                                }
                            }

                            int otro_nucleo = id_nucleo == 1 ? 0 : 1; //Para saber cual es el id del otro nucleo.
                            //Console.WriteLine("Antes de bloquear caché L1 de datos del otro nucleo. Nucleo: " + id_nucleo + ". Otro Nucleo: " + otro_nucleo);
                            if (Monitor.TryEnter(cache_L1_datos[otro_nucleo])) //Intento bloquear el otro nucleo
                            {
                                //Console.WriteLine("Tengo caché L1 de datos del otro nucleo. Nucleo: " + id_nucleo + ". Otro Nucleo: " + otro_nucleo);
                                if (cache_L1_datos[otro_nucleo].getNumBloque(ind_cache) == num_bloque && cache_L1_datos[otro_nucleo].getEstado(ind_cache) == MODIFICADO)
                                { //Está el que quiero modificado.
                                  //Console.WriteLine("La otra cache tiene el bloque que quiero y está modificado.");
                                    int ind_bloque_guardar = cache_L1_datos[otro_nucleo].getNumBloque(ind_cache);
                                    BloqueDatos bloque_guardar = cache_L1_datos[otro_nucleo].getBloque(ind_cache); //Obtengo el bloque que tengo que mandar a escribir.
                                    cache_L1_datos[otro_nucleo].setEstado(ind_cache, INVALIDO); //Invalido el bloque en la otra cache L1.
                                    //Se manda a escribir a L2 pero como L2 es No Write Allocate entonces se envia a escribir al siguiente nivel, osea, memoria.

                                    if (nucleos[otro_nucleo].obtenerRL() == num_bloque)
                                    { //Si el RL es igual a mi direccion de memoria entonces se pone en -1
                                        nucleos[otro_nucleo].asignarRL(INVALIDO);
                                    }

                                    for (int i = 0; i < 8; i++)
                                    { //Se espera por los 8 ciclos que dura enviar a escribir a L2.
                                        aumentarReloj(id_nucleo); //aumento reloj
                                    }

                                    //Console.WriteLine("Antes de liberar caché L1 de datos del otro nucleo. Nucleo: " + id_nucleo + ". Otro Nucleo: " + otro_nucleo);
                                    Monitor.Exit(cache_L1_datos[otro_nucleo]); //Libero la otra caché
                                    //Console.WriteLine("Despues de liberar caché L1 de datos del otro nucleo. Nucleo: " + id_nucleo + ". Otro Nucleo: " + otro_nucleo);
                                    memoria_datos[ind_bloque_guardar].setPalabras(bloque_guardar.getPalabras()); //Guardo el bloque en memoria.
                                    cache_L1_datos[id_nucleo].setBloque(bloque_guardar, num_bloque, ind_cache); //Escribo en mi cache el bloque.
                                    for (int i = 0; i < 40; i++)
                                    { //Se espera por los 40 ciclos que dura en escribir en memoria.
                                        aumentarReloj(id_nucleo); //aumento reloj
                                    }
                                }
                                else
                                {  //Está el que quiero compartido o no está.
                                    if (cache_L1_datos[otro_nucleo].getNumBloque(ind_cache) == num_bloque && cache_L1_datos[otro_nucleo].getEstado(ind_cache) == COMPARTIDO)
                                    {
                                        //Console.WriteLine("La otra caché tiene el bloque y está compartido");
                                        //Está el que quiero compartido
                                        cache_L1_datos[otro_nucleo].setEstado(ind_cache, INVALIDO);
                                    }
                                    if (nucleos[otro_nucleo].obtenerRL() == num_bloque)
                                    { //Si el RL es igual a mi direccion de memoria entonces se pone en -1
                                        nucleos[otro_nucleo].asignarRL(INVALIDO);
                                    }
                                    //Console.WriteLine("Antes de liberar caché L1 de datos del otro nucleo. Nucleo: " + id_nucleo + ". Otro Nucleo: " + otro_nucleo);
                                    Monitor.Exit(cache_L1_datos[otro_nucleo]); //Libero la otra caché
                                    //Console.WriteLine("Despues de liberar caché L1 de datos del otro nucleo. Nucleo: " + id_nucleo + ". Otro Nucleo: " + otro_nucleo);
                                    int ind_cache_L2 = num_bloque % 8; //indice de cache para la chaché L2 de datos.
                                    if (cache_L2.getNumBloque(ind_cache_L2) == num_bloque && cache_L2.getEstado(ind_cache_L2) == COMPARTIDO)
                                    { //Si en L2 está el bloque en compartido se invalida.
                                        //Console.WriteLine("La cache L2 lo tiene y está compartido");
                                        cache_L2.setEstado(ind_cache_L2, INVALIDO); //Invalido el bloque.
                                    }
                                    else
                                    {
                                        //Console.WriteLine("La cache L2 no lo tiene, tengo que subirla de memoria.");
                                        int[] bloque_memoria = memoria_datos[num_bloque].getPalabras(); //Obtengo el bloque de memoria.
                                        cache_L2.setBloqueEnteros(bloque_memoria, num_bloque, ind_cache_L2); //Asigno el bloque a cache L2.
                                        cache_L2.setEstado(ind_cache_L2, INVALIDO);
                                        //Console.WriteLine("La subí de memoria");
                                        for (int i = 0; i < 40; i++)
                                        { //Se espera por los 40 ciclos que dura en escribir desde memoria.
                                            //Console.WriteLine("Espero tiempo " + i);
                                            aumentarReloj(id_nucleo); //aumento reloj
                                        }
                                    }
                                    //Console.WriteLine("Subo el bloque a mi cache.");
                                    BloqueDatos bloque_cache_L2 = cache_L2.getBloque(ind_cache_L2);
                                    cache_L1_datos[id_nucleo].setBloque(bloque_cache_L2, num_bloque, ind_cache);
                                    for (int i = 0; i < 8; i++)
                                    { //Se espera por los 8 tiempos que dura enviar desde L2 a L1.
                                        aumentarReloj(id_nucleo); //aumento reloj
                                    }

                                }

                                //Console.WriteLine("Antes de liberar caché L2 de datos. Nucleo: " + id_nucleo);
                                Monitor.Exit(cache_L2); //Libero la caché L2, osea, el Bus.
                                //Console.WriteLine("Despues de liberar caché L2 de datos. Nucleo: " + id_nucleo);
                                cache_L1_datos[id_nucleo].setEstado(ind_cache, MODIFICADO); //Cambio el bloque a Modificado en mi caché.
                                //Console.WriteLine("Escribo el valor " + valor_registro_guardar + " En la palabra " + num_palabra + " en el bloque " + num_bloque);
                                cache_L1_datos[id_nucleo].escribirPalabra(ind_cache, valor_registro_guardar, num_palabra); //Escribo la palabra en el bloque.

                                //Console.WriteLine("Antes de liberar mi caché L1 de datos. Nucleo: " + id_nucleo);
                                Monitor.Exit(cache_L1_datos[id_nucleo]); //Libero mi caché.
                                //Console.WriteLine("Despues de liberar mi caché L1 de datos. Nucleo: " + id_nucleo);
                                guardado = true; // Pongo guardado en true para que no entre al while y termine la instrucción.

                            }
                            else
                            {
                                //Console.WriteLine("No puedo obtener la otra caché");
                                //Console.WriteLine("Antes de liberar caché L2 de datos. Nucleo: " + id_nucleo);
                                Monitor.Exit(cache_L2); //Libero la caché L2, osea, el Bus.
                                //Console.WriteLine("Despues de liberar caché L2 de datos. Nucleo: " + id_nucleo);
                                //Console.WriteLine("Antes de liberar mi caché L1 de datos. Nucleo: " + id_nucleo);
                                Monitor.Exit(cache_L1_datos[id_nucleo]); //Libero mi caché.
                                //Console.WriteLine("Despues de liberar mi caché L1 de datos. Nucleo: " + id_nucleo);
                                aumentarReloj(id_nucleo); //aumento reloj
                            }
                        }
                        else
                        {
                            //Console.WriteLine("No puedo obtener la caché L2");
                            //Console.WriteLine("Antes de liberar mi caché L1 de datos. Nucleo: " + id_nucleo);
                            Monitor.Exit(cache_L1_datos[id_nucleo]); //Libero mi caché.
                            //Console.WriteLine("Despues de liberar mi caché L1 de datos. Nucleo: " + id_nucleo);
                            aumentarReloj(id_nucleo); //aumento reloj
                        }
                    }
                }
                else
                {
                    //Console.WriteLine("No puedo obtener mi caché");
                    aumentarReloj(id_nucleo); //aumento reloj
                }
            }
            //aumento de reloj. Fin de la instrucción.



        }

        /**
         * Toma el dato del registro fuente lo suma con el inmediato y obtiene la direccion de memoria dada por esa suma.
         * saca la palabra y la guarda en el registro destino. 
         *          
        */
        public void operacion_LW(int id_nucleo, int reg_fuente, int reg_destino, int inmediato)
        {
            //Console.WriteLine("LOADDDDDDDDDDD");
            bool cargado = false;
            int direccion = nucleos[id_nucleo].obtenerRegistro(reg_fuente) + inmediato; //Obtiene la dirección de memoria a cargar.
            int num_bloque = direccion / 16; //numero de bloque
            int num_palabra = (direccion % 16) / 4; // numero de palabra
            int ind_cache = num_bloque % 4; //indice de cache para la chaché L1 de datos.




            while (!cargado)
            {
                if (Monitor.TryEnter(cache_L1_datos[id_nucleo])) //intento bloquear caché
                {
                    if (cache_L1_datos[id_nucleo].hit(num_bloque, ind_cache)) //Si el bloque está en cache y está modificado o compartido, puedo leerlo.
                    {
                        int resultado = cache_L1_datos[id_nucleo].getPalabraBloque(num_palabra, ind_cache); //Obtengo la palabra que quiero leer.
                        nucleos[id_nucleo].asignarRegistro(resultado, reg_destino); //Asigno la palabra al registro destino.
                        //Console.WriteLine("LW Nucleo: "+ id_nucleo + "lee el valor "+ resultado + " lo asigno al registro " + reg_destino + ". Palabra: " + num_palabra + " bloque: " + num_bloque + " Direccion " + direccion);
                        Monitor.Exit(cache_L1_datos[id_nucleo]);
                        cargado = true;
                    }
                    else
                    { //No hay acierto.
                        int estado_bloque_a_caer = cache_L1_datos[id_nucleo].getEstado(ind_cache); //Obtengo el estado del bloque al que le voy a caer encima
                        if (Monitor.TryEnter(cache_L2))
                        { //Pido candado para Cache L2, lo que significa que obtengo el bus y puedo trbajar con Cache L2 y memoria.
                            if (estado_bloque_a_caer == MODIFICADO)
                            { // si está modificado hay que mandarlo a escribir a la siguiente estructura.
                                BloqueDatos bloque_guardar = cache_L1_datos[id_nucleo].getBloque(ind_cache); //Obtengo el bloque que tengo que mandar a escribir.
                                int ind_bloque_guardar = cache_L1_datos[id_nucleo].getNumBloque(ind_cache);
                                cache_L1_datos[id_nucleo].setEstado(ind_cache, INVALIDO); //Invalido el bloque en cache L1.
                                //Se manda a escribir a L2 pero como L2 es No Write Allocate entonces se envia a escribir al siguiente nivel, osea, memoria.
                                memoria_datos[ind_bloque_guardar].setPalabras(bloque_guardar.getPalabras()); //Guardo el bloque en memoria.
                                for (int i = 0; i < 48; i++)
                                { //Se espera por los 8 tiempos que dura enviar a escribir a L2 y los 40 que dura en escribir en memoria.
                                    aumentarReloj(id_nucleo); //aumento reloj
                                }
                            }
                            int otro_nucleo = id_nucleo == 1 ? 0 : 1; //Para saber cual es el id del otro nucleo.
                            if (Monitor.TryEnter(cache_L1_datos[otro_nucleo]))
                            {
                                if (cache_L1_datos[otro_nucleo].getNumBloque(ind_cache) == num_bloque && cache_L1_datos[otro_nucleo].getEstado(ind_cache) == MODIFICADO)
                                {
                                    //Lo mando a escribir a Memoria y a mi cache.
                                    BloqueDatos bloque_guardar = cache_L1_datos[otro_nucleo].getBloque(ind_cache); //Obtengo el bloque que tengo que mandar a escribir.
                                    int ind_bloque_guardar = cache_L1_datos[otro_nucleo].getNumBloque(ind_cache);
                                    cache_L1_datos[otro_nucleo].setEstado(ind_cache, COMPARTIDO); //Pongo en compartido el bloque en la otra cache L1.
                                    //Se manda a escribir a L2 pero como L2 es No Write Allocate entonces se envia a escribir al siguiente nivel, osea, memoria.
                                    for (int i = 0; i < 8; i++)
                                    { //Se espera por los 8 tiempos que dura enviar a escribir a L2.
                                        aumentarReloj(id_nucleo); //aumento reloj
                                    }
                                    Monitor.Exit(cache_L1_datos[otro_nucleo]); //Libero la otra caché.
                                    memoria_datos[ind_bloque_guardar].setPalabras(bloque_guardar.getPalabras()); //Guardo el bloque en memoria.
                                    cache_L1_datos[id_nucleo].setBloque(bloque_guardar, num_bloque, ind_cache); //Escribo en mi cache el bloque.
                                    for (int i = 0; i < 40; i++)
                                    { //Se espera por los 40 ciclos que dura en escribir en memoria.
                                        aumentarReloj(id_nucleo); //aumento reloj
                                    }
                                }
                                else
                                {
                                    Monitor.Exit(cache_L1_datos[otro_nucleo]);
                                    int ind_cache_L2 = num_bloque % 8; //indice de cache para la chaché L2 de datos.
                                    if (cache_L2.getNumBloque(ind_cache_L2) != num_bloque || cache_L2.getEstado(ind_cache_L2) == INVALIDO)
                                    {
                                        int[] bloque_memoria = memoria_datos[num_bloque].getPalabras(); //Obtengo el bloque de memoria.
                                        cache_L2.setBloqueEnteros(bloque_memoria, num_bloque, ind_cache_L2); //Asigno el bloque a cache L2
                                        for (int i = 0; i < 40; i++)
                                        { //Se espera por los 40 ciclos que dura en escribir desde memoria.
                                            aumentarReloj(id_nucleo); //aumento reloj
                                        }
                                    }
                                    BloqueDatos bloque_cache_L2 = cache_L2.getBloque(ind_cache_L2);
                                    cache_L1_datos[id_nucleo].setBloque(bloque_cache_L2, num_bloque, ind_cache);
                                    for (int i = 0; i < 8; i++)
                                    { //Se espera por los 8 tiempos que dura enviar desde L2 a L1.
                                        aumentarReloj(id_nucleo); //aumento reloj
                                    }

                                    //Subir el bloque a mi cache.

                                }

                                Monitor.Exit(cache_L2);
                                int resultado = cache_L1_datos[id_nucleo].getPalabraBloque(num_palabra, ind_cache); //Obtengo la palabra que quiero leer.
                                //Console.WriteLine("LW Nucleo: " + id_nucleo + "lee el valor " + resultado + " lo asigno al registro " + reg_destino + ". Palabra: " + num_palabra + " bloque: " + num_bloque + " Direccion " + direccion);
                                nucleos[id_nucleo].asignarRegistro(resultado, reg_destino); //Asigno la palabra al registro destino.
                                Monitor.Exit(cache_L1_datos[id_nucleo]);
                                cargado = true;

                            }
                            else
                            {
                                Monitor.Exit(cache_L2);
                                Monitor.Exit(cache_L1_datos[id_nucleo]);
                                aumentarReloj(id_nucleo); //aumento reloj
                            }
                        }
                        else
                        {
                            Monitor.Exit(cache_L1_datos[id_nucleo]);
                            aumentarReloj(id_nucleo); //aumento reloj
                        }
                    }
                }
                else
                {
                    aumentarReloj(id_nucleo); //aumento reloj
                }

            }

            //aumento de reloj. Fin de instrucción
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
                Console.WriteLine("Ciclos de reloj: " + reloj.obtenerReloj());
                if (modo)
                {
                    Console.WriteLine("Presione Enter para continuar...");
                    while (Console.ReadKey().Key != ConsoleKey.Enter) { }
                }
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

        public void imprimirMemorias()
        {
            imprimirMemoriaDatos();
            imprimirCachesDatos();
            Console.WriteLine("==============================================================================================================");
        }

        public void imprimirMemoriaDatos()
        {
            Console.WriteLine("------------Memoria Datos--------------", -1);
            for (int i = 0; i < 24; i++)
            {
                int[] palabras = memoria_datos[i].getPalabras();
                Console.Write("Bloque " + i + "::::");
                Console.WriteLine("P1: " + palabras[0] + " P2: " + palabras[1] + " P3: " + palabras[2] + " P4: " + palabras[3]);
            }
        }

        public void imprimirCachesDatos()
        {
            Console.WriteLine("----------------Caché L1 datos nucleo 1--------------");
            int[] num_bloques1 = cache_L1_datos[0].obtenerNumBloques();
            int[] estados1 = cache_L1_datos[0].obtenerEstados();

            for (int i = 0; i < 4; i++)
            {
                BloqueDatos bd = cache_L1_datos[0].getBloque(i);
                Console.Write("Bloque " + i + "::::");
                Console.WriteLine(" P1: " + bd.getPalabra(0) + " P2: " + bd.getPalabra(1) + " P3: " + bd.getPalabra(2) + " P4: " + bd.getPalabra(3)+" E: "+estados1[i]+" B: "+num_bloques1[i]);
            }
            Console.WriteLine("----------------Caché L1 datos nucleo 2--------------");
            int[] num_bloques2 = cache_L1_datos[1].obtenerNumBloques();
            int[] estados2 = cache_L1_datos[1].obtenerEstados();

            for (int i = 0; i < 4; i++)
            {
                BloqueDatos bd = cache_L1_datos[1].getBloque(i);
                Console.Write("Bloque " + i + "::::");
                Console.WriteLine(" P1: " + bd.getPalabra(0) + " P2: " + bd.getPalabra(1) + " P3: " + bd.getPalabra(2) + " P4: " + bd.getPalabra(3) + " E: " + estados2[i] + " B: " + num_bloques2[i]);
            }
        }


        public void imprimirCacheL2()
        {
            Console.WriteLine("----------------Caché L2--------------");
            int[] num_bloques1 = cache_L2.obtenerNumBloques();
            int[] estados1 = cache_L2.obtenerEstados();

            for (int i = 0; i < 8; i++)
            {
                BloqueDatos bd = cache_L2.getBloque(i);
                Console.Write("Bloque " + i + "::::");
                Console.WriteLine(" P1: " + bd.getPalabra(0) + " P2: " + bd.getPalabra(1) + " P3: " + bd.getPalabra(2) + " P4: " + bd.getPalabra(3) + " E: " + estados1[i] + " B: " + num_bloques1[i]);
            }
        }

    }

}
