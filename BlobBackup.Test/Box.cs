using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobBackup.Test
{
    public record Box(BlobProvider BlobProvider, BackupProvider BackupProvider, RestoreProvider RestoreProvider, FileProvider FileProvider);
}
