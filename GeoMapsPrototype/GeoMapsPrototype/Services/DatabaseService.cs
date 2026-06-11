using SQLite;
using GraTerenowa.Models;

namespace GraTerenowa.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection? _db;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public async Task<SQLiteAsyncConnection> GetDbAsync()
    {
        // Blokada na wypadek równoczesnych wywołań przy starcie
        await _initLock.WaitAsync();
        try
        {
            if (_db is not null) return _db;

            var path = Path.Combine(
                FileSystem.AppDataDirectory, "gra_terenowa.db3");

            _db = new SQLiteAsyncConnection(path,
                SQLiteOpenFlags.ReadWrite |
                SQLiteOpenFlags.Create |
                SQLiteOpenFlags.SharedCache);

            await CreateTablesAsync();
            return _db;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task CreateTablesAsync()
    {
        await _db!.CreateTableAsync<LocationSet>();
        await _db!.CreateTableAsync<LocationTask>();
        await _db!.CreateTableAsync<TaskCompletion>();
    }
}