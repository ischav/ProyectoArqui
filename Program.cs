using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProcesadorMIPS
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(String[] args)
        {

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Procesador procesador = new Procesador();

            Console.WriteLine("---- Procesador MIPS para enteros con 2 núcleos ----\n");
            int cantidad_hilillos = obtenerCantidadHilillos();
            string[] rutas = leerArchivo(cantidad_hilillos);
            int quantum = obtenerQuantum();

            //se asigna el quantum al procesado
            procesador.asignarQuantum(quantum);
            //se asigna la cantidad de hilillos al procesador
            procesador.asignarNumeroHilillos(cantidad_hilillos);
            //se crean las estructuras para la memoria principal
            procesador.IniciarMemoria();
            //se crean los nucleos y son memorias asociadas
            procesador.IniciarNucleos();

            /* Al cargar las instrucciones se crean los hilillos 
             * ya que es el punto donde se sabe cuales intrucciones 
             * se deben ejecutar*/
            procesador.CargarInstrucciones(rutas);

            //Console.Write(procesador.imprimirMemoriaEstructuras());

            /*
             * En este punto el procesador tiene todo lo necesario para trabajar
             * Se han creado los hilillos y se han asignado a una cola
             * Se inicializaron los datos en memoria (inst,datos)
             * Se crearor los nucleos del procesador
             */

            bool modo = obtenerModo();
            procesador.asignarModo(modo);

            // Se inicia la simulación nucleo 1 y 2
            Thread Nucleo1 = new Thread(procesador.inicializar);
            Thread Nucleo2 = new Thread(procesador.inicializar);
            Nucleo1.Name="1";
            Nucleo2.Name = "2";

            //ejecución de ambos núcleos
            Nucleo1.Start(0);
            Nucleo2.Start(1);


            //se espera hasta que ambos terminen
            while (Nucleo1.IsAlive || Nucleo2.IsAlive)
            { }

            //se obtienen para recolectar sus dados



            Nucleo1.Join();
            Nucleo2.Join();
            Nucleo[] nucleos=procesador.obtenerNucleos();
            int[] registros_nucleo_1 = nucleos[0].obtenerRegistros();
            int[] registros_nucleo_2 = nucleos[1].obtenerRegistros();


            procesador.imprimirMemoriaDatos();
            procesador.imprimirCacheL2();
            procesador.imprimirColaHilillosFinalizados();
            //procesador.imprimirCacheL1Hilillos();
            //imprimirRegistros(registros_nucleo_1,1);
            //imprimirRegistros(registros_nucleo_2,2);
            //procesador.imprimirRegistros();
            //procesador.imprimirColaHilillos();
            //procesador.imprimirColaHilillosFinalizados();

            Console.WriteLine("Presione Enter para finalizar la ejecución...");
            while (Console.ReadKey().Key != ConsoleKey.Enter) { }
        }

        public static void imprimirRegistros(int[] registros_nucleo, int nucleo_id)
        {
            Console.WriteLine("Imprimiendo registros del nucleo "+nucleo_id+" después de la ejecución");
            for (int i = 0; i < 33; i++)
            {
                Console.Write(registros_nucleo[i]+"::");
            }
            Console.WriteLine();

        }

        public static int obtenerCantidadHilillos()
        {
            int cantidad_hilillos = 0;
            //se recibe la cantidad de hilillos y se verifica que sea mayor a cero
            while (cantidad_hilillos <= 0) {
                Console.WriteLine("Ingrese el número de hilillos que desea correr = ");
                cantidad_hilillos = Convert.ToInt32(Console.ReadLine());
            }
            return cantidad_hilillos;
        }

        public static bool obtenerModo()
        {
            DialogResult dialogResult = MessageBox.Show("Desea ver la ejecución en modo lento?", "Modo", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                return true;
            }
            else if (dialogResult == DialogResult.No)
            {
                return false;
            }

            return false;
        }

        // Leer ruta de archivos
        public static string[] leerArchivo(int cantidad_hilillos)
        {
            String[] rutas = new String[cantidad_hilillos];

            for (int i = 0; i < cantidad_hilillos; i++)
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
            return rutas;
        }

        public static int obtenerQuantum()
        {
            int quantum = 0;
            while (quantum < 1)
            {
                // Numero Quantum = numero de ciclos que puede correr un hilillo en el nucleo.
                Console.WriteLine("Ingrese el número quantum (Número entero mayor a cero) = ");
                quantum = Convert.ToInt32(Console.ReadLine());
            }
            return quantum;
        }

    }
}
