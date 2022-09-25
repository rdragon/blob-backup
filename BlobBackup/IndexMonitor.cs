using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobBackup;

/// <summary>
/// Keeps track of whether shards have been created or deleted and the stored index doesn't know this yet.
/// The state of this class is persisted between multiple runs of the application.
/// </summary>
public class IndexMonitor
{
    private readonly BlobProvider _blobProvider;

    public IndexMonitor(BlobProvider blobProvider)
    {
        _blobProvider = blobProvider;
    }


    private bool? _shardsChanged;

    public bool ShardsChanged
    {
        get
        {
            if (_shardsChanged is null)
            {
                _shardsChanged = _blobProvider.GetShardsChanged().Result;
            }

            return _shardsChanged.Value;
        }
        set
        {
            if (ShardsChanged == value)
            {
                return;
            }

            _blobProvider.SetShardsChanged(value).Wait();
            _shardsChanged = value;
        }
    }
}
