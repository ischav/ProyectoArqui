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
        public int RL; 
        public int quantum_hilillo;
        public bool finalizado;
        public String identificador_hilillo;
        public int acumulador_ciclos_reloj;


        public Nucleo() {
            quantum_hilillo = 0;
            finalizado = false;
            registros_nucleo = new int[33];
            identificador_hilillo = "-1";
            RL = -1; // empieza en -1.
        }

        public void aumentarAcumuladorReloj()
        {
            acumulador_ciclos_reloj++;
        }


        public int obtenerAcumuladorReloj()
        {
            return acumulador_ciclos_reloj;
        }

        public void asignarAcumuladorReloj(int valor)
        {
            acumulador_ciclos_reloj=valor;
        }
        /*
         * Obtener el identificador del hilillo
        */
        public String obtenerIdentificadorHilillo()
        {
            return identificador_hilillo; 
       }

        /*
         * Asignar un identificador correspondiente al hilillo que se cargo en este núcleo
        */
        public void asignarIdentificadorHilillo(String id)
        {
            identificador_hilillo = id;
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
            for (int i = 0; i < 33; i++)
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
        public void asignarQuantum(int valor)
        {
            quantum_hilillo = valor;
        }

        /*
        *
        *
        */
        public void aumentarQuantum()
        {
            quantum_hilillo++;
        }

        /*
        *
        *
        */
        public int obtenerQuantum()
        {
            return quantum_hilillo;
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

        public void asignarRL(int valor) {
            RL = valor;
        }

        public int obtenerRL() {
            return RL;
        }

    }
}
