using Azure.Storage.Blobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobBackup;

public class AzureProvider
{
    private BlobContainerClient? _container;

    private readonly BlobServiceClient _blobServiceClient;
    private readonly AzureProviderSettings _settings;

    public AzureProvider(BlobServiceClient blobServiceClient, AzureProviderSettings settings)
    {
        _settings = settings;
        _blobServiceClient = blobServiceClient;
    }

    private BlobContainerClient Container
    {
        get
        {
            if (_container is null)
            {
                _container = _blobServiceClient.GetBlobContainerClient(_settings.Container);

                if (_settings.CreateContainer)
                {
                    // This line is not executed always as it's another call to the blob storage.
                    _container.CreateIfNotExists();
                }
            }

            return _container;
        }
    }
}
