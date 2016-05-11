using DataAccessFW.Model;

namespace DataAccessFW.Service
{
    public sealed class DbAccess
    {
        public static string ConnectionString { get; set; }
        public static Provider DataBase { get; set; }

        private readonly Database _database;

        private static volatile DbAccess _instance;
        private static readonly object SyncRoot = new object();

        private DbAccess()
        {
            _database = new Database(ConnectionString, DataBase);
        }

        public static DbAccess Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;
                lock (SyncRoot)
                {
                    if (_instance == null)
                        _instance = new DbAccess();
                }
                return _instance;
            }
        }

        public Database GetDatabase()
        {
            return _database;
        }
    }
}
