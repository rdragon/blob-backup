using Azure.Storage.Blobs.Models;

namespace BlobBackup;

public interface IBlobProperties
{
    string AccessTier { get; }
    CopyStatus CopyStatus { get; }
}
