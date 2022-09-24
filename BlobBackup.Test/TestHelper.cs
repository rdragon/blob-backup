using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BlobBackup.Test
{
    public static class TestHelper
    {
        /// <summary>
        /// Returns a random unique string of length 32 consisting of the characters 0-9a-f.
        /// </summary>
        public static string GetRandomUniqueString() => RandomNumberGenerator.GetBytes(16).GetHexString();

        public static async Task RunWithInstanceFactory<T>(Random random, Func<Func<T>, Task> func) where T : notnull
        {
            await RunWithServiceProviderFactory(random, async createServiceProvider =>
            {
                await func(CreateInstance);

                T CreateInstance() => createServiceProvider().GetRequiredService<T>();
            });
        }

        public static async Task RunWithInstance<T>(Random random, Func<T, Task> func) where T : notnull
        {
            await RunWithServiceProviderFactory(random, async createServiceProvider =>
            {
                await func(createServiceProvider().GetRequiredService<T>());
            });
        }

        public static async Task RunWithServiceProvider(Random random, Func<IServiceProvider, Task> func)
        {
            await RunWithServiceProviderFactory(random, async createServiceProvider =>
            {
                await func(createServiceProvider());
            });
        }

        public static async Task RunWithBoxFactory(Random random, Func<Func<Box>, Task> func)
        {
            await RunWithServiceProviderFactory(random, async createServiceProvider =>
            {
                await func(CreateBox);

                Box CreateBox()
                {
                    var serviceProvider = createServiceProvider();

                    return new Box(
                        serviceProvider.GetRequiredService<BlobProvider>(),
                        serviceProvider.GetRequiredService<BackupProvider>(),
                        serviceProvider.GetRequiredService<RestoreProvider>(),
                        serviceProvider.GetRequiredService<FileProvider>());
                }
            });
        }

        public static async Task RunWithServiceProviderFactory(Random random, Func<Func<IServiceProvider>, Task> func)
        {
            var noAzure = random.Next(2) == 0;
            using var fileSystemFolder = new TemporaryFolder();
            using var chunksFolder = new TemporaryFolder();
            using var azureContainer = new TemporaryContainer();
            using var driveContainer = new TemporaryFolder();

            // Azurite version 3.10.0 doesn't handle archived blobs correctly. It doesn't allow copying an archived blob.
            // That's why access tier Cool is used instead.
            var accessTier = AccessTier.Cool;

            await func(CreateServiceProvider);

            IServiceProvider CreateServiceProvider()
            {
                return ServiceProviderProvider.CreateServiceProvider(
                    accessTier: accessTier,
                    fileSystemFolder: fileSystemFolder.Path,
                    chunksFolder: chunksFolder.Path,
                    container: noAzure ? driveContainer.Path : azureContainer.Container,
                    blobStorageFolder: TestConstants.EXAMPLE_NAME,
                    restorePrefix: null,
                    resetIndex: false,
                    createContainer: true,
                    fake: false,
                    sevenZip: random.Next(2) == 0,
                    verbose: false,
                    compressAlways: random.Next(2) == 0,
                    noCompression: false,
                    deleteSecrets: false,
                    noAzure: noAzure,
                    secretsFolder: null,
                    connectionString: "UseDevelopmentStorage=true",
                    cipherSecret: 32.GetByteArray().GetHexString(),
                    shardSizeBytes: 1).Result;
            }
        }
    }
}
