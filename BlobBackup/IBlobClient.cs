using Azure.Storage.Blobs.Models;

namespace BlobBackup;

public interface IBlobClient
{
    string Name { get; }
    Task<bool> Exists();
    Task StartCopy(IBlobClient source, AccessTier accessTier);
    Task<IBlobProperties> GetProperties();
    Task<bool> DeleteIfExists();
    Task Upload(byte[] bytes, AccessTier? accessTier);
    Task<byte[]> Download();
}
