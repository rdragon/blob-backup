using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace BlobBackup.Test
{
    public class FileProviderTest
    {
        [Theory]
        [ClassData(typeof(Seeds))]
        public async Task ReadAllBytes(int seed)
        {
            await TestWithFile(seed.GetRandom(), async (fileProvider, fileId, fileToken) =>
            {
                var actual = await fileProvider.ReadAllBytes(fileId);

                Assert.Equal(fileToken, actual);
            });
        }

        [Theory]
        [ClassData(typeof(Seeds))]
        public async Task GetFileIds(int seed)
        {
            await TestWithFile(seed.GetRandom(), async (fileProvider, fileId, _) =>
            {
                await Task.CompletedTask;
                var actual = fileProvider.GetAllFileIds().ToArray();

                Assert.Equal(new[] { fileId }, actual);
            });
        }

        private static async Task TestWithFile(Random random, TestWithFileFunc func)
        {
            using var temporaryFolder = new TemporaryFolder();
            using var chunksFolder = new TemporaryFolder();
            var fileProvider = new FileProvider(new FileProviderSettings(temporaryFolder.Path, chunksFolder.Path));
            var fileId = random.GetFileId();
            var bytes = random.GetBytes();
            await fileProvider.WriteAllBytes(fileId, bytes);
            await func(fileProvider, fileId, bytes);
        }

        delegate Task TestWithFileFunc(FileProvider fileProvider, FileId fileId, byte[] bytes);
    }
}
