using System;

namespace DataAccessFW.Atribute
{
    /// <summary>
    /// Referenciar Propriedades a Banco de Dados
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DbColumn : Attribute
    {
        /// <summary>
        /// Nome da Coluna no Banco de Dados
        /// </summary>
        public string ColumnName { get; set; }

        /// <summary>
        /// Tipo da Coluna no Banco de Dados
        /// </summary>
        public Type ColumnType { get; set; }

        /// <summary>
        /// Se é PrimaryKey
        /// </summary>
        public bool PrimaryKey { get; set; }

        public bool Identity { get; set; }

        /// <summary>
        /// Propriedade que define se campo será listado
        /// </summary>
        public bool List { get; set; }

        public bool Required { get; set; }

        public bool ComplexType { get; set; }

        public bool ForeignKey { get; set; }

        public string DbForeignKey { get; set; }

        public DbColumn()
        {
            PrimaryKey = false;
            List = true;
            Identity = true;
            Required = true;
        }

        public DbColumn(string columnName, Type columnType, bool primaryKey, bool list, bool required)
        {
            ColumnName = columnName;
            ColumnType = columnType;
            PrimaryKey = primaryKey;
            List = list;
            Required = required;
        }
    }
}