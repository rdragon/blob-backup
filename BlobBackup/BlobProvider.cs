using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobBackup
{
    public class BlobProvider
    {
        private IBlobContainerClient? _container;

        private readonly Cipher _cipher;
        private readonly BlobProviderSettings _settings;
        private readonly Serializer _serializer;
        private readonly BlobContainerProvider _blobContainerProvider;

        public BlobProvider(
            Cipher cipher,
            BlobProviderSettings settings,
            Serializer serializer,
            BlobContainerProvider blobContainerProvider)
        {
            _blobContainerProvider = blobContainerProvider;
            _serializer = serializer;
            _settings = settings;
            _cipher = cipher;
        }

        public async Task UploadShardToken(ShardId shardId, ShardToken shardToken)
        {
            await UploadFile(_serializer.Serialize(shardToken), GetShardTokenBlob(shardId), $"shard token {shardId}", useCompression: true);
        }

        public async Task<ShardToken> DownloadShardToken(ShardId shardId)
        {
            var bytes = await DownloadFile(GetShardTokenBlob(shardId), $"shard token {shardId}", useCompression: true);

            return _serializer.Deserialize<ShardToken>(bytes);
        }

        public async Task<CompressionType> UploadShard(ShardId shardId, byte[] bytes)
        {
            if (_settings.Fake)
            {
                Helper.WriteLine($"Uploading 'shard {shardId}' (fake).");

                return CompressionType.None;
            }

            var stopwatch = Stopwatch.StartNew();
            byte[] compressedBytes;

            if (_settings.NoCompression)
            {
                compressedBytes = bytes;
            }
            else
            {
                compressedBytes = _settings.SevenZip ? await SevenZipProvider.Compress(bytes) : GZipProvider.Compress(bytes);
            }

            stopwatch.Stop();
            CompressionType compressionType;

            if (compressedBytes.Length < bytes.Length || _settings.CompressAlways)
            {
                compressionType = _settings.SevenZip ? CompressionType.SevenZip : CompressionType.GZip;
                var avgBytes = (long)Math.Round(compressedBytes.Length / stopwatch.Elapsed.TotalSeconds);

                Helper.WriteLine(
                    $"Compressing {bytes.Length.GetPrettyBytes()} to {compressedBytes.Length.GetPrettyBytes()} using '{compressionType}' " +
                    $"took {stopwatch.GetPrettyElapsedTime()} (avg {avgBytes.GetPrettyBytes(decimals: 1)}/s).");
            }
            else
            {
                compressedBytes = bytes;
                compressionType = CompressionType.None;
            }

            await UploadFile(compressedBytes, GetShardBlob(shardId), $"shard {shardId}", accessTier: _settings.AccessTier);

            return compressionType;
        }

        public async Task<byte[]> DownloadShard(ShardId shardId, CompressionType compressionType)
        {
            byte[] compressedBytes;

            if (ShardCopyIsRequired)
            {
                await RequireCopyDone(shardId);

                compressedBytes = await DownloadFile(GetHotShardBlob(shardId), $"shard {shardId}");
            }
            else
            {
                compressedBytes = await DownloadFile(GetShardBlob(shardId), $"shard {shardId}");
            }

            return compressionType switch
            {
                CompressionType.None => compressedBytes,
                CompressionType.GZip => GZipProvider.Decompress(compressedBytes),
                CompressionType.SevenZip => await SevenZipProvider.Decompress(compressedBytes),
                _ => throw new Exception($"Unknown compression type {compressionType}."),
            };
        }

        public async Task UploadIndex(IndexData index)
        {
            await UploadFile(_serializer.Serialize(index), GetIndexBlob(), "index", useCompression: true);
            await UploadFile(_serializer.Serialize(index), GetIndexBackupBlob(), "index backup", useCompression: true);
        }

        public async Task<IndexData> DownloadIndex()
        {
            return _serializer.Deserialize<IndexData>(await DownloadFile(GetIndexBlob(), "index", useCompression: true));
        }

        public async Task<bool> IndexExists() => await GetIndexBlob().Exists();

        public async Task UploadMainCipherKey(byte[] mainCipherKey)
        {
            await UploadFile(mainCipherKey, GetMainCipherKeyBlob(), "main cipher key");
        }

        public async Task<byte[]> DownloadMainCipherKey()
        {
            return await DownloadFile(GetMainCipherKeyBlob(), "main cipher key");
        }

        public async Task<bool> MainCipherKeyExists() => await GetMainCipherKeyBlob().Exists();

        public async Task<bool> GetShardsChanged() => await GetShardsChangedBlob().Exists();

        public async Task SetShardsChanged(bool changed)
        {
            var blob = GetShardsChangedBlob();

            if (changed)
            {
                await UploadFile(Array.Empty<byte>(), blob, useEncryption: false);
            }
            else
            {
                await DeleteBlob(blob);
            }
        }

        public async Task<bool> ShardExists(ShardId shardId) => await GetShardBlob(shardId).Exists();

        public async Task DeleteShard(ShardId shardId)
        {
            await DeleteBlob(GetShardBlob(shardId));
        }

        /// <summary>
        /// Downloads all shard tokens found inside the "shard-tokens" folder.
        /// </summary>
        public async IAsyncEnumerable<(ShardId shardId, ShardToken shardToken)> DownloadShardTokens()
        {
            await foreach (var hierarchyItem in Container.GetBlobsByHierarchy(folder: GetShardTokensFolder()))
            {
                var shardId = GetShardIdFromBlobName(hierarchyItem.BlobName);
                var shardToken = await DownloadShardToken(shardId);

                yield return (shardId, shardToken);
            }
        }

        public async Task CopyShard(ShardId shardId)
        {
            if (ShardCopyIsRequired)
            {
                var archiveBlob = GetShardBlob(shardId);
                var hotBlob = GetHotShardBlob(shardId);

                if (!await hotBlob.Exists())
                {
                    if (_settings.Fake)
                    {
                        Helper.WriteLine($"Copy of shard {shardId} started (fake).");
                    }
                    else
                    {
                        await hotBlob.StartCopy(archiveBlob, AccessTier.Hot);
                        Helper.WriteLine($"Copy of shard {shardId} started.");
                    }
                }
            }
        }

        public async Task RequireCopyDone(ShardId shardId)
        {
            var properties = await GetHotShardBlob(shardId).GetProperties();
            var copyStatus = properties.CopyStatus;
            var accessTier = properties.AccessTier;

            if (copyStatus != CopyStatus.Success)
            {
                throw new Exception($"Copy of shard {shardId} has copy status '{copyStatus}'.");
            }

            if (accessTier == "Archive")
            {
                // Quite strange, but it seems that directly after a copy, the copy has access tier 'Archive' instead of the requested
                // tier 'Hot'.
                throw new Exception(
                    $"Copy of shard {shardId} has access tier '{accessTier}'. " +
                    $"Please wait for the access tier to become 'Hot'.");
            }
        }

        private static ShardId GetShardIdFromBlobName(string name)
        {
            try
            {
                return new ShardId(name[^(Constants.SHARD_ID_BYTES * 2)..].GetByteArrayFromHexString());
            }
            catch (Exception ex)
            {
                throw new Exception($"Could not extract shard ID from blob '{name}'.", ex);
            }
        }

        private async Task DeleteBlob(IBlobClient blob, string? title = null)
        {
            title ??= GetFileName(blob.Name);

            if (_settings.Fake)
            {
                Helper.WriteLine($"Deleted blob '{title}' (fake).");

                return;
            }

            if (await blob.DeleteIfExists())
            {
                Helper.WriteLine($"Deleted blob '{title}'.");
            }
        }

        /// <summary>
        /// If the blob already exists then it's overwritten.
        /// </summary>
        private async Task UploadFile(
            byte[] bytes,
            IBlobClient blob,
            string? title = null,
            bool useCompression = false,
            AccessTier? accessTier = null,
            bool useEncryption = true)
        {
            title ??= GetFileName(blob.Name);

            if (_settings.Fake)
            {
                Helper.WriteLine($"Uploading '{title}' (fake).");

                return;
            }

            if (useCompression)
            {
                bytes = GZipProvider.Compress(bytes);
            }

            var encryptedBytes = useEncryption ? _cipher.Encrypt(bytes) : bytes;
            Helper.WriteLine($"Uploading '{title}' {encryptedBytes.Length.GetPrettyBytes()}");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await blob.Upload(encryptedBytes, accessTier);
            }
            catch
            {
                Helper.WriteLine(
                    $"Uploading '{title}' {encryptedBytes.Length.GetPrettyBytes()} failed after {stopwatch.GetPrettyElapsedTime()}.");

                throw;
            }

            stopwatch.Stop();
            var avgBytes = (long)Math.Round(encryptedBytes.Length / stopwatch.Elapsed.TotalSeconds);
            var avgSuffix = encryptedBytes.Length > 0 ? $" (avg {avgBytes.GetPrettyBytes(decimals: 1)}/s)" : "";
            Helper.WriteLine(
                $"Uploading '{title}' {encryptedBytes.Length.GetPrettyBytes()} took {stopwatch.GetPrettyElapsedTime()}{avgSuffix}.");
        }

        private async Task<byte[]> DownloadFile(
            IBlobClient blob,
            string? title = null,
            bool useCompression = false,
            bool useEncryption = true)
        {
            title ??= GetFileName(blob.Name);
            Helper.WriteLine($"Downloading '{title}'");
            var stopwatch = Stopwatch.StartNew();
            var encryptedBytes = await blob.Download();
            stopwatch.Stop();

            if ((useEncryption ? _cipher.TryDecrypt(encryptedBytes) : encryptedBytes) is not byte[] bytes)
            {
                throw new Exception($"Could not decrypt blob '{blob.Name}'.");
            }

            var avgBytes = (long)Math.Round(encryptedBytes.Length / stopwatch.Elapsed.TotalSeconds);
            var avgSuffix = encryptedBytes.Length > 0 ? $" (avg {avgBytes.GetPrettyBytes(decimals: 1)}/s)" : "";
            Helper.WriteLine($"Downloading '{title}' {encryptedBytes.Length.GetPrettyBytes()} took {stopwatch.GetPrettyElapsedTime()}{avgSuffix}.");

            return useCompression ? GZipProvider.Decompress(bytes) : bytes;
        }

        private IBlobClient GetIndexBlob() => Container.GetBlobClient($"{_settings.BlobStorageFolder}/index");

        private IBlobClient GetIndexBackupBlob() => Container.GetBlobClient($"{_settings.BlobStorageFolder}/index-backups/{DateTime.UtcNow.Ticks}");

        private IBlobClient GetMainCipherKeyBlob() => Container.GetBlobClient($"{_settings.BlobStorageFolder}/main-cipher-key");

        private IBlobClient GetShardTokenBlob(ShardId shardId)
        {
            return Container.GetBlobClient($"{GetShardTokensFolder()}/{shardId.Bytes.GetHexString()}");
        }

        private IBlobClient GetShardBlob(ShardId shardId)
        {
            return Container.GetBlobClient($"{_settings.BlobStorageFolder}/shards/{shardId.Bytes.GetHexString()}");
        }

        private IBlobClient GetHotShardBlob(ShardId shardId)
        {
            return Container.GetBlobClient($"{_settings.BlobStorageFolder}/hot-shards/{shardId.Bytes.GetHexString()}");
        }

        private IBlobClient GetShardsChangedBlob() => Container.GetBlobClient($"{_settings.BlobStorageFolder}/shards-changed");

        private string GetShardTokensFolder() => $"{_settings.BlobStorageFolder}/shard-tokens";

        private IBlobContainerClient Container
        {
            get
            {
                if (_container is null)
                {
                    _container = _blobContainerProvider.GetContainer(_settings.Container);

                    if (_settings.CreateContainer)
                    {
                        // This line is not executed conditionally as it's another call to the blob storage.
                        _container.CreateIfNotExists().Wait();
                    }
                }

                return _container;
            }
        }

        private bool ShardCopyIsRequired
        {
            get
            {
                // The best check here would be "== AccessTier.Archive".
                // However, as the Azurite tests cannot use access tier Archive, "!= AccessTier.Hot" is used instead.
                return _settings.AccessTier != AccessTier.Hot;
            }
        }

        private static string GetFileName(string blobName)
        {
            var i = blobName.LastIndexOf('/');

            if (i >= 0)
            {
                return blobName[(i + 1)..];
            }

            return blobName;
        }
    }
}
