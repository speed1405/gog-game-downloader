using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GogGameDownloader.Data;
using GogGameDownloader.Models;
using Microsoft.EntityFrameworkCore;

namespace GogGameDownloader.Services.Settings;

public class SettingsRepository : ISettingsRepository
{
    private readonly AppDbContext _dbContext;

    public SettingsRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string?> GetValueAsync(string key, CancellationToken ct = default)
    {
        return await _dbContext.AppSettings
            .AsNoTracking()
            .Where(s => s.Key == key)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(ct);
    }

    public async Task SetValueAsync(string key, string value, CancellationToken ct = default)
    {
        var existing = await _dbContext.AppSettings.FirstOrDefaultAsync(s => s.Key == key, ct);
        if (existing is null)
        {
            _dbContext.AppSettings.Add(new AppSetting
            {
                Key = key,
                Value = value
            });
        }
        else
        {
            existing.Value = value;
        }

        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyDictionary<string, string>> GetValuesAsync(IEnumerable<string> keys, CancellationToken ct = default)
    {
        var requestedKeys = keys.Distinct().ToArray();
        if (requestedKeys.Length == 0)
        {
            return new Dictionary<string, string>();
        }

        return await _dbContext.AppSettings
            .AsNoTracking()
            .Where(s => requestedKeys.Contains(s.Key))
            .ToDictionaryAsync(s => s.Key, s => s.Value, ct);
    }
}
