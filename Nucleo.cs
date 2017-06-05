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

        //registros en el nucleo
        public int[] registros_nucleo;

        // Program Counter, registro 32
        public int pc_nucleo;

        public bool finalizado;

        public Nucleo() {
            finalizado = false;
            registros_nucleo = new int[32];
        }
        
        /*
         * modifica el estado del nucleo a finalizado "el hilillo terminó"
        */
        public void setFinalizado(bool estado)
        {
            finalizado = estado;
        }

        /*
         * modifica el estado del nucleo a finalizado "el hilillo terminó"
        */
        public bool getFinalizado()
        {
            return finalizado;
        }


        /*
         * *Asigna los registros y el contexto al nucleo
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
        public void aumentarPc(int cantidad)
        {
            registros_nucleo[32] += cantidad;
            pc_nucleo = registros_nucleo[32];
        }

        /*
        *
        *
        */
        public void aumentarPc()
        {
            registros_nucleo[32] += 4;
            pc_nucleo = registros_nucleo[32];
        }

        /*
        *
        *
        */
        public void asignarPc(int new_contador)
        {
            registros_nucleo[32] = new_contador;
            pc_nucleo = registros_nucleo[32];
        }


        /*
        *
        *
        */
        public int[] obtenerRegistros()
        {
            return registros_nucleo;
        }

        public int obtenerRegistro(int num_reg)
        {
            return registros_nucleo[num_reg];
        }

        public void asignarRegistro(int value, int num_reg)
        {
            registros_nucleo[num_reg] = value;
        }

    }
}
