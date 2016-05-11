using System;

namespace DataAccessFW.Atribute
{
    /// <summary>
    /// Atributo para Framework de Persistencia
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DbTable : Attribute
    {
        public readonly string TableName;

        public DbTable()
        {

        }

        /// <summary>
        /// Construtor
        /// </summary>
        /// <param name="tableName">Nome da Tabela</param>
        public DbTable(string tableName)
        {
            TableName = tableName;
        }

        public bool DeleteAll { get; set; }

        public bool ComplexType { get; set; }
    }
}