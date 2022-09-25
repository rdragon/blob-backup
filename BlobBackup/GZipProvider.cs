using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobBackup;

public class GZipProvider
{
    public static byte[] Compress(byte[] bytes)
    {
        using var outputStream = new MemoryStream();

        using (var inputStream = new MemoryStream(bytes))
        using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal))
        {
            inputStream.CopyTo(gzipStream);
        }

        return outputStream.ToArray();
    }

    public static byte[] Decompress(byte[] bytes)
    {
        using var inputStream = new MemoryStream(bytes);
        using var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();
        gzipStream.CopyTo(outputStream);
        return outputStream.ToArray();
    }
}
