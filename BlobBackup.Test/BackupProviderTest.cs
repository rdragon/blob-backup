using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace BlobBackup.Test
{
    public class BackupProviderTest
    {
        [Theory]
        [ClassData(typeof(Seeds))]
        public async Task CreateFile(int seed)
        {
            var random = seed.GetRandom();

            await TestHelper.RunWithBoxFactory(random, async createBox =>
            {
                var box = createBox();
                var fileId = random.GetFileId();
                var expectedBytes = random.GetBytes();
                await box.FileProvider.WriteAllBytes(fileId, expectedBytes);
                await box.BackupProvider.BackupFileSystemFolder();
                box.FileProvider.DeleteFileSystemFolderContents();

                box = createBox();
                await box.RestoreProvider.CopyShards();
                await box.RestoreProvider.RestoreFileSystemFolder();
                var actualBytes = await box.FileProvider.ReadAllBytes(fileId);

                Assert.Equal(expectedBytes, actualBytes);
            });
        }

        [Theory]
        [ClassData(typeof(Seeds))]
        public async Task ModifyFile(int seed)
        {
            var random = seed.GetRandom();

            await TestHelper.RunWithBoxFactory(random, async createBox =>
            {
                var box = createBox();
                var fileId = random.GetFileId();
                await box.FileProvider.WriteAllBytes(fileId, random.GetBytes());
                await box.BackupProvider.BackupFileSystemFolder();

                box = createBox();
                var expectedBytes = random.GetBytes();
                await box.FileProvider.WriteAllBytes(fileId, expectedBytes);
                await box.BackupProvider.BackupFileSystemFolder();
                box.FileProvider.DeleteFileSystemFolderContents();

                box = createBox();
                await box.RestoreProvider.CopyShards();
                await box.RestoreProvider.RestoreFileSystemFolder();
                var actualBytes = await box.FileProvider.ReadAllBytes(fileId);

                Assert.Equal(expectedBytes, actualBytes);
            });
        }

        [Theory]
        [ClassData(typeof(Seeds))]
        public async Task DeleteFile(int seed)
        {
            var random = seed.GetRandom();

            await TestHelper.RunWithBoxFactory(random, async createBox =>
            {
                var box = createBox();
                await box.FileProvider.WriteAllBytes(random.GetFileId(), random.GetBytes());
                await box.BackupProvider.BackupFileSystemFolder();

                box = createBox();
                box.FileProvider.DeleteFileSystemFolderContents();
                await box.BackupProvider.BackupFileSystemFolder();

                box = createBox();
                await box.RestoreProvider.CopyShards();
                await box.RestoreProvider.RestoreFileSystemFolder();

                box.FileProvider.RequireEmptyFileSystemFolder();
            });
        }

        [Theory]
        [ClassData(typeof(Seeds))]
        public async Task CreateTwoFiles(int seed)
        {
            var random = seed.GetRandom();

            await TestHelper.RunWithBoxFactory(random, async createBox =>
            {
                var box = createBox();
                var fileId1 = random.GetFileId();
                var fileId2 = random.GetFileId();
                var expectedBytes1 = random.GetBytes();
                var expectedBytes2 = random.Next(2) == 0 ? expectedBytes1 : random.GetBytes();
                await box.FileProvider.WriteAllBytes(fileId1, expectedBytes1);
                await box.FileProvider.WriteAllBytes(fileId2, expectedBytes2);
                await box.BackupProvider.BackupFileSystemFolder();
                box.FileProvider.DeleteFileSystemFolderContents();

                box = createBox();
                await box.RestoreProvider.CopyShards();
                await box.RestoreProvider.RestoreFileSystemFolder();
                var actualBytes1 = await box.FileProvider.ReadAllBytes(fileId1);
                var actualBytes2 = await box.FileProvider.ReadAllBytes(fileId2);

                Assert.Equal(expectedBytes1, actualBytes1);
                Assert.Equal(expectedBytes2, actualBytes2);
            });
        }
    }
}
