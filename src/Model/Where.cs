namespace DataAccessFW.Model
{
    public class Where
    {
        public string Campo { get; set; }
        public Operador Operador { get; set; }

        public object Valor { get; set; }

        public Where() { }

        public Where(string campo, object valor)
        {
            Campo = campo;
            Operador = Operador.Igual;
            Valor = valor;
        }

        public Where(string campo, Operador operador, object valor)
        {
            Campo = campo;
            Operador = operador;
            Valor = valor;
        }
    }
}
