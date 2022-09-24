using Azure.Core.Pipeline;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace BlobBackup
{
    public class ServiceProviderProvider
    {
        public static async Task<IServiceProvider> CreateServiceProvider(
            AccessTier accessTier,
            string fileSystemFolder,
            string container,
            string blobStorageFolder,
            string? restorePrefix,
            bool resetIndex,
            bool createContainer,
            bool fake,
            bool sevenZip,
            bool deleteSecrets,
            bool noCompression,
            bool noAzure,
            int shardSizeBytes,
            bool verbose,
            bool compressAlways = false,
            string? secretsFolder = null,
            string? connectionString = null,
            string? cipherSecret = null,
            string? chunksFolder = null)
        {
            if (noCompression && compressAlways)
            {
                throw new Exception($"Ambiguous compression settings found.");
            }

            if (noAzure)
            {
                accessTier = AccessTier.Hot;
            }

            blobStorageFolder = blobStorageFolder.TrimEnd('/');
            fileSystemFolder = Path.TrimEndingDirectorySeparator(Path.GetFullPath(fileSystemFolder));
            var secretProviderSettings = new SecretProviderSettings(secretsFolder, connectionString, cipherSecret);

            if (deleteSecrets)
            {
                DeleteSecrets(secretProviderSettings);
            }

            connectionString ??= await GetConnectionString(secretProviderSettings);
            var services = new ServiceCollection();
            services.TryAddSingleton(new BlobProviderSettings(container, createContainer, blobStorageFolder, accessTier, fake, sevenZip, compressAlways, noCompression));
            services.TryAddSingleton(new AzureProviderSettings(container, createContainer));
            services.TryAddSingleton(new FileProviderSettings(fileSystemFolder, chunksFolder));
            services.TryAddSingleton(new IndexSettings(resetIndex));
            services.TryAddSingleton(new BackupProviderSettings(shardSizeBytes));
            services.TryAddSingleton(new ChunkProviderSettings(shardSizeBytes));
            services.TryAddSingleton(new BlobContainerProviderSettings(noAzure));
            services.TryAddSingleton(new RestoreProviderSettings(restorePrefix));
            services.TryAddSingleton(secretProviderSettings);
            services.TryAddSingleton<BackupProvider>();
            services.TryAddSingleton<BlobContainerProvider>();
            services.TryAddSingleton<RestoreProvider>();
            services.TryAddSingleton<FileProvider>();
            services.TryAddSingleton<BlobProvider>();
            services.TryAddSingleton<Index>();
            services.TryAddSingleton<IndexMonitor>();
            services.TryAddSingleton<Cipher>();
            services.TryAddSingleton<SecretProvider>();
            services.TryAddSingleton<ChunkProvider>();
            services.TryAddSingleton<Serializer>();
            services.TryAddSingleton<MainCipherKeyLoader>();
            services.AddLogging();
            services.AddLogging(loggingBuilder =>
            {
                if (verbose)
                {
                    loggingBuilder.SetMinimumLevel(LogLevel.Debug);
                    loggingBuilder.AddDebug();
                    loggingBuilder.AddConsole();
                }
            });
            services.AddHttpClient(Constants.DEFAULT_HTTP_CLIENT, httpClient =>
            {
                httpClient.Timeout = Timeout.InfiniteTimeSpan;
            });
            services.AddAzureClients(builder =>
            {
                builder.AddBlobServiceClient(connectionString).ConfigureOptions((options, provider) =>
                {
                    // options.Retry.MaxRetries = 1;
                    options.Retry.NetworkTimeout = Timeout.InfiniteTimeSpan;
                    var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient(Constants.DEFAULT_HTTP_CLIENT);
                    options.Transport = new HttpClientTransport(httpClient);
                });
            });


            return services.BuildServiceProvider();
        }

        public static string DefaultDataFolder
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "blob-backup");
            }
        }

        private static async Task<string> GetConnectionString(SecretProviderSettings secretProviderSettings)
        {
            return await new SecretProvider(secretProviderSettings).GetSecret(SecretType.ConnectionString);
        }

        private static void DeleteSecrets(SecretProviderSettings secretProviderSettings)
        {
            new SecretProvider(secretProviderSettings).DeleteSecrets();
        }
    }
}
