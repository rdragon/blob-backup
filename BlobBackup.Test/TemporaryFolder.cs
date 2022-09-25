namespace BlobBackup.Test;

public sealed class TemporaryFolder : IDisposable
{
    public string Path { get; }

    public TemporaryFolder()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "blob-backup-test", TestHelper.GetRandomUniqueString());
    }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
