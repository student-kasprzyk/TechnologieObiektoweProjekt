using GraTerenowa.Models;

namespace GraTerenowa.Services;

public class LocationSetService
{
    private readonly DatabaseService _db;

    public LocationSetService(DatabaseService db)
    {
        _db = db;
    }

    public async Task<List<LocationSet>> GetAllAsync()
    {
        var db = await _db.GetDbAsync();
        var sets = await db.Table<LocationSet>().ToListAsync();
        var tasks = await db.Table<LocationTask>().ToListAsync();

        var countMap = tasks
            .GroupBy(t => t.LocationSetId)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var set in sets)
            set.LocationCount = countMap.GetValueOrDefault(set.Id, 0);

        return sets;
    }

    public async Task<LocationSet?> GetByIdAsync(int id)
    {
        var db = await _db.GetDbAsync();
        return await db.Table<LocationSet>()
                       .Where(s => s.Id == id)
                       .FirstOrDefaultAsync();
    }

    public async Task<int> SaveAsync(LocationSet set)
    {
        var db = await _db.GetDbAsync();

        if (set.Id == 0)
            return await db.InsertAsync(set);

        await db.UpdateAsync(set);
        return set.Id;
    }

    public async Task DeleteAsync(int id)
    {
        var db = await _db.GetDbAsync();
        var tasks = await db.Table<LocationTask>()
                            .Where(t => t.LocationSetId == id)
                            .ToListAsync();

        foreach (var task in tasks)
        {
            await db.Table<TaskCompletion>()
                    .Where(c => c.TaskId == task.Id)
                    .DeleteAsync();
        }

        await db.Table<LocationTask>()
                .Where(t => t.LocationSetId == id)
                .DeleteAsync();

        await db.DeleteAsync<LocationSet>(id);
    }

    public async Task<bool> ExistsAsync(string name)
    {
        var db = await _db.GetDbAsync();
        return await db.Table<LocationSet>()
                       .Where(s => s.Name == name)
                       .CountAsync() > 0;
    }
}