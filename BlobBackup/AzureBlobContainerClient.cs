using Azure.Storage.Blobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobBackup;

public class AzureBlobContainerClient : IBlobContainerClient
{
    private readonly BlobContainerClient _blobContainerClient;

    public AzureBlobContainerClient(BlobContainerClient blobContainerClient)
    {
        _blobContainerClient = blobContainerClient;
    }

    public async Task CreateIfNotExists()
    {
        await _blobContainerClient.CreateIfNotExistsAsync();
    }

    public IBlobClient GetBlobClient(string blobName)
    {
        return new AzureBlobClient(_blobContainerClient.GetBlobClient(blobName));
    }

    public async IAsyncEnumerable<IBlobHierarchyItem> GetBlobsByHierarchy(string folderBlobName)
    {
        await foreach (var item in _blobContainerClient.GetBlobsByHierarchyAsync(prefix: folderBlobName + "/"))
        {
            yield return new AzureBlobHierarcyItem(item);
        }
    }
}
