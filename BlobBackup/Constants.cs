namespace BlobBackup;

public static class Constants
{
    /// <summary>
    /// The length of a shard ID.
    /// </summary>
    public const int SHARD_ID_BYTES = 16;

    /// <summary>
    /// The length of a chunk ID.
    /// </summary>
    public const int CHUNK_ID_BYTES = 16;

    /// <summary>
    /// The name of the default HTTP client.
    /// </summary>
    public const string DEFAULT_HTTP_CLIENT = "default";
}
