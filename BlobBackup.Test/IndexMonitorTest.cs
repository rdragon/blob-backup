using Xunit;

namespace BlobBackup.Test;

public class IndexMonitorTest
{
    [Theory]
    [ClassData(typeof(Seeds))]
    public async Task Changed(int seed)
    {
        await TestHelper.RunWithInstanceFactory<IndexMonitor>(seed.GetRandom(), async createIndexMonitor =>
        {
            await Task.CompletedTask;
            var indexMonitor = createIndexMonitor();
            indexMonitor.ShardsChanged = true;
            indexMonitor = createIndexMonitor();

            Assert.True(indexMonitor.ShardsChanged);
        });
    }

    [Theory]
    [ClassData(typeof(Seeds))]
    public async Task NotChanged(int seed)
    {
        await TestHelper.RunWithInstanceFactory<IndexMonitor>(seed.GetRandom(), async createIndexMonitor =>
        {
            await Task.CompletedTask;
            var indexMonitor = createIndexMonitor();
            indexMonitor.ShardsChanged = true;
            indexMonitor.ShardsChanged = false;
            indexMonitor = createIndexMonitor();

            Assert.False(indexMonitor.ShardsChanged);
        });
    }
}
