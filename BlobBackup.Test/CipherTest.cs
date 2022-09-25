using Xunit;

namespace BlobBackup.Test;

public class CipherTest
{
    [Theory]
    [ClassData(typeof(ZeroTo99))]
    public async Task EncryptAndDecrypt(int dataLength)
    {
        await TestHelper.RunWithInstance<Cipher>(dataLength.GetRandom(), async cipher =>
        {
            await Task.CompletedTask;
            var expected = dataLength.GetByteArray();
            var actual = cipher.TryDecrypt(cipher.Encrypt(expected));

            Assert.Equal(expected, actual);
        });
    }
}
