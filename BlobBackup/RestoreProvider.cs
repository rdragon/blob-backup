using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobBackup
{
    public class RestoreProvider
    {
        private readonly FileProvider _fileProvider;
        private readonly Index _index;
        private readonly BlobProvider _blobProvider;

        public RestoreProvider(
            FileProvider fileProvider,
            Index index,
            BlobProvider blobProvider)
        {
            _blobProvider = blobProvider;
            _index = index;
            _fileProvider = fileProvider;
        }

        public async Task CopyShards(Stopwatch? stopwatch = null)
        {
            var success = false;

            try
            {
                await Run();
            }
            finally
            {
                if (stopwatch is { })
                {
                    Helper.WriteLine($"Copy shards {(success ? "done in" : "failed after")} {stopwatch.GetPrettyElapsedTime()}.");
                }
            }

            async Task Run()
            {
                await _index.LoadIndex();

                foreach (var (shardId, _) in _index.GetRelevantShardTokens(GetRelevantChunkIds()))
                {
                    await _blobProvider.CopyShard(shardId);
                }

                success = true;
            }
        }

        public async Task RestoreFileSystemFolder(Stopwatch? stopwatch = null)
        {
            var success = false;

            try
            {
                await Run();
            }
            finally
            {
                if (stopwatch is { })
                {
                    Helper.WriteLine($"Restore {(success ? "done in" : "failed after")} {stopwatch.GetPrettyElapsedTime()}.");
                }
            }

            async Task Run()
            {
                _fileProvider.RequireEmptyFileSystemFolder();
                await _index.LoadIndex();
                await DownloadChunks();

                foreach (var (fileId, fileToken) in _index.FileTokens)
                {
                    using (var fileStream = _fileProvider.OpenWrite(fileId))
                    {
                        foreach (var chunkId in fileToken.ChunkIds)
                        {
                            await _fileProvider.WriteChunkToStream(chunkId, fileStream);
                        }
                    }

                    _fileProvider.SetLastWriteTimeUtc(fileId, fileToken.LastWriteTimeUtc);
                }

                _fileProvider.DeleteChunks();

                success = true;
            }
        }

        private async Task DownloadChunks()
        {
            var relevantChunkIds = GetRelevantChunkIds();

            foreach (var (shardId, shardToken) in _index.GetRelevantShardTokens(relevantChunkIds))
            {
                var bytes = await _blobProvider.DownloadShard(shardId, shardToken.CompressionType);
                var index = 0;

                foreach (var chunkToken in shardToken.ChunkTokens)
                {
                    if (relevantChunkIds.Contains(chunkToken.ChunkId))
                    {
                        await _fileProvider.WriteChunk(chunkToken.ChunkId, bytes[index..(index + chunkToken.Length)]);
                    }

                    index += chunkToken.Length;
                }
            }
        }

        private IReadOnlySet<ChunkId> GetRelevantChunkIds()
        {
            var chunkIds = new HashSet<ChunkId>();

            foreach (var (_, fileToken) in _index.FileTokens)
            {
                foreach (var chunkId in fileToken.ChunkIds)
                {
                    if (!_fileProvider.ChunkExists(chunkId))
                    {
                        chunkIds.Add(chunkId);
                    }
                }
            }

            return chunkIds;
        }
    }
}
