using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace BlobBackup;

public class Index
{
    /// <summary>
    /// Contains a file token for each backuped file.
    /// </summary>
    public Dictionary<FileId, FileToken> FileTokens { get; } = new();

    /// <summary>
    /// Contains a shard token for each shard found in the blob storage.
    /// </summary>
    private Dictionary<ShardId, ShardToken> _shardTokens = new();

    /// <summary>
    /// The IDs of all chunks found in the blob storage.
    /// </summary>
    private HashSet<ChunkId> _chunkIds = new();

    private bool _changed;

    private readonly BlobProvider _blobProvider;
    private readonly IndexMonitor _indexMonitor;
    private readonly IndexSettings _settings;

    public Index(BlobProvider blobProvider, IndexMonitor indexMonitor, IndexSettings settings)
    {
        _settings = settings;
        _indexMonitor = indexMonitor;
        _blobProvider = blobProvider;
    }

    /// <summary>
    /// Loads the index from the blob storage.
    /// Should be called before any other method.
    /// </summary>
    public async Task LoadIndex()
    {
        if (!await _blobProvider.IndexExists())
        {
            // Reset the index in case the index was manually removed.
            await Reset();

            return;
        }

        var indexData = await _blobProvider.DownloadIndex();

        for (int i = 0; i < indexData.FileIds.Count; i++)
        {
            FileTokens[indexData.FileIds[i]] = indexData.FileTokens[i];
        }

        for (int i = 0; i < indexData.ShardIds.Count; i++)
        {
            var shardId = indexData.ShardIds[i];
            var shardToken = indexData.ShardTokens[i];
            _shardTokens[shardId] = shardToken;

            foreach (var chunkToken in shardToken.ChunkTokens)
            {
                _chunkIds.Add(chunkToken.ChunkId);
            }
        }

        if (_settings.ResetIndex)
        {
            await Reset();
        }
        else if (_indexMonitor.ShardsChanged)
        {
            Helper.WriteLine(
                "A previous run of the program did not save the index. " +
                "An index reset is required.");
            await Reset();
        }
    }

    /// <summary>
    /// Saves the index to the blob storage if needed.
    /// </summary>
    public async Task SaveIndex()
    {
        if (!_changed)
        {
            return;
        }

        var indexData = new IndexData(
            FileTokens.Keys.ToArray(),
            FileTokens.Values.ToArray(),
            _shardTokens.Keys.ToArray(),
            _shardTokens.Values.ToArray());

        await _blobProvider.UploadIndex(indexData);
        _changed = false;
        _indexMonitor.ShardsChanged = false;
    }

    public async Task AddShardToken(ShardId shardId, ShardToken shardToken)
    {
        await _blobProvider.UploadShardToken(shardId, shardToken);
        _shardTokens[shardId] = shardToken;
        _indexMonitor.ShardsChanged = true;
        _changed = true;
    }

    public bool ContainsChunkId(ChunkId chunkId) => _chunkIds.Contains(chunkId);

    public bool ContainsChunk(Chunk chunk) => ContainsChunkId(chunk.ChunkId);

    public bool ContainsFileToken(FileId fileId) => FileTokens.ContainsKey(fileId);

    public void AddOrReplaceFileToken(FileId fileId, FileToken fileToken)
    {
        if (!TryGetFileToken(fileId, out var foundFileToken) || !foundFileToken.Equals(fileToken))
        {
            FileTokens[fileId] = fileToken;
            _changed = true;
        }
    }

    public bool TryGetFileToken(FileId fileId, [MaybeNullWhen(false)] out FileToken fileToken)
    {
        return FileTokens.TryGetValue(fileId, out fileToken);
    }

    /// <summary>
    /// Removes all file tokens except for the file tokens with an ID inside the given set.
    /// </summary>
    public void RemoveFileTokens(IReadOnlySet<FileId> fileIdsToKeep)
    {
        var fileIdsToRemove = FileTokens.Keys.Where(fileId => !fileIdsToKeep.Contains(fileId)).ToArray();

        if (fileIdsToRemove.Length > 0)
        {
            foreach (var fileId in fileIdsToRemove)
            {
                FileTokens.Remove(fileId);
                Helper.WriteLine($"Removed '{fileId}' from index.");
            }

            _changed = true;
        }
    }

    /// <summary>
    /// Replaces the shard tokens in the index by the shard tokens found in the "shard-tokens" folder in the blob storage.
    /// Also, file tokens that reference non-existing chunks are deleted.
    /// </summary>
    public async Task Reset()
    {
        var stopwatch = Stopwatch.StartNew();

        var shardTokens = new Dictionary<ShardId, ShardToken>();

        await foreach (var (shardId, shardToken) in _blobProvider.DownloadShardTokens())
        {
            shardTokens[shardId] = shardToken;
        }

        if (!shardTokens.SequenceEqual(_shardTokens))
        {
            _shardTokens = shardTokens;
            _chunkIds = shardTokens.Values
                .SelectMany(token => token.ChunkTokens.Select(chunk => chunk.ChunkId))
                .ToHashSet();
            _indexMonitor.ShardsChanged = true;
            _changed = true;
        }

        var fileTokensToRemove = FileTokens.Where(pair => pair.Value.ChunkIds.Any(chunkId => !_chunkIds.Contains(chunkId))).ToArray();

        if (fileTokensToRemove.Length > 0)
        {
            foreach (var fileToken in fileTokensToRemove)
            {
                FileTokens.Remove(fileToken.Key);
            }

            _changed = true;
        }

        Helper.WriteLine($"Resetting index took {stopwatch.GetPrettyElapsedTime()}.");
    }

    public IEnumerable<(ShardId shardId, ShardToken shardToken)> GetRelevantShardTokens(IReadOnlySet<ChunkId> relevantChunkIds)
    {
        return _shardTokens
            .Where(pair => pair.Value.ChunkTokens.Any(token => relevantChunkIds.Contains(token.ChunkId)))
            .Select(pair => (pair.Key, pair.Value));
    }
}
