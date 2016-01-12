using DataAccessFW.Model;
using System;

namespace DataAccessFW.Service
{
    public sealed class DbAccess
    {
        public static string ConnectionString { get; set; }
        public static Provider DataBase { get; set; }

        private Database database;

        private static volatile DbAccess _instance;
        private static object syncRoot = new Object();

        private DbAccess()
        {
            database = new Database(ConnectionString, DataBase);
        }

        public static DbAccess Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (syncRoot)
                    {
                        if (_instance == null)
                            _instance = new DbAccess();
                    }
                }
                return _instance;
            }
        }

        public Database GetDatabase()
        {
            return database;
        }
    }
}
