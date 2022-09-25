using Xunit;

namespace BlobBackup.Test;

public class ExtensionMethodsTest
{
    [Fact]
    public void GetSha256Hash()
    {
        Assert.Equal(32, 100.GetByteArray().GetSha256Hash().Length);
    }

    [Theory]
    [MemberData(nameof(Data))]
    public void HexConversion(int dataLength)
    {
        var expected = dataLength.GetByteArray();
        var actual = expected.GetHexString().GetByteArrayFromHexString();

        Assert.Equal(expected, actual);
    }

    public static IEnumerable<object[]> Data => Enumerable.Range(0, 100).Select(i => new object[] { i });
}
