using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobBackup;

public interface IBlobContainerClient
{
    Task CreateIfNotExists();
    IBlobClient GetBlobClient(string blobName);
    IAsyncEnumerable<IBlobHierarchyItem> GetBlobsByHierarchy(string folder);
}
