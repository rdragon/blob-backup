using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BlobBackup
{
    public class FileProvider
    {
        /// <summary>
        /// The "relative path" of a file inside the file system folder uses this character as directory separater.
        /// </summary>
        private const char RELATIVE_PATH_DIRECTORY_SEPARATOR = '/';

        private readonly FileProviderSettings _settings;

        public FileProvider(FileProviderSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Returns the IDs of all files inside the file system folder.
        /// Files matching one of the regexes inside '.bbignore' are not returned.
        /// The '.bbignore' file itself is also not returned.
        /// </summary>
        public IEnumerable<FileId> GetAllFileIds()
        {
            if (!Directory.Exists(_settings.FileSystemFolder))
            {
                throw new DirectoryNotFoundException($"Source folder '{_settings.FileSystemFolder}' not found.");
            }

            var ignoreRegexes = GetIgnoreRegexes().ToArray();

            foreach (var path in Directory.GetFiles(_settings.FileSystemFolder, "*", SearchOption.AllDirectories))
            {
                var name = Path.GetFileName(path);

                if (name.Equals(".bbignore", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!path.StartsWith(_settings.FileSystemFolder, StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception($"Expecting path '{path}' to start with '{_settings.FileSystemFolder}'.");
                }

                var relativePath = path[(_settings.FileSystemFolder.Length + 1)..]
                    .Replace(Path.DirectorySeparatorChar, RELATIVE_PATH_DIRECTORY_SEPARATOR);

                if (ignoreRegexes.Any(regex => regex.IsMatch(relativePath)))
                {
                    continue;
                }

                yield return new FileId(relativePath);
            }
        }

        public (long length, DateTime lastWriteTimeUtc, bool hidden) GetFileInfo(FileId fileId)
        {
            var fileInfo = new FileInfo(GetPath(fileId));

            return (fileInfo.Length, fileInfo.LastWriteTimeUtc, fileInfo.Attributes.HasFlag(FileAttributes.Hidden));
        }

        public async Task<byte[]> ReadAllBytes(FileId fileId) => await File.ReadAllBytesAsync(GetPath(fileId));

        public FileStream OpenRead(FileId fileId)
        {
            return File.OpenRead(GetPath(fileId));
        }

        public FileStream OpenWrite(FileId fileId)
        {
            var path = GetPath(fileId);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            return File.OpenWrite(path);
        }

        public async Task WriteAllBytes(FileId fileId, byte[] bytes)
        {
            await WriteAllBytesAbsolutePath(GetPath(fileId), bytes);
        }

        public async Task WriteChunk(ChunkId chunkId, byte[] bytes)
        {
            await WriteAllBytesAbsolutePath(GetChunkPath(chunkId), bytes);
        }

        public void DeleteChunks()
        {
            var folder = GetChunksFolder();

            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }

        public async Task WriteChunkToStream(ChunkId chunkId, Stream stream)
        {
            using var fileStream = File.OpenRead(GetChunkPath(chunkId));
            await fileStream.CopyToAsync(stream);
        }

        public void RequireEmptyFileSystemFolder()
        {
            if (Directory.Exists(_settings.FileSystemFolder) &&
                Directory.GetFiles(_settings.FileSystemFolder).Length + Directory.GetDirectories(_settings.FileSystemFolder).Length > 0)
            {
                throw new IOException($"Source folder '{_settings.FileSystemFolder}' is not empty.");
            }
        }

        public void DeleteFileSystemFolderContents()
        {
            if (Directory.Exists(_settings.FileSystemFolder))
            {
                Directory.Delete(_settings.FileSystemFolder, recursive: true);
                Directory.CreateDirectory(_settings.FileSystemFolder);
            }
        }

        public void DeleteFile(FileId fileId)
        {
            var path = GetPath(fileId);

            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public bool ChunkExists(ChunkId chunkId)
        {
            return File.Exists(GetChunkPath(chunkId));
        }

        public void SetLastWriteTimeUtc(FileId fileId, DateTime lastWriteTimeUtc)
        {
            new FileInfo(GetPath(fileId)).LastWriteTimeUtc = lastWriteTimeUtc;
        }

        private string GetChunkPath(ChunkId chunkId)
        {
            return Path.Combine(GetChunksFolder(), chunkId.Bytes.GetHexString());
        }

        private string GetChunksFolder()
        {
            if (_settings.ChunksFolder is { })
            {
                return _settings.ChunksFolder;
            }

            return _settings.FileSystemFolder + "-chunks-cache";
        }

        private string GetPath(FileId fileId)
        {
            return Path.Combine(
                _settings.FileSystemFolder,
                fileId.RelativePath.Replace(RELATIVE_PATH_DIRECTORY_SEPARATOR, Path.DirectorySeparatorChar));
        }

        private IEnumerable<Regex> GetIgnoreRegexes()
        {
            var path = Path.Combine(_settings.FileSystemFolder, ".bbignore");

            if (File.Exists(path))
            {
                foreach (var line in File.ReadAllLines(path))
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        yield return new Regex(line, RegexOptions.IgnoreCase);
                    }
                }
            }
        }

        private static async Task WriteAllBytesAbsolutePath(string path, byte[] bytes)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllBytesAsync(path, bytes);
        }
    }
}
