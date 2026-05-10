using System.Threading.Tasks;

namespace GogGameDownloader.Services.Auth;

public interface ISecureTokenStore
{
    Task SaveTokenAsync(string key, string value);
    Task<string?> LoadTokenAsync(string key);
    Task DeleteTokenAsync(string key);
}
