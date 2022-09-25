using System.Text;

namespace BlobBackup;

public class SecretProvider
{
    private readonly SecretProviderSettings _settings;

    public SecretProvider(SecretProviderSettings settings)
    {
        _settings = settings;
    }

    public async Task<string> GetSecret(SecretType secretType, bool recache = false)
    {
        if (secretType == SecretType.CipherKey && _settings.CipherSecret is string cipherSecret)
        {
            return cipherSecret;
        }

        if (secretType == SecretType.ConnectionString && _settings.ConnectionString is string connectionString)
        {
            return connectionString;
        }

        if (!File.Exists(GetPath(secretType)) || recache)
        {
            Helper.WriteLine($"{secretType} not found. Please provide the {secretType}:");

            if (Console.ReadLine() is not string secret)
            {
                throw new Exception($"Unexpected end of stream.");
            }

            await SetSecret(secretType, secret);
        }

        return await ReadFile(secretType);
    }

    public async Task SetSecret(SecretType secretType, string secret)
    {
        var plainBytes = Encoding.UTF8.GetBytes(secret);
        var encryptedBytes = DataProtector.Protect(plainBytes);
        var path = GetPath(secretType);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path, encryptedBytes);
    }

    public void DeleteSecret(SecretType secretType)
    {
        var path = GetPath(secretType);

        if (File.Exists(path))
        {
            File.Delete(path);
            Helper.WriteLine($"Deleted secret {secretType}.");
        }
        else
        {
            Helper.WriteLine($"Secret {secretType} not found.");
        }
    }

    public void DeleteSecrets()
    {
        var folder = SecretsFolder;

        if (Directory.Exists(folder))
        {
            var files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                File.Delete(file);
            }

            Directory.Delete(folder);

            Helper.WriteLine($"Deleted {files.Length:N0} file(s) and 1 directory.");
        }
        else
        {
            Helper.WriteLine("Nothing to clear.");
        }
    }

    private async Task<string> ReadFile(SecretType secretType)
    {
        var encryptedBytes = await File.ReadAllBytesAsync(GetPath(secretType));

        try
        {
            var plainBytes = DataProtector.Unprotect(encryptedBytes);

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch
        {
            File.Delete(GetPath(secretType));

            throw;
        }
    }

    private string SecretsFolder => _settings.SecretsFolder ?? throw new InvalidOperationException("Secrets folder not set.");

    private string GetPath(SecretType secretType) => Path.Combine(SecretsFolder, $"{(int)secretType}");
}
