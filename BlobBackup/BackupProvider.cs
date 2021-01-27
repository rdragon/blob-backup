using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobBackup
{
    public class BackupProvider
    {
        private readonly FileProvider _fileProvider;
        private readonly Index _index;
        private readonly BackupProviderSettings _settings;
        private readonly ChunkProvider _chunkProvider;

        public BackupProvider(
            FileProvider fileProvider,
            Index index,
            BackupProviderSettings settings,
            ChunkProvider chunkProvider)
        {
            _chunkProvider = chunkProvider;
            _settings = settings;
            _index = index;
            _fileProvider = fileProvider;
        }

        /// <summary>
        /// Loads the index, backups all files, removes not found file tokens from the index,
        /// and finally saves the index again.
        /// </summary>
        public async Task BackupFileSystemFolder(Stopwatch? stopwatch = null)
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
                    Helper.WriteLine($"Backup {(success ? "done in" : "failed after")} {stopwatch.GetPrettyElapsedTime()}.");
                }
            }

            async Task Run()
            {
                await _index.LoadIndex();

                var stopwatch = Stopwatch.StartNew();
                var foundFileIds = _fileProvider.GetAllFileIds().ToHashSet();
                Helper.WriteLine($"Generating a list of all {foundFileIds.Count:N0} files took {stopwatch.GetPrettyElapsedTime()}.");

                foreach (var fileId in foundFileIds)
                {
                    await BackupFile(fileId);
                }

                await _chunkProvider.Flush();

                _index.RemoveFileTokens(foundFileIds);

                success = true;

                await _index.SaveIndex();
            }
        }

        private async Task BackupFile(FileId fileId)
        {
            var fileInfo = _fileProvider.GetFileInfo(fileId);

            if (fileInfo.hidden || IsUpToDate(fileId, fileInfo.length, fileInfo.lastWriteTimeUtc))
            {
                return;
            }

            var totalLength = 0L;
            var chunkIds = new List<ChunkId>();
            using var fileStream = _fileProvider.OpenRead(fileId);
            var shardSize = _settings.ShardSizeBytes;
            var buffer = new byte[Math.Min(shardSize, fileInfo.length)];
            var foundNewFile = false;

            while (true)
            {
                var length = await fileStream.ReadAsync(buffer);

                if (totalLength > 0 && length == 0)
                {
                    break;
                }

                totalLength += length;
                var bytes = buffer[..length];
                var chunkId = new ChunkId(bytes.GetSha256Hash()[..Constants.CHUNK_ID_BYTES]);

                if (!foundNewFile && !_index.ContainsChunkId(chunkId))
                {
                    foundNewFile = true;
                    Helper.WriteLine($"Found new file '{fileId}'.");
                }

                await _chunkProvider.BackupChunk(new Chunk(ChunkId: chunkId, Raw: new Raw(bytes)));
                chunkIds.Add(chunkId);

                if (length == 0)
                {
                    break;
                }
            }

            var fileToken = new FileToken(totalLength, fileInfo.lastWriteTimeUtc, chunkIds);

            if (!foundNewFile && !_index.ContainsFileToken(fileId))
            {
                Helper.WriteLine($"Found new copy '{fileId}'.");
            }

            _index.AddOrReplaceFileToken(fileId, fileToken);
        }

        private bool IsUpToDate(FileId fileId, long length, DateTime lastWriteTimeUtc)
        {
            return
                _index.TryGetFileToken(fileId, out var backupView) &&
                backupView.Length == length &&
                backupView.LastWriteTimeUtc == lastWriteTimeUtc;
        }
    }
}
