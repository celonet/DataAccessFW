using System;

namespace DataAccessFW.Model
{
    public class Where
    {
        public string Campo { get; set; }
        public Operador Operador { get; set; }

        public Object Valor { get; set; }

        public Where() { }

        public Where(string campo, object valor)
        {
            this.Campo = campo;
            this.Operador = Operador.Igual;
            this.Valor = valor;
        }

        public Where(string campo, Operador operador, object valor)
        {
            this.Campo = campo;
            this.Operador = operador;
            this.Valor = valor;
        }
    }
}
