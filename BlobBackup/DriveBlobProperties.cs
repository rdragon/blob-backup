using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobBackup
{
    public class DriveBlobProperties : IBlobProperties
    {
        public string AccessTier => "Hot";

        public CopyStatus CopyStatus => CopyStatus.Success;
    }
}
