using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobBackup;

public class DriveBlobContainerClient : IBlobContainerClient
{
    private readonly string _path;

    public DriveBlobContainerClient(string path) => _path = path.Replace('/', Path.DirectorySeparatorChar);

    public async Task CreateIfNotExists()
    {
        await Task.CompletedTask;
        Directory.CreateDirectory(_path);
    }

    public IBlobClient GetBlobClient(string blobName)
    {
        return new DriveBlobClient(Path.Combine(_path, blobName.Replace('/', Path.DirectorySeparatorChar)));
    }

    public async IAsyncEnumerable<IBlobHierarchyItem> GetBlobsByHierarchy(string folder)
    {
        await Task.CompletedTask;

        if (Directory.Exists(_path))
        {
            foreach (var path in Directory.GetFiles(_path, "*", SearchOption.AllDirectories))
            {
                if (!path.StartsWith(_path, StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception($"This should not happen.");
                }

                yield return new DriveBlobHierarchyItem(path[_path.Length..]);
            }
        }
    }
}
