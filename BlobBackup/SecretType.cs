using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobBackup;

public enum SecretType
{
    ConnectionString = 0,
    CipherKey = 1,
}
