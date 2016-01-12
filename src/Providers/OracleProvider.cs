using DataAccessFW.Interface;
using System;
using System.Data;
using System.Data.OracleClient;

namespace DataAccessFW.Providers
{
    internal class OracleProvider : DbProvider
    {
        private OracleConnection _conn;
        private OracleTransaction _tran;

        public OracleProvider(string connectionString)
            : base(connectionString)
        {
            _conn = new OracleConnection(connectionString);
        }

        public override bool ExecuteCommand(string query, ref string msgErro, params System.Data.Common.DbParameter[] parametros)
        {
            bool exec = false;
            using (_conn)
            {
                OracleCommand command = new OracleCommand(query, _conn);

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

        public override DataTable ExecuteQuery(string query, ref string msgErro, params System.Data.Common.DbParameter[] parametros)
        {
            DataTable dt = new DataTable();

            if (string.IsNullOrWhiteSpace(this.ConnectionString))
                throw new Exception("ConnectionString vazia!");

            using (_conn)
            {
                OracleDataAdapter da = new OracleDataAdapter(query, _conn);

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
            catch (OracleException ex)
            {
                Erro = ex.Message;
            }
        }
    }
}
