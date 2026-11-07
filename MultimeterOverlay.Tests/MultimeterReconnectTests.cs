
using MultimeterOverlay.Core;
using Xunit;

namespace MultimeterOverlay.Tests;

public sealed class MultimeterReconnectTests
{
    [Fact]
    public async Task ThreeTimeouts_ShouldReconnectAndDisposeOldDevice()
    {
        var firstDevice = new FakeMultimeter();
        firstDevice.AddTimeout();
        firstDevice.AddTimeout();
        firstDevice.AddTimeout();

        var secondDevice = new FakeMultimeter();

        var factory = new FakeMultimeterFactory();
        factory.AddDevice(firstDevice);
        factory.AddDevice(secondDevice);

        var policy = new ConnectionRetryPolicy(3);

        IMultimeter device = factory.Create();

        for (int i = 0; i < 3; i++)
        {
            try
            {
                await device.ReadAsync(CancellationToken.None);
                policy.ReportSuccess();
            }
            catch (TimeoutException)
            {
                policy.ReportFailure();
            }
        }

        Assert.True(policy.ShouldReconnect);

        device.Dispose();

        Assert.True(firstDevice.IsDisposed);
        Assert.Equal(3, firstDevice.ReadCount);

        policy.Reset();
        device = factory.Create();

        Assert.Same(secondDevice, device);
        Assert.Equal(2, factory.CreateCount);
        Assert.False(policy.ShouldReconnect);

        device.Dispose();
    }
}
