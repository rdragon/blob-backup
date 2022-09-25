using Xunit;

namespace BlobBackup.Test;

public class SevenZipProviderTest
{
    [Theory]
    [ClassData(typeof(ZeroTo99))]
    public async Task Run(int dataLength)
    {
        var expectedBytes = dataLength.GetByteArray();
        var compressedBytes = await SevenZipProvider.Compress(expectedBytes);
        var actualBytes = await SevenZipProvider.Decompress(compressedBytes);

        Assert.Equal(expectedBytes, actualBytes);
    }
}
