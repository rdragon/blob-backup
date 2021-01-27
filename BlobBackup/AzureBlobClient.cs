using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobBackup
{
    public class AzureBlobClient : IBlobClient
    {
        private readonly BlobClient _blobClient;

        public AzureBlobClient(BlobClient blobClient)
        {
            _blobClient = blobClient;
        }

        public string Name => _blobClient.Name;

        public async Task<bool> Exists()
        {
            return await _blobClient.ExistsAsync();
        }

        public async Task StartCopy(IBlobClient source, AccessTier accessTier)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var options = new BlobCopyFromUriOptions { AccessTier = accessTier, RehydratePriority = RehydratePriority.Standard };
            var blobClient = source as AzureBlobClient ??
                throw new ArgumentException($"Invalid source of type '{source.GetType().FullName}' found.");
            await _blobClient.StartCopyFromUriAsync(blobClient._blobClient.Uri, options);
        }

        public async Task<IBlobProperties> GetProperties()
        {
            return new AzureBlobProperties((await _blobClient.GetPropertiesAsync()).Value);
        }

        public async Task<bool> DeleteIfExists()
        {
            return await _blobClient.DeleteIfExistsAsync();
        }

        public async Task Upload(byte[] bytes, AccessTier? accessTier)
        {
            var memoryStream = new MemoryStream(bytes);
            var transferOptions = new StorageTransferOptions
            {
                MaximumTransferSize = long.MaxValue,
                InitialTransferSize = long.MaxValue,
            };
            var options = new BlobUploadOptions { AccessTier = accessTier, TransferOptions = transferOptions };
            await _blobClient.UploadAsync(memoryStream, options);
        }

        public async Task<byte[]> Download()
        {
            var memoryStream = new MemoryStream();
            await _blobClient.DownloadToAsync(memoryStream);

            return memoryStream.ToArray();
        }
    }
}
