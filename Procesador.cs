using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace ProcesadorMIPS
{
    class Procesador
    {
        public int[,,] MemoriaInstrucciones;
        public int[,] MemoriaDatos, CacheL2, Contexto;
        int Quantum, CantidadHilillos;
        const int INVALIDO = -1;
        const int COMPARTIDO = 0;
        const int MODIFICADO = 1;

        public void Inicializar() {
            Console.WriteLine("Ingrese el número de hilillos que desea correr = ");
            CantidadHilillos = Convert.ToInt32(Console.ReadLine());
            String[] rutas = new String[CantidadHilillos];

            for (int i = 0; i < CantidadHilillos; i++)
            {
                OpenFileDialog op = new OpenFileDialog();
                op.Title = "Seleccione el hilillo " + Convert.ToString(i + 1);
                DialogResult result = op.ShowDialog();
                if (result == DialogResult.OK) // Considerar el caso de que seleccione cancelar.
                {
                    rutas[i] = op.FileName;
                    Console.WriteLine(rutas[i]);
                }

            }


            Quantum = 0;
            while (Quantum < 1)
            {
                // Numero Quantum = numero de ciclos que puede correr un hilillo en el nucleo.
                Console.WriteLine("Ingrese el número quantum (Número entero mayor a cero) = ");
                Quantum = Convert.ToInt32(Console.ReadLine());
            }
            //Application.Run(new Form1());

            // Memoria Instrucciones = 40 bloques, 4 palabras por bloque, 4 cantidad de numeros por instruccion.
            MemoriaInstrucciones = new int[40, 4, 4];
            // Memomia Datos = 24 bloques, 4 palabras.
            MemoriaDatos = new int[24, 4];
            // Cache L2 Compartida = 8 bloques, 4 palabras, estado y numero de bloque en memoria principal.
            CacheL2 = new int[8, 6];

            // Contexto =  n cantidad de hilillos corriendo, 32 registros y el Program Counter.
            Contexto = new int[CantidadHilillos, 33];

            IniciarMemoria();
            InicializarCache();
            CargarInstrucciones(rutas);

            // Inicializar el nucleo 1
            Thread Nucleo1 = new Thread(Nucleo.inicializar);
            Nucleo1.Start();
            // Inicializar el nucleo 2
            Thread Nucleo2 = new Thread(Nucleo.inicializar);
            Nucleo2.Start();

            
        }

        /*
         * Método que carga las instrucciones de los hilillos en memoria de instrucciones
         */
        public void CargarInstrucciones(String[] rutas)
        {
            string line;
            String[] Instruccion;
            int bloque, palabra;
            int counter = 0;
            System.IO.StreamReader file;
            for (int i = 0; i < rutas.Length; i++) {
                Contexto[i, 32] = counter;
                file = new System.IO.StreamReader(rutas[i]); // accede al texto.
                while ((line = file.ReadLine()) != null)
                {
                    bloque = counter / 16; //número de bloque.
                    palabra = (counter % 16) / 4; //numero de palabra.
                    Instruccion = line.Split(' '); //Separa la instruccion en los 4 números
                    for (int j = 0; j < 4; j++) {
                        MemoriaInstrucciones[bloque, palabra, j] = Convert.ToInt32(Instruccion[j]); //asigna el numero en la matriz.
                    }
                    counter += 4; // suma el contador para el PC.
                }
            }
        }

        /*
         * Método que inicializa la memoria principal en 1.
         */
        public void IniciarMemoria() {
            //Se inicializa la memoria de instrucciones en 1.
            for (int i = 0; i < 40; i++) {
                for (int j = 0; j < 4; j++) {
                    for (int k = 0; k < 4; k++) {
                        MemoriaInstrucciones[i, j, k] = 1;
                    }
                }
            }
            //Se inicializa la memoria de datos en 1.
            for (int i = 0; i < 24; i++){
                for (int j = 0; j < 4; j++){
                    MemoriaDatos[i, j] = 1;
                }
            }
        }

        /*
         * Método para poner en invalido los bloques en cache al inicio de la ejecución. 
         */
        private void InicializarCache()
        {
            for (int i = 0; i < 8; i++)
            {
                CacheL2[i, 4] = INVALIDO;
            }
        }

    }
}
