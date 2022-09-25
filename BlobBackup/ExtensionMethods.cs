using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace BlobBackup;

public static class ExtensionMethods
{
    /// <summary>
    /// Returns the SHA-256 hash of the given bytes.
    /// The returned array has length 32.
    /// </summary>
    public static byte[] GetSha256Hash(this byte[] bytes)
    {
        return SHA256.HashData(bytes);
    }

    /// <summary>
    /// Returns a (lower case) hex string consisting of the characters 0-9a-f.
    /// The length of the string will be twice the length of the byte array.
    /// </summary>
    public static string GetHexString(this byte[] bytes)
    {
        // Copied from https://stackoverflow.com/questions/311165/how-do-you-convert-a-byte-array-to-a-hexadecimal-string-and-vice-versa/14333437#14333437.

        var chars = new char[bytes.Length * 2];
        int b;

        for (int i = 0; i < bytes.Length; i++)
        {
            b = bytes[i] >> 4;
            chars[i * 2] = (char)(87 + b + (((b - 10) >> 31) & -39));
            b = bytes[i] & 0xF;
            chars[i * 2 + 1] = (char)(87 + b + (((b - 10) >> 31) & -39));
        }

        return new string(chars);
    }

    public static byte[] GetByteArrayFromHexString(this string hex)
    {
        var length = hex?.Length ?? throw new ArgumentNullException(nameof(hex));

        if (length % 2 == 1)
        {
            throw new ArgumentException($"Hex string cannot have odd length {length}.");
        }

        var bytes = new byte[length / 2];

        for (int i = 0; i < length; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hex[i..(i + 2)], 16);
        }

        return bytes;
    }

    /// <summary>
    /// Returns an array of length 32.
    /// </summary>
    public static byte[] GetCipherKey(this string text)
    {
        if (text.Length == 44 && text[^1] == '=')
        {
            var key = new byte[32];

            if (Convert.TryFromBase64String(text, key, out var bytesWritten) && bytesWritten == 32)
            {
                return key;
            }
        }

        if (text.Length == 64 && text.All(c => '0' <= c && c <= '9' || 'a' <= c && c <= 'f' || 'A' <= c && c <= 'F'))
        {
            return text.GetByteArrayFromHexString();
        }

        Helper.WriteLine("Using SHA-256 to construct a 32 byte cipher key from the given input.");

        return Encoding.UTF8.GetBytes(text).GetSha256Hash();
    }

    /// <summary>
    /// Returns e.g. "10.40 MiB".
    /// </summary>
    public static string GetPrettyBytes(this int bytes) => GetPrettyBytes((long)bytes);

    /// <summary>
    /// Returns e.g. "10.40 MiB".
    /// </summary>
    public static string GetPrettyBytes(this long bytes, int decimals = 2)
    {
        string sign;

        if (bytes < 0 && bytes != long.MinValue)
        {
            sign = "-";
            bytes *= -1;
        }
        else
        {
            sign = "";
        }

        if (bytes <= 999)
        {
            return $"{sign}{bytes} B";
        }

        var value = (double)bytes / 1024;

        if (value <= 999)
        {
            return $"{sign}{Round(value)} KiB";
        }

        value /= 1024;

        if (value <= 999)
        {
            return $"{sign}{Round(value)} MiB";
        }

        value /= 1024;

        if (value <= 999)
        {
            return $"{sign}{Round(value)} GiB";
        }

        value /= 1024;

        if (value <= 999)
        {
            return $"{sign}{Round(value)} TiB";
        }

        value /= 1024;

        return $"{sign}{Round(value)} PiB";

        string Round(double value)
        {
            value = Math.Round(value, decimals, MidpointRounding.AwayFromZero);

            return value.ToString($"0.{new string('0', decimals)}");
        }
    }

    /// <summary>
    /// See <see cref="GetPrettyTime(TimeSpan, int)"/>.
    /// </summary>
    public static string GetPrettyElapsedTime(this Stopwatch stopwatch) => stopwatch.Elapsed.GetPrettyTime();

    /// <summary>
    /// Returns e.g. "4.2 ms" if below one second.
    /// Returns e.g. "4.2 sec" if below one minute.
    /// Returns e.g. "4.2 min" if below one hour.
    /// Returns e.g. "4.2 hours" if below one day.
    /// Returns e.g. "4.2 days" otherwise.
    /// </summary>
    public static string GetPrettyTime(this TimeSpan timeSpan, int decimals = 1)
    {
        var totalMilliSeconds = timeSpan.TotalMilliseconds;
        double number;
        string kind;

        if (totalMilliSeconds < 999)
        {
            number = totalMilliSeconds;
            kind = "ms";
        }
        else if (totalMilliSeconds < 60_000)
        {
            number = timeSpan.TotalSeconds;
            kind = "sec";
        }
        else if (totalMilliSeconds < 3_600_000)
        {
            number = timeSpan.TotalMinutes;
            kind = "min";
        }
        else if (totalMilliSeconds < 86_400_000)
        {
            number = timeSpan.TotalHours;
            kind = "hours";
        }
        else
        {
            number = timeSpan.TotalDays;
            kind = "days";
        }

        var formattedNumber = FormattableString.Invariant(decimals switch
        {
            0 => $"{number:0}",
            1 => $"{number:0.0}",
            2 => $"{number:0.00}",
            3 => $"{number:0.000}",
            _ => throw new ArgumentOutOfRangeException(nameof(decimals)),
        });

        return $"{formattedNumber} {kind}";
    }

    public static ChunkToken GetChunkToken(this Chunk chunk) => new ChunkToken(chunk.ChunkId, chunk.Raw.Bytes.Length);

    public static string GetShortTitle(this byte[] bytes)
    {
        return bytes[..4].GetHexString();
    }

    public static bool DictionaryEqual<TKey, TValue>(
        this IReadOnlyDictionary<TKey, TValue> left,
        IReadOnlyDictionary<TKey, TValue> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        var equalityComparer = EqualityComparer<TValue>.Default;

        foreach (var (key, leftValue) in left)
        {
            if (!right.TryGetValue(key, out var rightValue) || !equalityComparer.Equals(leftValue, rightValue))
            {
                return false;
            }
        }

        return true;
    }

    public static int GetSequenceEqualHashCode<T>(this IReadOnlyList<T> values)
    {
        return HashCode.Combine(values.Count, values.FirstOrDefault());
    }

    /// <summary>
    /// Like <see cref="CryptoStream.Read(byte[], int, int)"/> but reads as much bytes as possible.
    /// </summary>
    public static int ReadAll(this CryptoStream cryptoStream, byte[] bytes, int offset, int count)
    {
        var totalBytesRead = 0;
        int bytesRead;

        do
        {
            bytesRead = cryptoStream.Read(bytes, offset, count);
            totalBytesRead += bytesRead;
            offset += bytesRead;
            count -= bytesRead;
        } while (bytesRead > 0 && count > 0);

        return totalBytesRead;
    }
}
