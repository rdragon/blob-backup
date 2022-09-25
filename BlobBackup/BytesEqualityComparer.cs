using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobBackup;

/// <summary>
/// Compares byte arrays by their contents.
/// The hash code is extracted from the first four bytes.
/// </summary>
public class BytesEqualityComparer : IEqualityComparer<byte[]>
{
    public static BytesEqualityComparer Instance { get; } = new();

    public bool Equals(byte[]? left, byte[]? right)
    {
        if (left is null)
        {
            return right is null;
        }

        return right?.SequenceEqual(left) ?? false;
    }

    public int GetHashCode([DisallowNull] byte[] obj)
    {
        return BitConverter.ToInt32(obj, 0);
    }
}
