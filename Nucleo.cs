using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcesadorMIPS
{
    class Nucleo
    {
        const int INVALIDO = -1;
        const int COMPARTIDO = 0;
        const int MODIFICADO = 1;
        public static int[] registros, CacheL1InstrEtiq;
        public static int[,] CacheL1Datos;
        public static int[,,] CacheL1Instr;
        // Program Counter
        public static int PC;

        public static void inicializar() {
            // Registros = 32 registros y el RL
            registros = new int[33];
            // Cache L1 Instrucciones = 4 bloques, 4 palabras, 4 numeros por instruccion.
            CacheL1Instr = new int[4,4,4];
            // Cache L1 Instrucciones Etiquetas = para almacenar el numero de bloque de memoria.
            CacheL1InstrEtiq = new int[4];
            // Cache L1 Datos = 4 bloques, 6 donde 4 son palabras, estado y numero del bloque.
            CacheL1Datos = new int[4, 6];
            InicializarCache();
        }

        /*
         * Método para poner en invalido los bloques en cache al inicio de la ejecución. 
         */
        private static void InicializarCache() {
            for (int i = 0; i < 4; i++) {
                CacheL1Datos[i, 4] = INVALIDO;
            }
        }

    }
}
