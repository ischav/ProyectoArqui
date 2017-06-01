using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcesadorMIPS
{
    public class Nucleo
    {
        const int INVALIDO = -1;
        const int COMPARTIDO = 0;
        const int MODIFICADO = 1;
        public static int[] registros_nucleo, cache_L1_instr_etiq;
        public static int[,] cache_L1_Datos;
        public static int[,,] cache_L1_Instr;
        // Program Counter
        public int pc_nucleo;



        public Nucleo() {
            // Registros = 32 registros y el RL
            registros_nucleo = new int[33];
            // Cache L1 Instrucciones = 4 bloques, 4 palabras, 4 numeros por instruccion.
            cache_L1_Instr = new int[4, 4, 4];
            // Cache L1 Instrucciones Etiquetas = para almacenar el numero de bloque de memoria.
            cache_L1_instr_etiq = new int[4];
            // Cache L1 Datos = 4 bloques, 6 donde 4 son palabras, estado y numero del bloque.
            cache_L1_Datos = new int[4, 6];
        }

        /*
         * Método para poner en invalido y en ceros los bloques de datos en cache al inicio de la ejecución. 
        */
        public void inicializarCacheL1Datos() {
            for (int i = 0; i < 4; i++) {
                for (int j=0;j<6;j++)
                {
                    cache_L1_Datos[i, j] = 0;
                }
                cache_L1_Datos[i, 4] = INVALIDO;
            }
        }

        /*
        * Método para poner en invalido y en ceros los bloques de insgr en cache al inicio de la ejecución. 
        */
        public void inicializarCacheL1Inst()
        {
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    for (int k=0;k<4;k++)
                    {
                        cache_L1_Instr[i, j, k] = 0;
                    }
                }
                cache_L1_instr_etiq[i] = INVALIDO;
            }
        }


        /*
        *
        *
        */
        public int[,] obtenerL1Datos()
        {
            return cache_L1_Datos;
        }

        /*
        *
        *
        */
        public int[,,] obtenerL1Instrucciones()
        {
            return cache_L1_Instr;
        }



        /*
        *
        *
        */
        public void asignarContexto(int[] reg)
        {

            for (int i = 0; i < 33; i++)
                registros_nucleo[i] = reg[i];
        }


    }
}
