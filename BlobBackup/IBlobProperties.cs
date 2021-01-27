using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobBackup
{
    public interface IBlobProperties
    {
        string AccessTier { get; }
        CopyStatus CopyStatus { get; }
    }
}
