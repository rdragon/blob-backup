using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobBackup
{
    public class AzureBlobHierarcyItem : IBlobHierarchyItem
    {
        private readonly BlobHierarchyItem _blobHierarchyItem;

        public AzureBlobHierarcyItem(BlobHierarchyItem blobHierarchyItem)
        {
            _blobHierarchyItem = blobHierarchyItem;
        }

        public string BlobName => _blobHierarchyItem.Blob.Name;
    }
}
