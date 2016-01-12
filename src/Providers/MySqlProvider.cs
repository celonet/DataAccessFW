using DataAccessFW.Interface;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Text;

namespace DataAccessFW.Providers
{
    internal class MySqlProvider : DbProvider
    {
        private MySqlConnection _conn;
        private MySqlTransaction _tran;

        public MySqlProvider(string connectionString)
            : base(connectionString)
        {
            _conn = new MySqlConnection(connectionString);
        }

        public override bool ExecuteCommand(string query, ref string msgErro, params System.Data.Common.DbParameter[] parametros)
        {
            bool exec = false;
            using (MySqlConnection conn = new MySqlConnection(this.ConnectionString))
            {
                MySqlCommand command = new MySqlCommand(query, conn);

                if (parametros != null)
                {
                    foreach (var param in parametros)
                        command.Parameters.Add(param);
                }

                try
                {
                    conn.Open();
                    command.ExecuteNonQuery();
                    exec = true;
                }
                catch (Exception ex)
                {
                    msgErro = ex.Message;
                    msgErro += ex.StackTrace;
                    if(ex.InnerException != null)
                        msgErro += ex.InnerException.Message;
                }
            }
            return exec;
        }

        public override DataTable ExecuteQuery(string query, ref string msgErro, params System.Data.Common.DbParameter[] parametros)
        {
            StringBuilder sb = new StringBuilder();

            DataTable dt = new DataTable();

            if (string.IsNullOrWhiteSpace(this.ConnectionString))
                throw new Exception("ConnectionString vazia!");

            using (MySqlConnection conn = new MySqlConnection(this.ConnectionString))
            {
                MySqlDataAdapter da = new MySqlDataAdapter(query, conn);

                if (parametros != null)
                {
                    foreach (var param in parametros)
                        da.SelectCommand.Parameters.Add(param);
                }

                try
                {
                    sb.Append("Open Conn ");
                    conn.Open();
                    sb.Append("Fill");
                    da.Fill(dt);
                }
                catch (Exception ex)
                {
                    
                    sb.Append("Message: " + ex.Message);
                    sb.Append("StackTrace: " + ex.StackTrace);
                    if (ex.InnerException != null)
                    {
                        sb.Append("InnerException");
                        sb.Append("Message: " + ex.InnerException.Message);
                        sb.Append("StackTrace: " + ex.InnerException.StackTrace);
                    }
                    msgErro = sb.ToString();
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
            catch (MySqlException ex)
            {
                Erro = ex.Message;
                Erro += ex.StackTrace;
                if (ex.InnerException != null)
                    Erro += ex.InnerException.Message;
            }
        }
    }
}
