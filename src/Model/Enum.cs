namespace DataAccessFW.Model
{
    public enum Provider
    {
        OleDb,
        Odbc,
        SqlServer,
        MySql,
        Oracle,
        Custom
    }

    internal enum TipoQuery
    {
        Select,
        Insert,
        Update,
        Delete
    }

    public enum Operador
    {
        Igual,
        Diferente,
        MaiorIgual,
        MenorIgual,
        BehindLike,
        Like,
        AfterLike
    }
}
