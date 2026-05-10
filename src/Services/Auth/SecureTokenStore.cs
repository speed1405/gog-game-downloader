using System;
using System.IO;
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

    private string GetTokenPath(string key) =>
        Path.Combine(_storePath, $"{key.Replace("/", "_").Replace("\\", "_")}.dat");

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

    private static byte[] GetOrCreateKey()
    {
        var keyPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GogGameDownloader", "key.dat");

        if (File.Exists(keyPath))
            return File.ReadAllBytes(keyPath);

        Directory.CreateDirectory(Path.GetDirectoryName(keyPath)!);
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        File.WriteAllBytes(keyPath, key);
        return key;
    }
}
