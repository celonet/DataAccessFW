using DataAccessFW.Interface;
using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace DataAccessFW.Providers
{
    /// <summary>
    /// Provider Construido para SqlServer
    /// </summary>
    internal sealed class SqlServerProvider : DbProvider
    {
        private readonly SqlConnection _conn;
        private SqlTransaction _tran;

        public SqlServerProvider(string connectionString)
            : base(connectionString)
        {
            _conn = new SqlConnection(connectionString);
        }

        public override void IniciaTransacao()
        {
            if (_conn != null)
                _tran = _conn.BeginTransaction();
        }

        public override void FinalizaTransacao(bool commit)
        {
            if (_conn == null) return;
            if (_tran == null) return;

            try
            {
                if (commit)
                    _tran.Commit();
                else
                    _tran.Rollback();
            }
            catch (InvalidOperationException ey)
            {
                Erro = ey.Message;
            }
            catch (SqlException ex)
            {
                Erro = ex.Message;
            }
        }

        public override bool ExecuteCommand(string query, ref string msgErro, params DbParameter[] parametros)
        {
            var exec = false;
            using (_conn)
            {
                var command = new SqlCommand(query, _conn);

                if (parametros != null)
                {
                    foreach (var param in parametros)
                        command.Parameters.Add(param);
                }

                try
                {
                    _conn.Open();
                    command.ExecuteNonQuery();
                    exec = true;
                }
                catch (Exception ex)
                {
                    msgErro = ex.Message;
                }
            }
            return exec;
        }

        public override DataTable ExecuteQuery(string query, ref string msgErro, params DbParameter[] parametros)
        {
            var dt = new DataTable();

            if (string.IsNullOrWhiteSpace(this.ConnectionString))
                throw new Exception("ConnectionString vazia!");

            using (_conn)
            {
                var da = new SqlDataAdapter(query, _conn);

                if (parametros != null)
                {
                    foreach (var param in parametros)
                        da.SelectCommand.Parameters.Add(param);
                }

                try
                {
                    _conn.Open();
                    da.Fill(dt);
                }
                catch (Exception ex)
                {
                    msgErro = ex.Message;
                }
            }
            return dt;
        }
    }
}
