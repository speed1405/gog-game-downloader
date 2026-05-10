using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GogGameDownloader.Services.Settings;

public interface ISettingsRepository
{
    Task<string?> GetValueAsync(string key, CancellationToken ct = default);
    Task SetValueAsync(string key, string value, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, string>> GetValuesAsync(IEnumerable<string> keys, CancellationToken ct = default);
}
