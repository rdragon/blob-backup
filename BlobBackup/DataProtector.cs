using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BlobBackup;

public static class DataProtector
{
    public static byte[] Protect(byte[] bytes)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return ProtectedData.Protect(bytes, OptionalEntropy, DataProtectionScope.CurrentUser);
        }

        return GetCipher().Encrypt(bytes);
    }

    public static byte[] Unprotect(byte[] bytes)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return ProtectedData.Unprotect(bytes, OptionalEntropy, DataProtectionScope.CurrentUser);
        }

        return GetCipher().TryDecrypt(bytes) ?? throw new Exception("Cannot decrypt bytes.");
    }

    // Not secure, but better than nothing.
    private static Cipher GetCipher() => new Cipher(OptionalEntropy);

    private static byte[] OptionalEntropy => "e4d15faa95c78afe0a78b367b4a2bd26afc8e015e7937e311e52a2f0ed38f095".GetCipherKey();
}
