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

            int cantidad_hilillos = obtenerCantidadHilillos();
            string[] rutas = leerArchivo(cantidad_hilillos);
            int quantum = obtenerQuantum();

            procesador.asignarQuantum(quantum);
            procesador.IniciarMemoria();
            procesador.InicializarCache();
            
            //al cargarsen las instrucciones se crean los hilillos ya que es el punto donde se sabe cuales intrucciones deben ejecutar
            procesador.CargarInstrucciones(rutas);


            Console.Write(procesador.imprimirMemoriaEstructuras());

            /*
             * En este punto el procesador tiene todo lo necesario para trabajar
             * Se han creado los hilillos y se han asignado a una cola
             * Se inicializaron los datos en memoria (inst,datos)
             * Se crearor los nucleos del procesador
             */

            // Se inicia la simulación nucleo 1 y 2
            Thread Nucleo1 = new Thread(procesador.inicializar);
            Thread Nucleo2 = new Thread(procesador.inicializar);

            //ejecución de ambos núcleos
            Nucleo1.Start(0);
            Nucleo2.Start(1);


            //se espera hasta que ambos terminen
            while (Nucleo1.IsAlive || Nucleo2.IsAlive)
            { }

            //se obtienen para recolectar sus dados
            Nucleo1.Join();
            Nucleo2.Join();
        }

        public static int obtenerCantidadHilillos()
        {
            //se recibe la cantidad de hilos
            Console.WriteLine("Ingrese el número de hilillos que desea correr = ");
            int cantidad_hilillos = Convert.ToInt32(Console.ReadLine());
            return cantidad_hilillos;
        }

        public static string [] leerArchivo(int cantidad_hilillos)
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
