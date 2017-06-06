using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcesadorMIPS
{
    class Hilillo
    {
        int identificador_hilillo;//valor único para cada hilillo
        int inicio_hilillo;//indica el inicio de la ejecución del hilillo.
        int fin_hilillo;//indica el fin de la ejecución del hilillo
        int [] registros_hilillo;//es el contexto, mantiene los valores del hilillo.
        int pc_hilillo;
        int nucleo_hilillo;//indica el procesador en el que está corriendo el hilillo
        int ciclos_reloj_hilillo;//cantidad de ciclos de reloj asignadas al hilillo
        bool finalizado_hilillo;//indica el estado del hilillo

        /*
        *
        *
        */
        public Hilillo(int identificador)
        {
            this.identificador_hilillo = identificador;
            inicio_hilillo = 0;
            fin_hilillo = 0;
            pc_hilillo = 0;
            nucleo_hilillo = -1; // Para que inicialmente no pertenezca a ninguno
            finalizado_hilillo = false;
            registros_hilillo = new int[33];
            for (int i = 0; i < registros_hilillo.Length; i++)
            {
                registros_hilillo[i] = 0;
            }
            //se asigna el PC
            pc_hilillo = registros_hilillo[32];
            ciclos_reloj_hilillo = 0;
        }

        /*
        *
        *
        */
        public void Inicializar(int identificador) {

        }



        /*
        *
        *
        */
        public void asignarNumeroHilillo(int numero)
        {
            this.identificador_hilillo = numero;
        }
        
        
        /*
        *
        *
        */
        public int obtenerNumero_hil()
        {
            return identificador_hilillo;
        }
        
        
        /*
        *
        *
        */
        public void asignarCiclosReloj(int ciclos)
        {
            this.ciclos_reloj_hilillo = ciclos;
        }
        
        
        /*
        *
        *
        */
        public int obtenerCiclosReloj()
        {
            return this.ciclos_reloj_hilillo;
        }
        
        
        /*
        *
        *
        */
        public void asignarInicioHilillo(int inicia)
        {
            inicio_hilillo = inicia;
        }


        /*
        *
        *
        */
        public int obtenerInicioHilillo()
        {
            return inicio_hilillo;
        }
        
        
        /*
        *
        *
        */
        public void asignarFinHilillo(int termina)
        {
            fin_hilillo = termina;
        }
        
        
        /*
        *
        *
        */
        public int obtenerFinHilillo()
        {
            return fin_hilillo;
        }
        
        
        /*
        *
        *
        */
        public int obtenerPC()
        {
            return registros_hilillo[32];
        }
        
        
        /*
        *
        *
        */
        public void asignarPC(int contador_programa)
        {
            registros_hilillo[32] = contador_programa;
        }

        
        /*
        *
        *
        */
        public int[] obtenerRegistros()
        {
            return registros_hilillo;
        }

        
        /*
        *
        *
        */
        public void asignarContexto(int contador_programa, int[] reg)
        {

            for (int i = 0; i < 33; i++)
                registros_hilillo[i] = reg[i];
        }

        /*
        *
        *
        */
        public int[] obtenerContexto()
        {
            return registros_hilillo;
        }


        /*
        *
        *
        */
        public void asignarFinalizado()
        {
            finalizado_hilillo = true;
        }


        /*
        *
        *
        */
        public void asignarFinalizado(bool f)
        {
            finalizado_hilillo = f;
        }

        
        /*
        *
        *
        */
        public bool obtenerFinalizado()
        {
            return finalizado_hilillo;
        }
        

        /*
        *
        *
        */
        public void asignarNumeroNucleo(int hilo)
        {
            nucleo_hilillo = hilo;
        }


        /*
        *
        *
        */
        public int obtenerNumeroNucleo()
        {
            return nucleo_hilillo;
        }


    }
}
