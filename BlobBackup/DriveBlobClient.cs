using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobBackup;

public class DriveBlobClient : IBlobClient
{
    public string Name { get; }

    public DriveBlobClient(string path) => Name = path;

    private string FilePath => Name;

    public async Task<bool> Exists()
    {
        await Task.CompletedTask;

        return File.Exists(FilePath);
    }

    public async Task StartCopy(IBlobClient source, AccessTier accessTier)
    {
        await Task.CompletedTask;

        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        var driveBlobClient = source as DriveBlobClient ??
            throw new ArgumentException($"Invalid source of type '{source.GetType().FullName}' found.");

        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.Copy(driveBlobClient.FilePath, FilePath);
    }

    public async Task<IBlobProperties> GetProperties()
    {
        await Task.CompletedTask;

        return new DriveBlobProperties();
    }

    public async Task<bool> DeleteIfExists()
    {
        await Task.CompletedTask;

        if (File.Exists(FilePath))
        {
            File.Delete(FilePath);

            return true;
        }

        return false;
    }

    public async Task Upload(byte[] bytes, AccessTier? _)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        await File.WriteAllBytesAsync(FilePath, bytes);
    }

    public async Task<byte[]> Download()
    {
        return await File.ReadAllBytesAsync(FilePath);
    }
}
