using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobBackup.Test
{
    public static class TestExtensionMethods
    {
        public static byte[] GetByteArray(this int length)
        {
            if (length == 0)
            {
                return Array.Empty<byte>();
            }

            var buffer = new byte[length];
            new Random(length * 91909 + 68947).NextBytes(buffer);

            return buffer;
        }

        public static IndexData GetIndexData(this Random random, int? fileTokenCount = null, int? shardTokenCount = null)
        {
            fileTokenCount ??= random.Next(3);
            shardTokenCount ??= random.Next(3);
            var fileTokens = new Dictionary<FileId, FileToken>();
            var shardTokens = new Dictionary<ShardId, ShardToken>();

            for (int i = 0; i < fileTokenCount; i++)
            {
                fileTokens[random.GetFileId()] = random.GetFileToken();
            }

            for (int i = 0; i < shardTokenCount; i++)
            {
                shardTokens[random.GetShardId()] = random.GetShardToken();
            }

            return new IndexData(
                fileTokens.Keys.ToArray(),
                fileTokens.Values.ToArray(),
                shardTokens.Keys.ToArray(),
                shardTokens.Values.ToArray());
        }

        public static FileId GetFileId(this Random random)
        {
            var name = random.GetUniqueString().Insert(8, " ");

            return new FileId(random.Next(4) switch
            {
                0 => name,
                1 => $"folder1/{name}",
                2 => $"folder1/sub folder/{name}",
                _ => $"folder 2/{name}"
            });
        }

        public static ShardToken GetShardToken(this Random random, int? chunkCount = null)
        {
            chunkCount ??= random.Next(3) + 1;
            var chunkTokens = Enumerable.Range(0, chunkCount.Value).Select(_ => random.GetChunkToken()).ToArray();

            return new ShardToken(chunkTokens, CompressionType.None);
        }

        public static ChunkToken GetChunkToken(this Random random)
        {
            var length = random.Next();
            var chunkId = random.GetChunkId();

            return new ChunkToken(chunkId, length);
        }

        public static FileToken GetFileToken(this Random random, int? chunkCount = null)
        {
            chunkCount ??= random.Next(3) + 1;
            var length = random.Next();
            var lastWriteTimeUtc = DateTime.UtcNow.AddTicks(-random.Next());
            var chunkIds = Enumerable.Range(0, chunkCount.Value).Select(_ => random.GetChunkId()).ToArray();

            return new FileToken(length, lastWriteTimeUtc, chunkIds);
        }

        public static ChunkId GetChunkId(this Random random) => new ChunkId(random.GetBytes(Constants.CHUNK_ID_BYTES));

        public static ShardId GetShardId(this Random random) => new ShardId(random.GetBytes(Constants.SHARD_ID_BYTES));

        public static byte[] GetBytes(this Random random, int? count = null)
        {
            count ??= random.Next(3);
            var bytes = new byte[count.Value];
            random.NextBytes(bytes);

            return bytes;
        }

        public static string GetUniqueString(this Random random) => random.GetBytes(16).GetHexString();

        public static Random GetRandom(this int seed) => new Random(seed);
    }
}
