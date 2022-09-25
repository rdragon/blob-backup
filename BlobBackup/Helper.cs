using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobBackup;

public static class Helper
{
    public static void WriteLine()
    {
        Console.WriteLine();
    }

    public static void WriteLine(string value)
    {
        Console.WriteLine($"{DateTimeOffset.Now:HH:mm:ss.fff} {value}");
    }
}
