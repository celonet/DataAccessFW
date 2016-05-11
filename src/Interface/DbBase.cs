using DataAccessFW.Atribute;
using DataAccessFW.Model;
using DataAccessFW.Service;
using DataAccessFW.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DataAccessFW.Interface
{
    public abstract class DbBase<T>
    {
        #region Prop

        private readonly Database _database;
        private readonly bool _loadAttr;
        private readonly string _tableName;
        private readonly bool _deleteAll;

        public string Query { get; set; }

        public string ErrorMessage { get; set; }

        #endregion

        protected DbBase()
        {
            _database = DbAccess.Instance.GetDatabase();
            var sttrDb = GetAttrDbTable();
            if (sttrDb != null)
            {
                _loadAttr = true;
                _tableName = sttrDb.TableName ?? GetNameDb();
                _deleteAll = sttrDb.DeleteAll;
            }
            else
            {
                _tableName = GetNameDb();
            }
        }

        #region DML

        public virtual T Consulta(params Where[] where)
        {
            ErrorMessage = string.Empty;
            var sb = new StringBuilder();
            sb.Append("SELECT ");
            sb.Append(MakeSql(TipoQuery.Select));
            sb.AppendFormat("FROM {0} ", _tableName);

            if (where != null && where.Length > 0)
            {
                sb.Append("WHERE ");
                sb.Append(MakeWhere(where));
            }
            else
            {
                Query = sb.ToString();
                ErrorMessage = "Necessario instrução Where!";
                return default(T);
            }

            var msgError = "";
            Query = sb.ToString();
            var result = _database.ExecuteQuery(sb.ToString(), ref msgError, null);
            if (result.Rows.Count == 0)
                ErrorMessage = msgError;

            if (result.Rows.Count > 0)
            {
                var myObject = (T)Activator.CreateInstance(typeof(T));

                for (var i = 0; i < result.Columns.Count; i++)
                {
                    var prop = myObject.GetType().GetProperty(result.Columns[i].ColumnName, BindingFlags.Public | BindingFlags.Instance);
                    var sttr = GetAttrDbColumn(prop);
                    if (null != prop && prop.CanWrite)
                    {
                        if (result.Rows[0].ItemArray[i] is DBNull)
                            continue;
                        try
                        {
                            if (prop.PropertyType == typeof(bool))
                                prop.SetValue(myObject, result.Rows[0].ItemArray[i].ToString() != "0", null);
                            else
                                prop.SetValue(myObject, result.Rows[0].ItemArray[i], null);
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                    }

                    #region Carrega ComplexType

                    if (sttr == null || !sttr.ForeignKey)
                        continue;

                    var pName = string.IsNullOrWhiteSpace(sttr.DbForeignKey) ? result.Rows[0].Table.Columns[i].ColumnName.Replace("ID", "") : sttr.DbForeignKey;

                    var pComplexType = myObject.GetType().GetProperty(pName, BindingFlags.Public | BindingFlags.Instance);

                    if (pComplexType == null || !pComplexType.CanWrite)
                        continue;

                    var id = ConvertType.ToInt(result.Rows[0].ItemArray[i]);

                    var complexType = pComplexType.PropertyType;
                    var myObjectComplex = Activator.CreateInstance(pComplexType.PropertyType);
                    var magicConstructor = complexType.GetConstructor(Type.EmptyTypes);

                    if (myObjectComplex == null)
                        continue;

                    var complexTypeObject = magicConstructor.Invoke(new object[] { });

                    var methInfo = complexType.GetMethod("Consulta");

                    if (methInfo == null)
                        continue;

                    var objectValue = methInfo.Invoke(complexTypeObject, new object[] {
                        new Where[] {
                            new Where(result.Rows[0].Table.Columns[i].ColumnName, id)
                        }
                    });

                    pComplexType.SetValue(myObject, result.Rows[0].ItemArray[i] is DBNull ? null : objectValue, null);

                    #endregion
                }

                return myObject;
            }
            else
                return default(T);
        }

        public virtual List<T> Listar()
        {
            ErrorMessage = string.Empty;
            return Listar(null);
        }

        public virtual List<T> Listar(params Where[] where)
        {
            ErrorMessage = string.Empty;
            var sb = new StringBuilder();
            sb.Append("SELECT ");
            sb.Append(MakeSql(TipoQuery.Select));
            sb.AppendFormat("FROM {0} ", _tableName);
            if (where != null)
            {
                sb.Append("WHERE ");
                sb.Append(MakeWhere(where));
            }

            var msgError = "";
            Query = sb.ToString();
            var result = _database.ExecuteQuery(sb.ToString(), ref msgError, null);
            if (!string.IsNullOrWhiteSpace(msgError))
                ErrorMessage = msgError;

            var lstT = CreateList(typeof(T));

            if (result.Rows.Count <= 0)
                return (List<T>)lstT;

            foreach (DataRow dr in result.Rows)
            {
                var myObject = (T)Activator.CreateInstance(typeof(T));

                for (var i = 0; i < dr.Table.Columns.Count; i++)
                {
                    var prop = myObject.GetType().GetProperty(dr.Table.Columns[i].ColumnName, BindingFlags.Public | BindingFlags.Instance);
                    var sttr = GetAttrDbColumn(prop);
                    if (prop != null && prop.CanWrite)
                    {
                        if (dr.ItemArray[i] is DBNull)
                            prop.SetValue(myObject, null, null);
                        else
                        {
                            if (prop.PropertyType == typeof(bool))
                                prop.SetValue(myObject, dr.ItemArray[i].ToString() != "0", null);
                            else
                            {
                                try
                                {
                                    prop.SetValue(myObject, dr.ItemArray[i], null);
                                }
                                catch (Exception)
                                {
                                    // ignored
                                }
                            }
                        }
                    }

                    #region Carrega Complex Type

                    if (sttr == null || !sttr.ForeignKey)
                        continue;

                    var pName = string.IsNullOrWhiteSpace(sttr.DbForeignKey) ?
                        dr.Table.Columns[i].ColumnName.Replace("ID", "") :
                        sttr.DbForeignKey;

                    var pComplexType = myObject.GetType().GetProperty(pName, BindingFlags.Public | BindingFlags.Instance);
                    if (pComplexType == null || !pComplexType.CanWrite)
                        continue;

                    var id = ConvertType.ToInt(dr.ItemArray[i]);

                    var complexType = pComplexType.PropertyType;
                    var myObjectComplex = Activator.CreateInstance(pComplexType.PropertyType);
                    var magicConstructor = complexType.GetConstructor(Type.EmptyTypes);

                    if (myObjectComplex == null)
                        continue;

                    var complexTypeObject = magicConstructor.Invoke(new object[] { });

                    var methInfo = complexType.GetMethod("Consulta");

                    if (methInfo == null)
                        continue;

                    var objectValue = methInfo.Invoke(complexTypeObject, new object[]
                    {
                        new[]
                        {
                            new Where(dr.Table.Columns[i].ColumnName, id)
                        }
                    });

                    if (dr.ItemArray[i] is DBNull)
                        pComplexType.SetValue(myObject, null, null);
                    else
                        pComplexType.SetValue(myObject, objectValue, null);

                    #endregion
                }

                lstT.Add(myObject);
            }
            return (List<T>)lstT;
        }

        public virtual bool Gravar()
        {
            ErrorMessage = string.Empty;
            var sb = new StringBuilder();
            sb.AppendFormat("INSERT INTO {0} ", _tableName);
            sb.Append("(");
            sb.Append(MakeSql(TipoQuery.Insert));
            sb.Append(") ");
            sb.Append("VALUES(");

            var values = new List<string>();
            var props = GetProperties();

            foreach (var t in props)
            {
                var attrDbColumn = GetAttrDbColumn(t);
                if (attrDbColumn == null)
                    values.Add(FormatValue(t));
                else
                {
                    if (attrDbColumn.PrimaryKey)
                    {
                        if (attrDbColumn.Identity)
                            continue;
                        values.Add(FormatValue(t));
                    }
                    else if (attrDbColumn.ComplexType)
                        continue;
                    else if (attrDbColumn.ForeignKey)
                    {
                        values.Add(FormatValue(t));
                    }
                    else if (!attrDbColumn.Required)
                        continue;
                    else
                        values.Add(FormatValue(t));
                }
            }

            for (var i = 0; i < values.Count; i++)
                sb.AppendFormat("{0}{1}", values[i], i == (values.Count - 1) ? "" : ",");

            sb.Append(") ");

            var msgError = "";
            Query = sb.ToString();
            var result = _database.ExecuteCommand(sb.ToString(), ref msgError, null);
            if (!result)
                ErrorMessage = msgError;

            return result;
        }

        public virtual bool Alterar()
        {
            ErrorMessage = string.Empty;
            var sb = new StringBuilder();
            sb.AppendFormat("UPDATE {0} ", _tableName);
            sb.Append("SET ");
            sb.Append(MakeSql(TipoQuery.Update));

            sb.Append("WHERE ");
            sb.Append(GetWherePrimaryKey());

            var msgError = "";
            Query = sb.ToString();
            var result = _database.ExecuteCommand(sb.ToString(), ref msgError, null);
            if (!result)
                ErrorMessage = msgError;
            return result;
        }

        /// <summary>
        /// Altera Objeto
        /// </summary>
        /// <param name="where">Parametros</param>
        /// <returns></returns>
        public virtual bool Alterar(params Where[] where)
        {
            ErrorMessage = string.Empty;
            var sb = new StringBuilder();
            sb.AppendFormat("UPDATE {0} ", _tableName);
            sb.Append("SET ");
            sb.Append(MakeSql(TipoQuery.Update));

            if (where != null && where.Length > 0)
            {
                sb.Append("WHERE ");
                sb.Append(MakeWhere(where));
            }
            else
            {
                Query = sb.ToString();
                ErrorMessage = "Necessario instrução Where!";
                return false;
            }

            var msgError = "";
            Query = sb.ToString();
            var result = _database.ExecuteCommand(sb.ToString(), ref msgError, null);
            if (!result)
                ErrorMessage = msgError;
            return result;
        }

        /// <summary>
        /// Exclui informação do Objeto
        /// </summary>
        /// <param name="where">Parametros</param>
        /// <returns></returns>
        public virtual bool Excluir(params Where[] where)
        {
            ErrorMessage = string.Empty;
            var sb = new StringBuilder();
            sb.AppendFormat("DELETE FROM {0} ", _tableName);

            if (where.Length > 0)
            {
                sb.Append("WHERE ");
                sb.Append(MakeWhere(where));
            }
            else
            {
                if (_loadAttr && !_deleteAll)
                {
                    ErrorMessage = "Tabela configurada para não permitir deleteAll, por favor insirar Where(s)!";
                    return false;
                }
            }

            var msgError = "";
            Query = sb.ToString();

            var result = _database.ExecuteCommand(sb.ToString(), ref msgError, null);
            if (!result)
                ErrorMessage = msgError;
            return result;
        }

        /// <summary>
        /// Executa Consulta em Banco de Dados
        /// </summary>
        /// <param name="consulta">Query Sql</param>
        /// <returns>DataTable</returns>
        public DataTable ConsultaDataTable(string consulta)
        {
            var msgError = "";
            var result = _database.ExecuteQuery(consulta, ref msgError, null);
            if (result.Rows.Count == 0)
                ErrorMessage = msgError;
            return result;
        }

        #endregion

        #region Utils

        /// <summary>
        /// Constroi Intrução Sql
        /// </summary>
        /// <param name="tipoQuery">Tipo de Intrução</param>
        /// <returns></returns>
        private string MakeSql(TipoQuery tipoQuery)
        {
            var fields = new List<string>();
            var fieldProp = new List<PropertyInfo>();

            var sb = new StringBuilder();

            var properties = GetProperties();
            foreach (var prop in properties)
            {
                var attrDbColumn = GetAttrDbColumn(prop);

                switch (tipoQuery)
                {
                    case TipoQuery.Select:
                        #region Select

                        if (attrDbColumn != null)
                        {
                            if (attrDbColumn.ComplexType)
                                break;
                            if (attrDbColumn.List == false)
                                break;
                        }

                        fields.Add(prop.Name);

                        #endregion
                        break;
                    case TipoQuery.Insert:
                        #region Insert

                        if (attrDbColumn == null)
                            fields.Add(prop.Name);
                        else
                        {
                            if (attrDbColumn.PrimaryKey)
                            {
                                if (attrDbColumn.Identity)
                                    break;
                                fields.Add(prop.Name);
                            }
                            else if (attrDbColumn.ForeignKey)
                                fields.Add(prop.Name);
                            else if (attrDbColumn.ComplexType)
                                /*
                                {
                                    fields.Add(prop.Name + "ID");
                                    break;
                                }*/
                                break;
                            else if (attrDbColumn.Required == false)
                                break;
                            else
                                fields.Add(prop.Name);
                        }

                        #endregion
                        break;
                    case TipoQuery.Update:
                        #region Update
                        if (attrDbColumn != null)
                        {
                            if (attrDbColumn.PrimaryKey)
                                continue;
                            if (attrDbColumn.ComplexType)
                                continue;
                            fieldProp.Add(prop);
                        }
                        else
                            fieldProp.Add(prop);
                        #endregion
                        break;
                    case TipoQuery.Delete:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(tipoQuery), tipoQuery, null);
                }
            }

            switch (tipoQuery)
            {
                case TipoQuery.Select:
                case TipoQuery.Insert:
                    for (var i = 0; i < fields.Count; i++)
                        sb.AppendFormat("{0}{1} ", fields[i], i == (fields.Count - 1) ? "" : ",");
                    break;
                case TipoQuery.Update:
                    for (var i = 0; i < fieldProp.Count; i++)
                        sb.AppendFormat("{0}={1}{2}", fieldProp[i].Name, FormatValue(fieldProp[i]), i == (fieldProp.Count - 1) ? " " : ",");
                    break;
                case TipoQuery.Delete:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tipoQuery), tipoQuery, null);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Constroi instrução Where
        /// </summary>
        /// <param name="where">Lista de Parametros</param>
        /// <returns></returns>
        private static string MakeWhere(params Where[] where)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < where.Length; i++)
            {
                if (i != 0)
                    sb.Append(" AND ");

                switch (where[i].Operador)
                {
                    case Operador.AfterLike:
                        sb.AppendFormat("{0} LIKE '{1}%'", where[i].Campo, where[i].Valor);
                        break;
                    case Operador.BehindLike:
                        sb.AppendFormat("{0} LIKE '%{1}'", where[i].Campo, where[i].Valor);
                        break;
                    case Operador.Like:
                        sb.AppendFormat("{0} LIKE '%{1}%'", where[i].Campo, where[i].Valor);
                        break;
                    default:
                        sb.AppendFormat("{0} {1} {2}", where[i].Campo, ConvertOperador(where[i].Operador), FormatValue(where[i].Valor));
                        break;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Pega Nome da Tabela
        /// </summary>
        /// <returns>Nome da Tabela</returns>
        private static string GetNameDb()
        {
            var dbTable = "";
            var attrs = Attribute.GetCustomAttributes(typeof(T));
            foreach (var attr in attrs.OfType<DbTable>())
            {
                dbTable = (attr).TableName;
                break;
            }
            if (string.IsNullOrWhiteSpace(dbTable))
                dbTable = typeof(T).Name;
            return dbTable;
        }

        /// <summary>
        /// Pega Valor de Where PrimaryKey
        /// </summary>
        /// <returns></returns>
        private string GetWherePrimaryKey()
        {
            var primaryKey = "";

            var prop = GetProperties();

            for (int index = 0; index < prop.Length; index++)
            {
                var t = prop[index];
                var attr = GetAttrDbColumn(t);

                if (attr == null || !attr.PrimaryKey)
                    continue;

                if (primaryKey != string.Empty)
                    primaryKey += " AND ";
                primaryKey += $"{t.Name} = {FormatValue(t)}";
            }

            return primaryKey;
        }

        /// <summary>
        /// Verifica se Tabela Permite DeleteAll
        /// </summary>
        /// <returns></returns>
        private bool AllowDeleteAll()
        {
            var deleteAll = false;
            var attrs = Attribute.GetCustomAttributes(typeof(T));
            foreach (var a in attrs.OfType<DbTable>())
                deleteAll = a.DeleteAll;
            return deleteAll;
        }

        /// <summary>
        /// Cria Lista de Objeto
        /// </summary>
        /// <param name="t">Tipo de Lista</param>
        /// <returns></returns>
        private static IList CreateList(Type t)
        {
            var listType = typeof(List<>);
            var constructedListType = listType.MakeGenericType(t);
            var instance = Activator.CreateInstance(constructedListType);
            return (IList)instance;
        }

        /// <summary>
        /// Pega Atributos da Property
        /// </summary>
        /// <param name="propertyInfo">Property</param>
        /// <returns></returns>
        private static DbColumn GetAttrDbColumn(PropertyInfo propertyInfo)
        {
            var cAttrs = propertyInfo.GetCustomAttributes(false);
            DbColumn attrDbColumn = null;
            foreach (var attr in cAttrs)
                attrDbColumn = attr as DbColumn;
            return attrDbColumn;
        }

        /// <summary>
        /// Pega Atributos da Classe Tabela
        /// </summary>
        /// <returns></returns>
        private static DbTable GetAttrDbTable()
        {
            DbTable dbTable = null;
            var attrs = Attribute.GetCustomAttributes(typeof(T));
            foreach (var attr in attrs.OfType<DbTable>())
                dbTable = attr;
            return dbTable;
        }

        public static Dictionary<string, object> GetPropertyAttributes(PropertyInfo property)
        {
            var attribs = new Dictionary<string, object>();

            foreach (var attribData in property.GetCustomAttributesData())
            {
                if (attribData.ConstructorArguments.Count != 1)
                    continue;

                var typeName = attribData.Constructor.DeclaringType.Name;
                if (typeName.EndsWith("Attribute")) typeName = typeName.Substring(0, typeName.Length - 9);
                attribs[typeName] = attribData.ConstructorArguments[0].Value;
            }
            return attribs;
        }

        private static PropertyInfo[] GetProperties()
        {
            return typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(c => c.Name != "Query" && c.Name != "ErrorMessage").ToArray();
        }

        private PropertyInfo[] GetProperties<T>()
        {
            return typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(c => c.Name != "Query" && c.Name != "ErrorMessage").ToArray();
        }

        private string FormatValue(PropertyInfo property)
        {
            if (property.PropertyType == typeof(short) || property.PropertyType == typeof(int) || property.PropertyType == typeof(long))
                return property.GetValue(this, null).ToString();

            if (property.PropertyType == typeof(double))
                return property.GetValue(this, null).ToString().Replace(",", ".");

            if (property.PropertyType == typeof(string))
                return $"'{property.GetValue(this, null)}'";

            if (property.PropertyType == typeof(char))
                return $"'{property.GetValue(this, null)}'";

            if (property.PropertyType == typeof(bool))
            {
                var value = (bool)property.GetValue(this, null);
                return $"{(value ? 1 : 0)}";
            }

            if (property.PropertyType == typeof(DateTime))
            {
                var dt = Convert.ToDateTime(property.GetValue(this, null));
                //return string.Format("'{0:yyyy-MM-dd HH:mm:ss}'", dt);
                //return string.Format("'{0:yyyy-MM-dd}'", dt);
                return $"'{dt:yyyy-dd-MM}'";
            }

            if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var v = property.GetValue(this, null);
                return v?.ToString() ?? "null";
            }

            if (property.PropertyType.IsEnum)
                return property.GetValue(this, null).ToString();

            if (property.PropertyType != typeof(byte[]))
                return "";

            try
            {
                var value = (byte[])property.GetValue(this, null);
                if (value == null)
                    return "null";

                var conValue = $"0x{BitConverter.ToString(value).Replace("-", "")}";
                //var conValue = string.Format("{0}", BitConverter.ToString(value).Replace("-", ""));
                return conValue;
            }
            catch (Exception)
            {
                return "null";
            }
        }

        private static string FormatValue(object obj)
        {
            if (obj is short || obj is int || obj is long)
                return obj.ToString();

            if (obj is double)
                return obj.ToString().Replace(",", ".");

            if (obj is string)
                return $"'{obj}'";

            if (obj is char)
                return $"'{obj}'";

            if (obj is bool)
            {
                var value = (bool)obj;
                return $"{(value ? 1 : 0)}";
            }

            if (!(obj is DateTime))
                return "";

            var dt = Convert.ToDateTime(obj);
            return $"'{dt:yyyy-MM-dd HH:mm:ss}'";
        }

        private static string ConvertOperador(Operador operador)
        {
            switch (operador)
            {
                case Operador.Igual:
                    return "=";
                case Operador.Diferente:
                    return "<>";
                case Operador.MaiorIgual:
                    return ">=";
                case Operador.MenorIgual:
                    return "<=";
                case Operador.BehindLike:
                case Operador.Like:
                case Operador.AfterLike:
                    return "";
                default:
                    return "";
            }
        }

        #endregion
    }
}

