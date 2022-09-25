﻿using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobBackup;

public class AzureBlobProperties : IBlobProperties
{
    private readonly BlobProperties _blobProperties;

    public AzureBlobProperties(BlobProperties blobProperties)
    {
        _blobProperties = blobProperties;
    }

    public string AccessTier => _blobProperties.AccessTier;

    public CopyStatus CopyStatus => _blobProperties.CopyStatus;
}
