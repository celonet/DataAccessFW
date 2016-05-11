using System;
using System.Data;
using System.Data.Common;

namespace DataAccessFW.Interface
{
    /// <summary>
    /// Classe Base de Provider
    /// </summary>
    public abstract class DbProvider
    {
        public string Erro;

        /// <summary>
        /// ConnectionString do Banco
        /// </summary>
        public string ConnectionString { get; set; }

        protected DbProvider(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new Exception("Verifique ConnectionString");
            ConnectionString = connectionString;
        }

        public abstract void IniciaTransacao();

        public abstract void FinalizaTransacao(bool commit);

        /// <summary>
        /// Executa Comandos Sql(Insert, Delete ou Update)
        /// </summary>
        /// <param name="query">Comando</param>
        /// <param name="msgErro">Caso houver este será o retorno do erro</param>
        /// <param name="parametros">parametros para query Sql</param>
        /// <returns></returns>
        public abstract bool ExecuteCommand(string query, ref string msgErro, params DbParameter[] parametros);

        /// <summary>
        /// Executa Consultas a Base de Dados
        /// </summary>
        /// <param name="query">Consulta</param>
        /// <param name="msgErro">Caso houver este será o retorno do erro</param>
        /// <param name="parametros">parametros para query Sql</param>
        /// <returns></returns>
        public abstract DataTable ExecuteQuery(string query, ref string msgErro, params DbParameter[] parametros);
    }
}
