using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace ProcesadorMIPS
{
    /*Esta clase contiene los datos que son compartidos por ambos nucleos
     * Los datos independientos como lo son las caché L1 son controladas por cada núcleo
     */
    class Procesador
    {

        public int[,,] memoria_instrucciones;
        public int[,] memoria_datos, cache_L2;
        int  quantum, cantidad_hilillos;
        const int INVALIDO = -1;
        const int COMPARTIDO = 0;
        const int MODIFICADO = 1;
        Nucleo []nucleos;//contiene ambos nucleos de la maquina
        Queue<Hilillo> cola_hilillos;//cola de donde los nucleos obtienen hilillos para ejecutar

        /*
         * Contructor de la clase
         * inicializa las variables
        */
        public Procesador()
        {
            quantum = 0;
            // Memoria Instrucciones = 40 bloques, 4 palabras por bloque, 4 cantidad de numeros por instruccion.
            memoria_instrucciones = new int[40, 4, 4];
            // Memomia Datos = 24 bloques, 4 palabras.
            memoria_datos = new int[24, 4];
            // Cache L2 Compartida = 8 bloques, 4 palabras, estado y numero de bloque en memoria principal.
            cache_L2 = new int[8, 6];

            //Se crean e inicializan los nucleos con sus respectivas caché L1
            nucleos = new Nucleo[2];
            for (int i = 0;i<2;i++)
            {
                nucleos[i] = new Nucleo();
                nucleos[i].inicializarCacheL1Datos();
                nucleos[i].inicializarCacheL1Inst();
            }


            //Se inicializa la cola donde serán almacenados los hilillos
            cola_hilillos = new Queue<Hilillo>();

        }


        /*
         * indica la cantidad total de hilillos que corren en la simulacion
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
            //Se inicializa la memoria de instrucciones en 1.
            for (int i = 0; i < 40; i++) {
                for (int j = 0; j < 4; j++) {
                    for (int k = 0; k < 4; k++) {
                        memoria_instrucciones[i, j, k] = 1;
                    }
                }
            }

            //Se inicializa la memoria de datos en 1.
            for (int i = 0; i < 24; i++){
                for (int j = 0; j < 4; j++){
                    memoria_datos[i, j] = 1;
                }
            }
        }



        /*
         * Método que carga las instrucciones de los hilillos en memoria de instrucciones
         * Recibe por parametro un vector con los nombres de los archivos que contienen las instrucciones
        */
        public void CargarInstrucciones(String[] rutas)
        {
            string line;
            String[] Instruccion;
            int bloque, palabra;
            int counter = 0;
            int inicio = 0;
            int fin = 0;
            System.IO.StreamReader file;
            for (int i = 0; i < rutas.Length; i++) {
                //Contexto[i, 32] = counter;
                file = new System.IO.StreamReader(rutas[i]); // accede al texto.
                inicio = counter;
                while ((line = file.ReadLine()) != null)
                {
                    bloque = counter / 16; //número de bloque.
                    palabra = (counter % 16) / 4; //numero de palabra.
                    Instruccion = line.Split(' '); //Separa la instruccion en los 4 números
                    for (int j = 0; j < 4; j++) {
                        memoria_instrucciones[bloque, palabra, j] = Convert.ToInt32(Instruccion[j]); //asigna el numero en la matriz.
                    }
                    counter += 4; // suma el contador para el PC.
                }
                fin = counter;
                crearHilillo(i,inicio,fin);
            }
        }


        /*
         * Método para poner en invalido y ceros los bloques en cache al inicio de la ejecución. 
         */
        public void InicializarCache()
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j=0;j<6;j++)
                {
                    cache_L2[i, j] = 0;
                }
                cache_L2[i, 4] = INVALIDO;
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
         * Metodo para imprimir todos los datos actualmente en memoria y caché
        */
        public string imprimirMemoriaEstructuras()
        {
            string datos = "";
            datos += "\nMemoria principal de instrucciones\n";
            for (int i = 0; i < 40; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    for (int k = 0; k < 4; k++)
                    {
                        datos += Convert.ToString(memoria_instrucciones[i,j,k])+" | ";
                    }
                }
            }

            datos += "\nMemoria principal de datos\n";
            for (int i = 0; i < 24; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    datos+=Convert.ToString(memoria_datos[i, j])+" | ";
                }
            }

            for(int q = 0; q < 2; q++)
            {
                //se obtienen las matrices
                int[,] l1_datos = nucleos[q].obtenerL1Datos();
                int[,,] l1_inst = nucleos[q].obtenerL1Instrucciones();
                //se obtienen los datos de las matrices
                datos += "\nL1 de instrucciones\n";
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        for (int k = 0; k < 4; k++)
                        {
                            datos += Convert.ToString(l1_inst[i, j, k]) + " | ";
                        }
                    }
                }

                datos += "\nL1de datos\n";
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 6; j++)
                    {
                        datos += Convert.ToString(l1_datos[i, j]) + " | ";
                    }
                }

            }

            return datos;
        }

        /*
         * Retorna verdadero si se pudo desencolar un hilillo
         * En caso de retornar verdero el nucleo contiene los datos del nuevo hilillo
        */ 
        public bool desencolarHilillo(int num_nucleo)
        {
            if (cola_hilillos.Count>0)
            {
                Hilillo hilo_desencolado=cola_hilillos.Dequeue();
                int[] contexto_hilo_desencolado = hilo_desencolado.obtenerContexto();
                nucleos[num_nucleo].asignarContexto(contexto_hilo_desencolado);
            }

            return true;
        }



        /*Metodo principal de la simulación 
         * Una vez todo cargado en el procesador inicia la ejecución
         * recibe por parámetro el número de nucleo que se está llamando
        */
        public void inicializar(object nucleo)
        {
            int num_nucleo = Convert.ToInt32(nucleo);
            Console.WriteLine("iniciando el núcleo: "+Convert.ToString(num_nucleo));
            while (this.cola_hilillos.Count>0)//mientras existan hilos en cola ejecutar
            {
                if (desencolarHilillo(num_nucleo))
                {
                    
                }
            }
        }


    }
}
