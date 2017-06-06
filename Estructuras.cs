using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcesadorMIPS
{
    public class Reloj
    {
        int reloj;
        public Reloj()
        {
            reloj = 0;
        }

        public int obtenerReloj()
        {
            return reloj;
        }

        public void asignarReloj(int cantidad)
        {
            reloj += cantidad;
        }

        public void aumentarReloj()
        {
            reloj++;
        }
    }



    public class Instruccion {
        int[] instruccion;

        public Instruccion() {
            instruccion = new int[4];
        }
        public void setInstruccion(int[] nueva_instruccion)
        {
            for (int i = 0; i < 4; i++)
                instruccion[i] = nueva_instruccion[i];
        }

        public int getParteInstruccion(int indice)
        {
            return instruccion[indice];
        }

        public int[] getInstruccion()
        {
            return instruccion;
        }

    }

    public class BloqueDatos
    {
        public int[] palabras;

        public BloqueDatos()
        {
            palabras = new int[4];
        }

        public int[] getPalabras()
        {
            return palabras;
        }
        public void setPalabras(int[] nuevo_palabras)
        {
            for (int i = 0; i < 4; i++)
                palabras[i] = nuevo_palabras[i];
        }

        public void setPalabra(int dato, int indice)
        {
            palabras[indice] = dato;
        }

        public int getPalabra(int indice)
        {
            return palabras[indice];
        }
    }

    public class BloqueInstrucciones {
        public Instruccion[] instrucciones;

        public BloqueInstrucciones() {
            instrucciones = new Instruccion[4];

            for (int i = 0; i < 4; i++)
                instrucciones[i] = new Instruccion();

        }

        public void setInstruccion(Instruccion instruccion, int indice)
        {
            instrucciones[indice].setInstruccion(instruccion.getInstruccion());
        }

        public void setInstruccion(int[] instruccion, int indice)
        {
            instrucciones[indice].setInstruccion(instruccion);
        }

        public Instruccion getInstruccion(int indice)
        {
            return instrucciones[indice];
        }

        public void setInstrucciones(Instruccion[] nuevas_instrucciones)
        {
            for (int i = 0; i < 4; i++)
                instrucciones[i].setInstruccion(nuevas_instrucciones[i].getInstruccion());
           
        }
        

        public Instruccion[] getInstrucciones()
        {
            return instrucciones;
        }

    }


    public class CacheDatos {
        public BloqueDatos[] bloques;
        public int[] estados;
        public int[] num_bloques;

        public CacheDatos(int tamano) {
            bloques = new BloqueDatos[tamano];
            estados = new int[tamano];
            num_bloques = new int[tamano];

            for (int i = 0; i < tamano; i++)
            {
                bloques[i] = new BloqueDatos();
                estados[i] = -1; //Invalido
                num_bloques[i] = -1; //No hay bloque
            }
        }

        public void setBloque(BloqueDatos nuevo_bloque, int nuevo_num_bloque, int indice_cache) {
            bloques[indice_cache].setPalabras(nuevo_bloque.getPalabras());
            estados[indice_cache] = 0; //Compartido
            num_bloques[indice_cache] = nuevo_num_bloque;
        }

        public BloqueDatos getBloque(int indice_cache) {
            return bloques[indice_cache];
        }
        public int getEstado(int indice_cache) {
            return estados[indice_cache];
        }
        public void setEstado(int indice_cache, int nuevo_estado)
        {
            estados[indice_cache] = nuevo_estado;
        }

        public int getNumBloque(int indice_cache)
        {
            return num_bloques[indice_cache];
        }
        public bool hit(int nuevo_num_bloque, int indice_cache)
        {
            if (nuevo_num_bloque == num_bloques[indice_cache])
                return true;
            return false;
        }

    }

    public class CacheInstrucciones {
        public BloqueInstrucciones[] bloques;
        public int[] num_bloques;

        public CacheInstrucciones() {
            bloques = new BloqueInstrucciones[4];
            num_bloques = new int[4];
            for (int i = 0; i < 4; i++)
            {
                bloques[i] = new BloqueInstrucciones();
                num_bloques[i] = -1; //No hay bloque
            }
        }

        public void setBloque(BloqueInstrucciones nuevo_bloque, int nuevo_num_bloque, int indice_cache) {
            bloques[indice_cache].setInstrucciones(nuevo_bloque.getInstrucciones());
            num_bloques[indice_cache] = nuevo_num_bloque;
        }

        public BloqueInstrucciones getBloque(int indice_cache) {
            return bloques[indice_cache];
        }



        public int getNumBloque(int indice_cache)
        {
            return num_bloques[indice_cache];
        }

        public Instruccion getInstruccion(int palabra, int indice_cache)
        {
            return bloques[indice_cache].getInstruccion(palabra);
        }


        public bool hit(int nuevo_num_bloque, int indice_cache)
        {
            if (nuevo_num_bloque == num_bloques[indice_cache])
                return true;
            return false;
        }


    }




}
