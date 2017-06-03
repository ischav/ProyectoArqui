using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcesadorMIPS
{
    public class Nucleo
    {
        // Estados de un bloque
        const int INVALIDO = -1;
        const int COMPARTIDO = 0;
        const int MODIFICADO = 1;
        
        public static int[] registros_nucleo;
        public CacheDatos cache_L1_datos;
        public CacheInstrucciones cache_L1_instr;
        // Program Counter
        public int pc_nucleo;

        public Nucleo() {
            // Registros = 32 registros y el RL
            registros_nucleo = new int[33];
            // Cache L1 Instrucciones = 4 bloques, 4 palabras, 4 numeros por instruccion.
            cache_L1_instr = new CacheInstrucciones();
            // Cache L1 Datos = 4 bloques, 6 donde 4 son palabras, estado y numero del bloque.
            cache_L1_datos = new CacheDatos(4);
        }
        

        /*
        *
        *
        */
        public void asignarContexto(int[] reg)
        {

            for (int i = 0; i < 32; i++)
                registros_nucleo[i] = reg[i];
            pc_nucleo = reg[32];
        }


    }
}
