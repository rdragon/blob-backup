# Blob Backup
Store long-term backups in the Azure Blob Storage archive access tier.

## Quick Start
- Install the [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
- Obtain an [Azure Blob Storage](https://azure.microsoft.com/en-us/services/storage/blobs/#get-started) connection string
- Backup the `.git` folder: `dotnet run -p BlobBackup -- .git git -cc`. When asked for a connection string, paste the connection string from the previous step. When asked for a cipher key, enter any string.

## Features
- Optimized for a small number of Blob Storage read and write operations by storing the backup in 512 MiB files. This decreases the Azure costs.
- Very low [storage costs](https://azure.microsoft.com/en-us/pricing/details/storage/blobs) (e.g. €0.01 per gigabyte per year).
- [AES-256](https://en.wikipedia.org/wiki/Advanced_Encryption_Standard) is used to encrypt the files that are uploaded to the Blob Storage.
- [GZip](https://www.gzip.org/) or [7-Zip](https://www.7-zip.org/) (if installed) can be used to compress the files that are uploaded to the Blob Storage.
- You can change your cipher key at any moment, because the main cipher key (which is used to encrypt all backup data) is stored in an encrypted file in the Blob Storage.
- Incremental backups: only new and changed files are transfered when you backup the same folder again.
- Files with the same byte contents only take up space once in the backup.
- The contents of a renamed or moved file do not need to be uploaded again if the file had already been backed up before.
- Instead of using the Azure Blob Storage archive access tier you can also use the cool or hot tiers.
- Instead of using the Azure Blob Storage you can also use the file system as backup medium.
- Specific files or folder can be excluded from the backup by adding a `.bbignore` file.
- The application is cross-platform. It has been tested on Windows and Mac. It should also run on Linux.

## Command Line Arguments
Usage: `blob-backup <file-system-folder> <blob-storage-folder> [options]`
- `<file-system-folder>`: Required. The folder to backup or restore.
- `<blob-storage-folder>`: Required. The blob storage folder to use.

| Argument | Description |
| --- | --- |
| `‑?` or `‑h` or `‑‑help`          | Show help information |
| `‑at <tier>` or `‑‑access‑tier <tier>`   | The access tier to use for the shards. Possible values: archive, cool, hot. Archive is the default value. |
| `‑c <name>` or `‑‑container <name>`      | The name of the blob container to use. Defaults to "blob‑backup". |
| `‑cc` or `‑‑create‑container`     | Create the blob container (if it doesn't exist) before any other operation is done. |
| `‑ck` or `‑‑change‑key`           | Change the cipher key. No backup or restore is done. After changing the cipher key you manually need to remove the old main cipher key backup, otherwise you can still obtain the main cipher key by using the old cipher key and the backup. |
| `‑cs` or `‑‑copy‑shards`          | Copy the shards to the hot access tier. |
| `‑ds` or `‑‑delete‑secrets`       | Delete the saved secrets so that new secrets can be submitted. Only the secrets for the current secrets identifier (see `--secret`) are deleted. |
| `‑f` or `‑‑fake`                  | Do not upload anything, but show what would have been done. |
| `‑na` or `‑‑no‑azure`             | Do not use Azure but use the file system instead. |
| `‑nc` or `‑‑no‑compression`       | Do not compress shards. |
| `‑pi` or `‑‑print‑index`          | Print the index. No backup or restore is done. |
| `‑r` or `‑‑restore`               | Restore the file system folder (instead of doing a backup). The folder needs to be empty. |
| `‑ri` or `‑‑reset‑index`          | Replace the shard tokens in the index by the shard tokens found in the `shard‑tokens` folder in the blob storage. Also, file tokens that reference non‑existing chunks are deleted. |
| `‑rp <path>` or `‑‑restore‑prefix <path>` | Only restore the files of which the relative path starts with the given path. |
| `‑s <identifier>` or `‑‑secrets <identifier>`  | The secrets to use. For each identifier the secrets are stored separately. |
| `-sf <path>` or `--secrets-folder <path>` | The folder to store the secrets. |
| `‑smk` or `‑‑set‑main‑key`        | Set the main cipher key. This command is only for debugging, as you normally do not know the main cipher key. No backup or restore is done. |
| `‑ss <MiB>` or `‑‑shard‑size <MiB>`     | The target shard size. Defaults to 512 MiB. If you change the shard size of an existing backup then files larger than the shard size need to be uploaded again after an index reset. |
| `‑v` or `‑‑verbose`               | Print debugging information. |
| `‑7` or `‑‑7zip`                  | Use 7‑Zip to compress the files. To use this feature the program 7‑Zip needs to be installed on the system and the PATH environment variable needs to include the path to the `7z` executable. |

## Known Bugs / Limitations
- If you delete a file the size of the backup does not decrease. This is because the application cannot yet remove unreferenced chunks. This will be added soon.
- The application has high memory usage. More than 6 GB is possible. One way to solve this is to replace byte array operations with stream operations.
- Almost no [key stretching](https://en.wikipedia.org/wiki/Key_stretching) is done on the cipher key. If you do not provide a 32 byte key then SHA-256 is used to obtain a 32 byte key.
- On non-Windows systems the connection string and cipher key are stored on the file system with almost no protection. They are stored encrypted, but the encryption key can be found in the source code of this application. On Windows the class [ProtectedData](https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.protecteddata) is used instead.
- Empty folders are not included in the backup.
- You cannot backup the root folder (e.g. `C:\` in Windows).

## Technical Overview
### Files
The following files (or blobs) are stored in the Azure Blob Storage.
| Path | Description |
| --- | --- |
| `main‑cipher‑key` | Contains the main cipher key that is used to decrypt all other files. This is the only file that is encrypted with the cipher key of the user. |
| `index` | Contains a list of all the files in the file system folder. For each file the following properties are stored: relative path, size, last modified date, and a list of chunk IDs of the chunks that make up the file. The index also contains a list of all the shards. For each shard the following properties are stored: its ID, a compression algorithm, and a list of chunk IDs and chunk lengths of the chunks that make up the shard. |
| `shards/<id>` | A shard contains the contents of multiple files, a single file, or part of a file. Most shards are around 512 MiB in size. A shard does not contain any metadata. Shards are the only files that are stored in the archive access tier. |
| `shard‑tokens/<id>` | Each shard has a corresponding shard token. A shard token contains a list of chunk IDs and chunk sizes of the chunks that make up the shard. It also contains the compression algorithm that was used to compress the shard. Without this file a shard is almost useless. |
| `shards‑changed` | An empty file that if exists signals that shards have been added or removed and the `index` file has not been updated yet. |

All files (except for the empty `shards-changed` file) are encrypted before being sent to the Blob Storage.

### Backup operation
Like most operations, this operation starts by downloading the main cipher key and the index. Then all files inside the file system folder are enumerated. If a file with the same relative path, the same size and the same last modified date is found in the index, then the program moves to the next file. Otherwise the contents of the file are read in chunks of 512 MiB. All chunks that are not found in the index are uploaded to the Blob Storage. Finally the file and the uploaded chunks (if any) are added to the index.

After all files have been enumerated the operation ends with the following two actions. First all files in the index that no longer exist in the file system are removed from the index. Second (and finally) the changed index is saved by uploading it to the Blob Storage.

### Restore operation
The restore operation consists of two stages. In the first stage the shards are copied to the online hot access tier. This is also known as [rehydration](https://docs.microsoft.com/en-us/azure/storage/blobs/storage-blob-rehydration). This is required because you cannot directly download from the offline archive tier. This stage might take up to 15 hours. Use the argument `--copy-shards` to start the rehydration process.

In the second stage all relevant shards are downloaded. For each shard its relevant chunks are temporarily stored on the file system (in a folder next to the file system folder). Then all files in the index are create one by one, using the chunks to restore the file contents. Also the last modified date of each file is restored. This stage can be started with the argument `--restore`. 

### The `.bbignore` file
To exclude specific files or folders from the backup you can create a file named `.bbignore`. Put this file in the root of the file system folder. A [regex pattern](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference) can be added to each line of this file. Files with a relative path that match any of these regex patterns will not be included in the backup.

The paths are taken relative to the file system folder. Also, the path delimiter `/` is used in the relative path. For example, if the file system folder has path `C:\data`, then the file `C:\data\folder\file` has relative path `folder/file`. The regex pattern `^folder/file$` would match exactly this file.

### Index reset
If the program stops (e.g. an error occurs) before the index could be saved, then the next time the program automatically starts with an index reset. During an index reset, all shard tokens are downloaded from the blob storage to determine which shards (and therefore which chunks) can be found in the blob storage. The shards in the index are replaced by the found shards.

In addition, during an index reset all file tokens in the index that refer to unknown chunks are removed from the index.

### Unit tests
To run the unit tests you'll first need to install and start [Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite).
