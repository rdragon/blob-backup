using Azure.Storage.Blobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobBackup
{
    public class BlobContainerProvider
    {
        private readonly BlobContainerProviderSettings _settings;
        private readonly BlobServiceClient _blobServiceClient;

        public BlobContainerProvider(BlobContainerProviderSettings settings, BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
            _settings = settings;
        }

        public IBlobContainerClient GetContainer(string blobContainerName)
        {
            if (_settings.NoAzure)
            {
                return new DriveBlobContainerClient(blobContainerName);
            }

            return new AzureBlobContainerClient(_blobServiceClient.GetBlobContainerClient(blobContainerName));
        }
    }
}
