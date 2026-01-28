using SQLite;
using System.Diagnostics;

namespace Journal.Services
{
    public abstract class DatabaseService
    {
        protected readonly SQLiteAsyncConnection _db;

        protected DatabaseService()
        {
#if WINDOWS
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var dbPath = Path.Combine(desktopPath, "Journal.db3");
#else
            // Fallback for other platforms
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "Journal.db3");
#endif

            Debug.WriteLine($"DB PATH: {dbPath}");

            _db = new SQLiteAsyncConnection(dbPath);
        }
    }
}
