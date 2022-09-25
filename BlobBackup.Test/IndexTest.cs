using Xunit;

namespace BlobBackup.Test;

public class IndexTest
{
    [Theory]
    [ClassData(typeof(Seeds))]
    public async Task SaveFileToken(int seed)
    {
        var random = new Random(seed);

        await TestHelper.RunWithInstanceFactory<Index>(random, async createIndex =>
        {
            var index = createIndex();
            var relativePath = random.GetFileId();
            var fileToken = random.GetFileToken();
            index.AddOrReplaceFileToken(relativePath, fileToken);
            await index.SaveIndex();
            index = createIndex();
            await index.LoadIndex();

            Assert.Equal(new[] { relativePath }, index.FileTokens.Keys.ToArray());
            Assert.Equal(new[] { fileToken }, index.FileTokens.Values.ToArray());
        });
    }
}
