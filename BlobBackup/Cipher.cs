using System.Security.Cryptography;

namespace BlobBackup;

/// <summary>
/// Encrypt and decrypt byte arrays using AES-256 and HMAC-SHA256.
/// </summary>
public class Cipher
{
    private const int BLOCK_SIZE_BYTES = 16;
    private const int HMAC_SIZE_BYTES = 32;

    private byte[]? _key;

    private readonly SecretProvider? _secretProvider;

    public Cipher(SecretProvider secretProvider)
    {
        _secretProvider = secretProvider;
    }

    public Cipher(byte[] key)
    {
        _key = key;
    }

    /// <summary>
    /// Encrypts the given byte array using AES-256 and HMAC-SHA256.
    /// The encrypted byte array is between 49 and 64 bytes larger than the original, due to IV (16), padding (1-15) and HMAC (32).
    /// </summary>
    public byte[] Encrypt(byte[] data)
    {
        var key = Key;

        using var aes = Aes.Create();
        aes.KeySize = key.Length * 8;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        var iv = RandomNumberGenerator.GetBytes(BLOCK_SIZE_BYTES);
        using var transform = aes.CreateEncryptor(key, iv);
        using var memoryStream = new MemoryStream();
        using var cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write);
        using var hmacObj = new HMACSHA256(key);
        memoryStream.Write(iv, 0, BLOCK_SIZE_BYTES);
        cryptoStream.Write(data, 0, data.Length);
        cryptoStream.FlushFinalBlock();
        memoryStream.Position = 0;
        var hmac = hmacObj.ComputeHash(memoryStream);
        memoryStream.Write(hmac, 0, hmac.Length);

        return memoryStream.ToArray();
    }

    /// <summary>
    /// Decrypts the given byte array using AES-256 and HMAC-SHA256.
    /// Returns null on failure.
    /// </summary>
    public byte[]? TryDecrypt(byte[]? data)
    {
        if (data is null)
        {
            return null;
        }

        var key = Key;
        var n = data.Length - HMAC_SIZE_BYTES - BLOCK_SIZE_BYTES;

        if (n <= 0)
        {
            return null;
        }

        using var aes = Aes.Create();
        using var hmac = new HMACSHA256(key);
        var actualHmac = hmac.ComputeHash(data, 0, BLOCK_SIZE_BYTES + n);
        var expectedHmac = GetSubArray(data, BLOCK_SIZE_BYTES + n, HMAC_SIZE_BYTES);

        if (!actualHmac.SequenceEqual(expectedHmac))
        {
            return null;
        }

        aes.KeySize = key.Length * 8;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        var iv = GetSubArray(data, 0, BLOCK_SIZE_BYTES);
        var buffer = new byte[data.Length];
        int readCount;
        using var transform = aes.CreateDecryptor(key, iv);
        using var memoryStream = new MemoryStream(data, BLOCK_SIZE_BYTES, n);
        using var cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Read);
        readCount = cryptoStream.ReadAll(buffer, 0, buffer.Length);

        return GetSubArray(buffer, 0, readCount);
    }

    public byte[] Key
    {
        get
        {
            _key ??= _secretProvider?.GetSecret(SecretType.CipherKey).Result.GetCipherKey() ??
                throw new InvalidOperationException("No secret provider found and key is null.");

            return _key;
        }
        set
        {
            if (value.Length != 32)
            {
                throw new ArgumentException("Cipher key should consist of exactly 32 bytes.");
            }

            _key = value;
        }
    }

    public void DeleteKey()
    {
        _key = null;
    }

    private static T[] GetSubArray<T>(T[] data, int index, int length)
    {
        if (index == 0 && length == data.Length)
        {
            return data;
        }

        var result = new T[length];
        Array.Copy(data, index, result, 0, length);

        return result;
    }
}
