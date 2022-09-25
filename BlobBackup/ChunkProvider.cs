using System.Buffers;
using System.Security.Cryptography;

namespace BlobBackup;

public class ChunkProvider
{
    private readonly List<Chunk> _chunks = new List<Chunk>();
    private long _chunksTotalBytes;

    private readonly Index _index;
    private readonly ChunkProviderSettings _settings;
    private readonly BlobProvider _blobProvider;

    public ChunkProvider(Index index, ChunkProviderSettings settings, BlobProvider blobProvider)
    {
        _blobProvider = blobProvider;
        _settings = settings;
        _index = index;
    }

    /// <summary>
    /// Backups the given chunk if it is not found inside the index.
    /// Might write the chunk to a buffer instead of uploading it to the blob storage.
    /// </summary>
    public async Task BackupChunk(Chunk chunk)
    {
        if (_index.ContainsChunk(chunk))
        {
            return;
        }

        if (chunk.Raw.Bytes.Length == _settings.ShardSizeBytes)
        {
            await UploadShard(new[] { chunk });
        }
        else
        {
            _chunks.Add(chunk);
            _chunksTotalBytes += chunk.Raw.Bytes.Length;

            if (_chunksTotalBytes >= _settings.ShardSizeBytes)
            {
                await Flush();
            }
        }
    }

    /// <summary>
    /// Uploads all chunks that are left inside the buffer.
    /// </summary>
    public async Task Flush()
    {
        if (_chunks.Count > 0)
        {
            await UploadShard(_chunks);
            _chunks.Clear();
            _chunksTotalBytes = 0;
        }
    }

    private async Task UploadShard(IReadOnlyList<Chunk> chunks)
    {
        var shardId = new ShardId(RandomNumberGenerator.GetBytes(16));
        var compressionType = await _blobProvider.UploadShard(shardId, chunks.SelectMany(chunk => chunk.Raw.Bytes).ToArray());
        var shardToken = new ShardToken(chunks.Select(chunk => chunk.GetChunkToken()).ToArray(), compressionType);
        await _index.AddShardToken(shardId, shardToken);
    }
}
