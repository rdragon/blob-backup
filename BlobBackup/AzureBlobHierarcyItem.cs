using Azure.Storage.Blobs.Models;

namespace BlobBackup;

public class AzureBlobHierarcyItem : IBlobHierarchyItem
{
    private readonly BlobHierarchyItem _blobHierarchyItem;

    public AzureBlobHierarcyItem(BlobHierarchyItem blobHierarchyItem)
    {
        _blobHierarchyItem = blobHierarchyItem;
    }

    public string BlobName => _blobHierarchyItem.Blob.Name;
}
