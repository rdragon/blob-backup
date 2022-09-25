using Xunit;

namespace BlobBackup.Test;

public class BlobProviderTest
{
    [Theory]
    [ClassData(typeof(Seeds))]
    public async Task UploadAndDownloadIndex(int seed)
    {
        var random = seed.GetRandom();

        await TestHelper.RunWithInstance<BlobProvider>(random, async blobProvider =>
        {
            var expected = random.GetIndexData();
            await blobProvider.UploadIndex(expected);
            var actual = await blobProvider.DownloadIndex();

            Assert.Equal(expected, actual);
        });
    }

    [Theory]
    [ClassData(typeof(Seeds))]
    public async Task UploadShard(int seed)
    {
        await RunWithShard(seed.GetRandom(), async (blobProvider, shardId, _) =>
        {
            Assert.True(await blobProvider.ShardExists(shardId));
        });
    }

    [Theory]
    [ClassData(typeof(Seeds))]
    public async Task DeleteShard(int seed)
    {
        await RunWithShard(seed.GetRandom(), async (blobProvider, shardId, _) =>
        {
            await blobProvider.DeleteShard(shardId);

            Assert.False(await blobProvider.ShardExists(shardId));
        });
    }

    [Theory]
    [ClassData(typeof(Seeds))]
    public async Task UploadAndDownloadShardToken(int seed)
    {
        var random = seed.GetRandom();

        await TestHelper.RunWithInstance<BlobProvider>(random, async blobProvider =>
        {
            var shardId = random.GetShardId();
            var expectedShardToken = random.GetShardToken();
            await blobProvider.UploadShardToken(shardId, expectedShardToken);
            var actualShardToken = await blobProvider.DownloadShardToken(shardId);

            Assert.Equal(expectedShardToken, actualShardToken);
        });
    }

    private static async Task RunWithShard(Random random, RunWithShardFunc func)
    {
        await TestHelper.RunWithInstance<BlobProvider>(random, async blobProvider =>
        {
            var bytes = random.GetBytes();
            var shardId = random.GetShardId();
            await blobProvider.UploadShard(shardId, bytes);

            await func(blobProvider, shardId, bytes);
        });
    }

    delegate Task RunWithShardFunc(BlobProvider blobProvider, ShardId shardId, byte[] bytes);
}
