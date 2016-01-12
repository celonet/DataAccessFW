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

        private Database _database;
        private bool _loadAttr;
        private string _tableName;
        private bool _deleteAll;

        public string Query { get; set; }

        public string ErrorMessage { get; set; }

        #endregion

        public DbBase()
        {
            _database = DbAccess.Instance.GetDatabase();
            var sttrDB = GetAttrDbTable();
            if (sttrDB != null)
            {
                _loadAttr = true;
                _tableName = sttrDB.TableName ?? GetNameDb();
                _deleteAll = sttrDB.DeleteAll;
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
            StringBuilder sb = new StringBuilder();
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
                this.Query = sb.ToString();
                this.ErrorMessage = "Necessario instrução Where!";
                return default(T);
            }

            string msgError = "";
            this.Query = sb.ToString();
            var result = _database.ExecuteQuery(sb.ToString(), ref msgError, null);
            if (result.Rows.Count == 0)
                ErrorMessage = msgError;

            if (result.Rows.Count > 0)
            {
                var myObject = (T)Activator.CreateInstance(typeof(T));

                for (int i = 0; i < result.Columns.Count; i++)
                {
                    PropertyInfo prop = myObject.GetType().GetProperty(result.Columns[i].ColumnName, BindingFlags.Public | BindingFlags.Instance);
                    var sttr = GetAttrDbColumn(prop);
                    if (null != prop && prop.CanWrite)
                    {
                        if (result.Rows[0].ItemArray[i].GetType() == typeof(DBNull))
                            continue;
                        try
                        {
                            prop.SetValue(myObject, result.Rows[0].ItemArray[i], null);
                        }
                        catch (Exception) { }
                    }

                    #region Carrega ComplexType

                    if (sttr != null && sttr.ForeignKey)
                    {
                        string pName = "";
                        if (string.IsNullOrWhiteSpace(sttr.DbForeignKey))
                            pName = result.Rows[0].Table.Columns[i].ColumnName.Replace("ID", "");
                        else
                            pName = sttr.DbForeignKey;

                        PropertyInfo pComplexType = myObject.GetType().GetProperty(pName, BindingFlags.Public | BindingFlags.Instance);
                        if (pComplexType != null && pComplexType.CanWrite)
                        {
                            var id = ConvertType.ToInt(result.Rows[0].ItemArray[i]);

                            Type complexType = pComplexType.PropertyType;
                            var myObjectComplex = Activator.CreateInstance(pComplexType.PropertyType);
                            ConstructorInfo magicConstructor = complexType.GetConstructor(Type.EmptyTypes);
                            if (myObjectComplex != null)
                            {
                                object complexTypeObject = magicConstructor.Invoke(new object[] { });

                                MethodInfo methInfo = complexType.GetMethod("Consulta");
                                if (methInfo != null)
                                {
                                    var objectValue = methInfo.Invoke(complexTypeObject, new object[] { 
                                        new Where[] { 
                                            new Where(result.Rows[0].Table.Columns[i].ColumnName, id) 
                                        } 
                                    });

                                    if (result.Rows[0].ItemArray[i].GetType() == typeof(System.DBNull))
                                        pComplexType.SetValue(myObject, null, null);
                                    else
                                        pComplexType.SetValue(myObject, objectValue, null);
                                }
                            }
                        }
                    }

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
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT ");
            sb.Append(MakeSql(TipoQuery.Select));
            sb.AppendFormat("FROM {0} ", _tableName);
            if (where != null)
            {
                sb.Append("WHERE ");
                sb.Append(MakeWhere(where));
            }

            string msgError = "";
            this.Query = sb.ToString();
            var result = _database.ExecuteQuery(sb.ToString(), ref msgError, null);
            if (!string.IsNullOrWhiteSpace(msgError))
                ErrorMessage = msgError;

            var lstT = CreateList(typeof(T));

            if (result.Rows.Count > 0)
            {
                foreach (DataRow dr in result.Rows)
                {
                    var myObject = (T)Activator.CreateInstance(typeof(T));

                    for (int i = 0; i < dr.Table.Columns.Count; i++)
                    {
                        PropertyInfo prop = myObject.GetType().GetProperty(dr.Table.Columns[i].ColumnName, BindingFlags.Public | BindingFlags.Instance);
                        var sttr = GetAttrDbColumn(prop);
                        if (prop != null && prop.CanWrite)
                        {
                            if (dr.ItemArray[i].GetType() == typeof(System.DBNull))
                                prop.SetValue(myObject, null, null);
                            else
                            {
                                if (prop.PropertyType == typeof(Boolean))
                                {
                                    if (dr.ItemArray[i].ToString() == "0")
                                        prop.SetValue(myObject, false, null);
                                    else
                                        prop.SetValue(myObject, true, null);
                                }
                                else
                                {
                                    //if(_database.Provider == Provider.MySql)
                                    try
                                    {
                                        prop.SetValue(myObject, dr.ItemArray[i], null);
                                    }
                                    catch (Exception) { }
                                }
                            }
                        }

                        #region Carrega Complex Type

                        if (sttr != null && sttr.ForeignKey)
                        {
                            string pName = "";
                            if (string.IsNullOrWhiteSpace(sttr.DbForeignKey))
                                pName = dr.Table.Columns[i].ColumnName.Replace("ID", "");
                            else
                                pName = sttr.DbForeignKey;
                            PropertyInfo pComplexType = myObject.GetType().GetProperty(pName, BindingFlags.Public | BindingFlags.Instance);
                            if (pComplexType != null && pComplexType.CanWrite)
                            {
                                var id = ConvertType.ToInt(dr.ItemArray[i]);

                                Type complexType = pComplexType.PropertyType;
                                var myObjectComplex = Activator.CreateInstance(pComplexType.PropertyType);
                                ConstructorInfo magicConstructor = complexType.GetConstructor(Type.EmptyTypes);
                                if (myObjectComplex != null)
                                {
                                    object complexTypeObject = magicConstructor.Invoke(new object[] { });

                                    MethodInfo methInfo = complexType.GetMethod("Consulta");
                                    if (methInfo != null)
                                    {
                                        var objectValue = methInfo.Invoke(complexTypeObject, new object[] { new Where[] { new Where(dr.Table.Columns[i].ColumnName, id) } });

                                        if (dr.ItemArray[i].GetType() == typeof(System.DBNull))
                                            pComplexType.SetValue(myObject, null, null);
                                        else
                                            pComplexType.SetValue(myObject, objectValue, null);
                                    }
                                }
                            }
                        }

                        #endregion
                    }

                    lstT.Add(myObject);
                }
            }
            return (List<T>)lstT;
        }

        public virtual bool Gravar()
        {
            ErrorMessage = string.Empty;
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("INSERT INTO {0} ", _tableName);
            sb.Append("(");
            sb.Append(MakeSql(TipoQuery.Insert));
            sb.Append(") ");
            sb.Append("VALUES(");

            List<string> values = new List<string>();
            var props = GetProperties();
            for (int i = 0; i < props.Length; i++)
            {
                var attrDbColumn = GetAttrDbColumn(props[i]);
                if (attrDbColumn == null)
                    values.Add(FormatValue(props[i]));
                else
                {
                    if (attrDbColumn.PrimaryKey)
                    {
                        if (attrDbColumn.Identity)
                            continue;
                        else
                            values.Add(FormatValue(props[i]));
                    }
                    else if (attrDbColumn.ComplexType)
                        continue;
                    else if (attrDbColumn.ForeignKey)
                    {
                        values.Add(FormatValue(props[i]));
                        continue;
                    }
                    else if (!attrDbColumn.Required)
                        continue;
                    else
                        values.Add(FormatValue(props[i]));
                }
            }

            for (int i = 0; i < values.Count; i++)
                sb.AppendFormat("{0}{1}", values[i], i == (values.Count - 1) ? "" : ",");

            sb.Append(") ");

            string msgError = "";
            Query = sb.ToString();
            var result = _database.ExecuteCommand(sb.ToString(), ref msgError, null);
            if (!result)
                ErrorMessage = msgError;
            return result;
        }

        public virtual bool Alterar()
        {
            ErrorMessage = string.Empty;
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("UPDATE {0} ", _tableName);
            sb.Append("SET ");
            sb.Append(MakeSql(TipoQuery.Update));

            sb.Append("WHERE ");
            sb.Append(GetWherePrimaryKey());

            string msgError = "";
            this.Query = sb.ToString();
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
            StringBuilder sb = new StringBuilder();
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
                this.Query = sb.ToString();
                this.ErrorMessage = "Necessario instrução Where!";
                return false;
            }

            string msgError = "";
            this.Query = sb.ToString();
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
            StringBuilder sb = new StringBuilder();
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

            string msgError = "";
            this.Query = sb.ToString();

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
            string msgError = "";
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
            List<String> fields = new List<string>();
            List<PropertyInfo> fieldProp = new List<PropertyInfo>();

            StringBuilder sb = new StringBuilder();

            var properties = GetProperties();
            for (int i = 0; i < properties.Length; i++)
            {
                var prop = properties[i];
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
                                if (attrDbColumn.Identity == true)
                                    break;
                                else
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
                }
            }

            switch (tipoQuery)
            {
                case TipoQuery.Select:
                case TipoQuery.Insert:
                    for (int i = 0; i < fields.Count; i++)
                        sb.AppendFormat("{0}{1} ", fields[i], i == (fields.Count - 1) ? "" : ",");
                    break;
                case TipoQuery.Update:
                    for (int i = 0; i < fieldProp.Count; i++)
                        sb.AppendFormat("{0}={1}{2}", fieldProp[i].Name, FormatValue(fieldProp[i]), i == (fieldProp.Count - 1) ? " " : ",");
                    break;
            }

            fields = null;
            fieldProp = null;
            return sb.ToString();
        }

        /// <summary>
        /// Constroi instrução Where
        /// </summary>
        /// <param name="where">Lista de Parametros</param>
        /// <returns></returns>
        private string MakeWhere(params Where[] where)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < where.Length; i++)
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
        private string GetNameDb()
        {
            string dbTable = "";
            System.Attribute[] attrs = System.Attribute.GetCustomAttributes(typeof(T));
            foreach (System.Attribute attr in attrs)
            {
                if (attr is DbTable)
                {
                    dbTable = ((DbTable)attr).TableName;
                    break;
                }
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
            string primaryKey = "";

            var prop = GetProperties();

            for (int i = 0; i < prop.Length; i++)
            {
                var attr = GetAttrDbColumn(prop[i]);

                if (attr != null && attr.PrimaryKey)
                {
                    if (primaryKey != string.Empty)
                        primaryKey += " AND ";
                    primaryKey += string.Format("{0} = {1}", prop[i].Name, FormatValue(prop[i]));
                }
            }

            return primaryKey;
        }

        /// <summary>
        /// Verifica se Tabela Permite DeleteAll
        /// </summary>
        /// <returns></returns>
        private bool AllowDeleteAll()
        {
            bool deleteAll = false;
            System.Attribute[] attrs = System.Attribute.GetCustomAttributes(typeof(T));
            foreach (System.Attribute attr in attrs)
            {
                if (attr is DbTable)
                {
                    DbTable a = (DbTable)attr;
                    deleteAll = a.DeleteAll;
                }
            }
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
        private DbColumn GetAttrDbColumn(PropertyInfo propertyInfo)
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
        private DbTable GetAttrDbTable()
        {
            DbTable dbTable = null;
            System.Attribute[] attrs = System.Attribute.GetCustomAttributes(typeof(T));
            foreach (System.Attribute attr in attrs)
            {
                if (attr is DbTable)
                    dbTable = (DbTable)attr;
            }
            return dbTable;
        }

        public static Dictionary<string, object> GetPropertyAttributes(PropertyInfo property)
        {
            Dictionary<string, object> attribs = new Dictionary<string, object>();

            foreach (CustomAttributeData attribData in property.GetCustomAttributesData())
            {
                if (attribData.ConstructorArguments.Count == 1)
                {
                    string typeName = attribData.Constructor.DeclaringType.Name;
                    if (typeName.EndsWith("Attribute")) typeName = typeName.Substring(0, typeName.Length - 9);
                    attribs[typeName] = attribData.ConstructorArguments[0].Value;
                }
            }
            return attribs;
        }

        private PropertyInfo[] GetProperties()
        {
            return typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(c => c.Name != "Query" && c.Name != "ErrorMessage").ToArray();
        }

        private PropertyInfo[] GetProperties<T>()
        {
            return typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(c => c.Name != "Query" && c.Name != "ErrorMessage").ToArray();
        }

        private string FormatValue(PropertyInfo property)
        {
            if (property.PropertyType == typeof(Int16) || property.PropertyType == typeof(Int32) || property.PropertyType == typeof(Int64))
                return property.GetValue(this, null).ToString();
            else if (property.PropertyType == typeof(double))
                return property.GetValue(this, null).ToString().Replace(",", ".");
            else if (property.PropertyType == typeof(string))
                return string.Format("'{0}'", property.GetValue(this, null));
            else if (property.PropertyType == typeof(char))
                return string.Format("'{0}'", property.GetValue(this, null));
            else if (property.PropertyType == typeof(bool))
            {
                var value = (bool)property.GetValue(this, null);
                return string.Format("{0}", value ? 1 : 0);
            }
            else if (property.PropertyType == typeof(DateTime))
            {
                var dt = Convert.ToDateTime(property.GetValue(this, null));
                //return string.Format("'{0:yyyy-MM-dd HH:mm:ss}'", dt);
                //return string.Format("'{0:yyyy-MM-dd}'", dt);
                return string.Format("'{0:yyyy-dd-MM}'", dt);
            }
            else if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var v = property.GetValue(this, null);
                if (v == null)
                    return "null";
                else
                    return v.ToString(); ;
            }
            else if (property.PropertyType.IsEnum)
                return property.GetValue(this, null).ToString();
            else if (property.PropertyType == typeof(byte[]))
            {
                try
                {
                    var value = (Byte[])property.GetValue(this, null);
                    if (value == null)
                        return "null";
                    else
                    {
                        var conValue = string.Format("0x{0}", BitConverter.ToString(value).Replace("-", ""));
                        //var conValue = string.Format("{0}", BitConverter.ToString(value).Replace("-", ""));
                        return conValue;
                    }
                }
                catch (Exception)
                {
                    return "null";
                }
            }
            else
                return "";
        }

        private string FormatValue(object obj)
        {
            if (obj is Int16 || obj is Int32 || obj is Int64)
                return obj.ToString();
            else if (obj is double)
                return obj.ToString().Replace(",", ".");
            else if (obj is string)
                return string.Format("'{0}'", obj);
            else if (obj is char)
                return string.Format("'{0}'", obj);
            else if (obj is bool)
            {
                var value = (bool)obj;
                return string.Format("{0}", value ? 1 : 0);
            }
            else if (obj is DateTime)
            {
                var dt = Convert.ToDateTime(obj);
                return string.Format("'{0:yyyy-MM-dd HH:mm:ss}'", dt);
            }
            else
                return "";
        }

        private string ConvertOperador(Operador operador)
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

