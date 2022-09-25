using Azure.Storage.Blobs.Models;

namespace BlobBackup;

public class DriveBlobProperties : IBlobProperties
{
    public string AccessTier => "Hot";

    public CopyStatus CopyStatus => CopyStatus.Success;
}
