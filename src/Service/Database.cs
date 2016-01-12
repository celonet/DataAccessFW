using DataAccessFW.Interface;
using DataAccessFW.Model;
using DataAccessFW.Providers;
using System;
using System.Data;
using System.Data.Common;

namespace DataAccessFW.Service
{
    public class Database
    {
        public string ConnectionString;
        public Provider Provider { get; set; }

        public Database() { }

        public Database(string connectionString, Provider provider)
        {
            ConnectionString = connectionString;
            Provider = provider;
        }

        public bool ExecuteCommand(string query, ref string msgErro, params DbParameter[] parametros)
        {
            DbProvider provider = GetProvider();
            provider.ConnectionString = this.ConnectionString;
            return provider.ExecuteCommand(query, ref msgErro, parametros);
        }

        public DataTable ExecuteQuery(string query, ref string msgErro, params DbParameter[] parametros)
        {
            DbProvider provider = GetProvider();
            provider.ConnectionString = this.ConnectionString;
            return provider.ExecuteQuery(query, ref msgErro, parametros);
        }

        public DbProvider GetProvider()
        {
            switch (Provider)
            {
                case Provider.SqlServer:
                    return new SqlServerProvider(ConnectionString);
                case Provider.MySql:
                    return new MySqlProvider(ConnectionString);
                case Provider.Oracle:
                    return new OracleProvider(ConnectionString);
                default:
                    throw new Exception("Provider não implementado!");
            }
        }
    }
}
