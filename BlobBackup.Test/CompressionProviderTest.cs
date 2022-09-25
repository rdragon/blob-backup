using Xunit;

namespace BlobBackup.Test;

public class CompressionProviderTest
{
    [Fact]
    public void CompressAndDecompress()
    {
        var expected = 9.GetByteArray();
        var actual = GZipProvider.Decompress(GZipProvider.Compress(expected));

        Assert.Equal(expected, actual);
    }
}
