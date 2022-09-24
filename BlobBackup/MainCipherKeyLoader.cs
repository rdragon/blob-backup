using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BlobBackup
{
    public class MainCipherKeyLoader
    {
        private readonly BlobProvider _blobProvider;
        private readonly Cipher _cipher;
        private readonly SecretProvider _secretProvider;

        public MainCipherKeyLoader(BlobProvider blobProvider, Cipher cipher, SecretProvider secretProvider)
        {
            _secretProvider = secretProvider;
            _cipher = cipher;
            _blobProvider = blobProvider;
        }

        public async Task LoadMainCipherKey()
        {
            if (await _blobProvider.MainCipherKeyExists())
            {
                UpdateCipher(await _blobProvider.DownloadMainCipherKey());
                return;
            }

            if (await _blobProvider.IndexExists() || (await _blobProvider.DownloadShardTokens().ToListAsync()).Count > 0)
            {
                throw new Exception($"The blob storage folder is corrupted. The main cipher key cannot be found.");
            }

            var mainCipherKey = RandomNumberGenerator.GetBytes(32);
            await SaveMainCipherKey(mainCipherKey);
        }

        public async Task SaveMainCipherKey(byte[] mainCipherKey)
        {
            await _blobProvider.UploadMainCipherKey(mainCipherKey);
            UpdateCipher(mainCipherKey);
        }

        public async Task ChangeCipherKey()
        {
            var mainCipherKey = await _blobProvider.DownloadMainCipherKey();
            _secretProvider.DeleteSecret(SecretType.CipherKey);
            _cipher.DeleteKey();
            await SaveMainCipherKey(mainCipherKey);
        }

        public async Task SetMainCipherKey()
        {
            Helper.WriteLine("Please provide the main cipher key:");

            if (Console.ReadLine() is not string text)
            {
                throw new Exception($"Unexpected end of stream.");
            }

            await SaveMainCipherKey(text.GetCipherKey());
        }

        private void UpdateCipher(byte[] cipherKey)
        {
            _cipher.Key = cipherKey;
        }
    }
}
