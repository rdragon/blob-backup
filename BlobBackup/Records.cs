using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BlobBackup;

public record FileProviderSettings(string FileSystemFolder, string? ChunksFolder);
public record BlobProviderSettings(string Container, bool CreateContainer, string BlobStorageFolder, AccessTier AccessTier, bool Fake, bool SevenZip, bool CompressAlways, bool NoCompression);
public record BackupProviderSettings(int ShardSizeBytes);
public record IndexSettings(bool ResetIndex);
public record SecretProviderSettings(string? SecretsFolder, string? ConnectionString, string? CipherSecret);
public record ChunkProviderSettings(int ShardSizeBytes);
public record RestoreProviderSettings(string? RestorePrefix);
public record AzureProviderSettings(string Container, bool CreateContainer);
public record BlobContainerProviderSettings(bool NoAzure);

/// <summary>
/// Represents the ID of a file inside the file system folder.
/// Consists only of the relative path of the file.
/// </summary>
public sealed record FileId(string RelativePath)
{
    public bool Equals(FileId? other)
    {
        return other is { } && RelativePath.Equals(other.RelativePath, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(RelativePath);

    public override string ToString() => RelativePath;
}

/// <summary>
/// Represents the ID of a shard.
/// Has length <see cref="Constants.SHARD_ID_BYTES"/>.
/// Shard IDs are random byte arrays.
/// </summary>
public sealed record ShardId(byte[] Bytes)
{
    public bool Equals(ShardId? other)
    {
        return other is { } && BytesEqualityComparer.Instance.Equals(Bytes, other.Bytes);
    }

    public override int GetHashCode() => BytesEqualityComparer.Instance.GetHashCode(Bytes);

    public override string ToString()
    {
        return Bytes.GetShortTitle();
    }
}

/// <summary>
/// Represents the ID of a chunk.
/// Has length <see cref="Constants.CHUNK_ID_BYTES"/>.
/// Equals a prefix of the SHA-256 hash of the contents of the chunk.
/// </summary>
public sealed record ChunkId(byte[] Bytes)
{
    public bool Equals(ChunkId? other)
    {
        return other is { } && BytesEqualityComparer.Instance.Equals(Bytes, other.Bytes);
    }

    public override int GetHashCode() => BytesEqualityComparer.Instance.GetHashCode(Bytes);

    public override string ToString()
    {
        return Bytes.GetShortTitle();
    }
}

/// <summary>
/// A wrapper around a byte array that has value equality semantics.
/// </summary>
public sealed record Raw(byte[] Bytes)
{
    public bool Equals(Raw? other)
    {
        return other is { } && BytesEqualityComparer.Instance.Equals(Bytes, other.Bytes);
    }

    public override int GetHashCode() => BytesEqualityComparer.Instance.GetHashCode(Bytes);

    public override string ToString()
    {
        return Bytes.GetShortTitle();
    }
}

/// <summary>
/// A shard token consists of a sequence of chunk tokens together with the compression type that was used to compress the contents
/// of the shard.
/// </summary>
public sealed record ShardToken(IReadOnlyList<ChunkToken> ChunkTokens, CompressionType CompressionType)
{
    public bool Equals(ShardToken? other)
    {
        return other is { } && ChunkTokens.SequenceEqual(other.ChunkTokens) && CompressionType == other.CompressionType;
    }

    public override int GetHashCode() => HashCode.Combine(ChunkTokens.GetSequenceEqualHashCode(), CompressionType);
}

/// <summary>
/// Represents a file or part of a file in the file system folder.
/// A chunk does not contain metadata about the file.
/// Moreover, a chunk does not contain any reference to the original file.
/// Small files (with a length at most the shard size) are represented by a single chunk.
/// Large files (with a length larger than the shard size) are represented by multiple chunks.
/// </summary>
public record Chunk(ChunkId ChunkId, Raw Raw);

/// <summary>
/// A chunk token consists of a chunk ID together with the length of the chunk in bytes.
/// </summary>
public record ChunkToken(ChunkId ChunkId, int Length);

/// <summary>
/// The index is stored in the blob storage using this structure.
/// </summary>
public sealed record IndexData(
    IReadOnlyList<FileId> FileIds,
    IReadOnlyList<FileToken> FileTokens,
    IReadOnlyList<ShardId> ShardIds,
    IReadOnlyList<ShardToken> ShardTokens)
{
    public bool Equals(IndexData? other)
    {
        return
            other is { } &&
            FileIds.SequenceEqual(other.FileIds) &&
            FileTokens.SequenceEqual(other.FileTokens) &&
            ShardIds.SequenceEqual(other.ShardIds) &&
            ShardTokens.SequenceEqual(other.ShardTokens);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            FileIds.GetSequenceEqualHashCode(),
            FileTokens.GetSequenceEqualHashCode(),
            ShardIds.GetSequenceEqualHashCode(),
            ShardTokens.GetSequenceEqualHashCode());
    }
}

/// <summary>
/// Consists of the length of a file, it's last write time, and a sequence of chunk IDs of the chunks that make up the file.
/// The order of the chunk IDs is important.
/// </summary>
public sealed record FileToken(long Length, DateTime LastWriteTimeUtc, IReadOnlyList<ChunkId> ChunkIds)
{
    public bool Equals(FileToken? other)
    {
        return
            other is { } &&
            Length == other.Length &&
            LastWriteTimeUtc == other.LastWriteTimeUtc &&
            ChunkIds.SequenceEqual(other.ChunkIds);
    }

    public override int GetHashCode() => HashCode.Combine(Length, LastWriteTimeUtc, ChunkIds.GetSequenceEqualHashCode());
}
