using Azure.Storage.Blobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobBackup.Test
{
    public sealed class TemporaryContainer : IDisposable
    {
        public string Container { get; }

        public TemporaryContainer()
        {
            Container = TestHelper.GetRandomUniqueString();
        }

        public static string ConnectionString => "UseDevelopmentStorage=true";

        public void Dispose()
        {
            new BlobServiceClient(ConnectionString).GetBlobContainerClient(Container).DeleteIfExists();
        }
    }
}
