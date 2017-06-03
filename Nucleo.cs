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
        
        public int[] registros_nucleo;

        // Program Counter
        public int pc_nucleo;


        public Nucleo() {
            registros_nucleo = new int[32];
        }
        

        /*
        *Asigna los registros y el contexto al nucleo
        *
        */
        public void asignarContexto(int[] reg)
        {
            for (int i = 0; i < 32; i++)
                registros_nucleo[i] = reg[i];
                //se asigna el PC
                pc_nucleo = reg[32];
        }

        /*
        *
        *
        */
        public int obtenerPc()
        {
            return pc_nucleo;
        }

        /*
        *
        *
        */
        public int[] obtenerRegistros()
        {
            return registros_nucleo;
        }

    }
}
