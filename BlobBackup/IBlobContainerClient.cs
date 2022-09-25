namespace BlobBackup;

public interface IBlobContainerClient
{
    Task CreateIfNotExists();
    IBlobClient GetBlobClient(string blobName);
    IAsyncEnumerable<IBlobHierarchyItem> GetBlobsByHierarchy(string folder);
}
