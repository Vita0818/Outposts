using System.Security.Cryptography;
using System.Text;

namespace Flotis.Interop;

public static class SecureSecretStore
{
    private static string SecretFilePath(string reference) => Path.Combine(FileLocations.AppDataFolder, $"{reference}.bin");

    public static bool Save(string reference, string secret)
    {
        try
        {
            Directory.CreateDirectory(FileLocations.AppDataFolder);
            var data = Encoding.UTF8.GetBytes(secret ?? string.Empty);
            var encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(SecretFilePath(reference), encrypted);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string? Load(string reference)
    {
        try
        {
            var path = SecretFilePath(reference);
            if (!File.Exists(path)) return null;
            var encrypted = File.ReadAllBytes(path);
            var plain = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plain);
        }
        catch
        {
            return null;
        }
    }

    public static void Delete(string reference)
    {
        try
        {
            var path = SecretFilePath(reference);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best effort delete for local secrets.
        }
    }
}
