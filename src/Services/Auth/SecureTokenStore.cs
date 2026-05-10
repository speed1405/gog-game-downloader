using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GogGameDownloader.Services.Auth;

public class SecureTokenStore : ISecureTokenStore
{
    private readonly string _storePath;

    public SecureTokenStore()
    {
        _storePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GogGameDownloader",
            "tokens");
        Directory.CreateDirectory(_storePath);
    }

    public async Task SaveTokenAsync(string key, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var encrypted = Encrypt(bytes);
        await File.WriteAllBytesAsync(GetTokenPath(key), encrypted);
    }

    public async Task<string?> LoadTokenAsync(string key)
    {
        var filePath = GetTokenPath(key);
        if (!File.Exists(filePath)) return null;

        try
        {
            var encrypted = await File.ReadAllBytesAsync(filePath);
            var decrypted = Decrypt(encrypted);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch
        {
            return null;
        }
    }

    public Task DeleteTokenAsync(string key)
    {
        var filePath = GetTokenPath(key);
        if (File.Exists(filePath)) File.Delete(filePath);
        return Task.CompletedTask;
    }

    private string GetTokenPath(string key)
    {
        // Hash the key to ensure filesystem-safe filenames across all platforms
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        var safeName = Convert.ToHexString(hash).ToLowerInvariant();
        return Path.Combine(_storePath, $"{safeName}.dat");
    }

    private static byte[] Encrypt(byte[] data)
    {
        var key = GetOrCreateKey();
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();
        using var ms = new MemoryStream();
        ms.Write(aes.IV, 0, aes.IV.Length);
        using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            cs.Write(data, 0, data.Length);
        return ms.ToArray();
    }

    private static byte[] Decrypt(byte[] data)
    {
        var key = GetOrCreateKey();
        using var aes = Aes.Create();
        aes.Key = key;
        var iv = new byte[16];
        Array.Copy(data, 0, iv, 0, 16);
        aes.IV = iv;
        using var ms = new MemoryStream(data, 16, data.Length - 16);
        using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var result = new MemoryStream();
        cs.CopyTo(result);
        return result.ToArray();
    }

    // NOTE: On Windows, tokens are wrapped with DPAPI (machine+user binding) for stronger protection.
    // On non-Windows platforms (macOS/Linux), the AES key is stored on disk as a best-effort fallback.
    // A future phase will integrate platform Keychain / Secret Service for full OS-level protection.
    private static byte[] GetOrCreateKey()
    {
        var keyPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GogGameDownloader", "key.dat");

        if (File.Exists(keyPath))
        {
            var raw = File.ReadAllBytes(keyPath);
            return UnwrapKey(raw);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(keyPath)!);
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        File.WriteAllBytes(keyPath, WrapKey(key));
        return key;
    }

    private static byte[] WrapKey(byte[] key)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return ProtectedData.Protect(key, null, DataProtectionScope.CurrentUser);
        }
        return key;
    }

    private static byte[] UnwrapKey(byte[] stored)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return ProtectedData.Unprotect(stored, null, DataProtectionScope.CurrentUser);
        }
        return stored;
    }
}
