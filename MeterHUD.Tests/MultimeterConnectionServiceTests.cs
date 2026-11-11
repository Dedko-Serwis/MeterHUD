
using MeterHUD.Core;
using Xunit;

namespace MeterHUD.Tests;

public sealed class MultimeterConnectionServiceTests
{
    [Fact]
    public async Task ThreeTimeouts_ShouldAutomaticallyReconnect()
    {
        var firstDevice = new FakeMultimeter();
        firstDevice.AddTimeout();
        firstDevice.AddTimeout();
        firstDevice.AddTimeout();

        var secondDevice = new FakeMultimeter();

        var factory = new FakeMultimeterFactory();
        factory.AddDevice(firstDevice);
        factory.AddDevice(secondDevice);

        using var service = new MultimeterConnectionService(
            factory,
            maxConsecutiveFailures: 3,
            readDelay: TimeSpan.FromMilliseconds(1),
            reconnectDelay: TimeSpan.FromMilliseconds(1));

        await service.StartAsync(CancellationToken.None);

        try
        {
            // Czekamy na utworzenie drugiego miernika.
            // Limit chroni test przed zawieszeniem.
            await WaitUntilAsync(
                () => factory.CreateCount >= 2,
                TimeSpan.FromSeconds(3));

            Assert.Equal(3, firstDevice.ReadCount);
            Assert.True(firstDevice.IsDisposed);
            Assert.Same(secondDevice, service.CurrentDevice);
        }
        finally
        {
            await service.StopAsync();
        }

        Assert.True(secondDevice.IsDisposed);
    }

    private static async Task WaitUntilAsync(
        Func<bool> condition,
        TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);

        while (!condition())
        {
            cts.Token.ThrowIfCancellationRequested();
            await Task.Delay(10, cts.Token);
        }
    }

    [Fact]
    public async Task IOException_ShouldReconnectImmediately()
    {
        var firstDevice = new FakeMultimeter();
        firstDevice.AddError(
            new IOException("Symulowane odłączenie USB."));

        var secondDevice = new FakeMultimeter();

        var factory = new FakeMultimeterFactory();
        factory.AddDevice(firstDevice);
        factory.AddDevice(secondDevice);

        using var service = new MultimeterConnectionService(
            factory,
            maxConsecutiveFailures: 3,
            readDelay: TimeSpan.FromMilliseconds(1),
            reconnectDelay: TimeSpan.FromMilliseconds(1));

        await service.StartAsync(CancellationToken.None);

        try
        {
            await WaitUntilAsync(
                () => factory.CreateCount >= 2,
                TimeSpan.FromSeconds(3));

            Assert.Equal(1, firstDevice.ReadCount);
            Assert.True(firstDevice.IsDisposed);
        }
        finally
        {
            await service.StopAsync();
        }

        Assert.True(secondDevice.IsDisposed);
    }


    [Fact]
    public async Task SuccessfulReading_ShouldResetTimeoutCounter()
    {
        var firstDevice = new FakeMultimeter();

        firstDevice.AddTimeout();
        firstDevice.AddTimeout();

        firstDevice.AddReading(new MultimeterReading
        {
            DisplayValue = "12.34",
            Unit = "V",
            FunctionCode = 0x02
        });

        firstDevice.AddTimeout();
        firstDevice.AddTimeout();

        var factory = new FakeMultimeterFactory();
        factory.AddDevice(firstDevice);

        using var cts = new CancellationTokenSource();

        using var service = new MultimeterConnectionService(
            factory,
            maxConsecutiveFailures: 3,
            readDelay: TimeSpan.FromMilliseconds(20),
            reconnectDelay: TimeSpan.FromMilliseconds(1));

        await service.StartAsync(cts.Token);

        try
        {
            await WaitUntilAsync(
                () => firstDevice.ReadCount >= 5,
                TimeSpan.FromSeconds(3));

            cts.Cancel();
            await service.StopAsync();

            Assert.Equal(5, firstDevice.ReadCount);
            Assert.Equal(1, factory.CreateCount);
            Assert.True(firstDevice.IsDisposed);
        }
        finally
        {
            await service.StopAsync();
        }
    }


    [Fact]
    public async Task StopAsync_ShouldCancelAndDisposeDevice()
    {
        var device = new FakeMultimeter();

        var factory = new FakeMultimeterFactory();
        factory.AddDevice(device);

        using var service = new MultimeterConnectionService(
            factory,
            maxConsecutiveFailures: 3,
            readDelay: TimeSpan.FromSeconds(10),
            reconnectDelay: TimeSpan.FromSeconds(10));

        await service.StartAsync(CancellationToken.None);

        try
        {
            await WaitUntilAsync(
                () => device.ReadCount >= 1,
                TimeSpan.FromSeconds(3));

            await service.StopAsync();

            Assert.True(device.IsDisposed);
            Assert.Null(service.CurrentDevice);
            Assert.False(service.IsConnected);
            Assert.Equal(1, factory.CreateCount);
        }
        finally
        {
            await service.StopAsync();
        }
    }

    [Fact]
    public async Task DisposeIOException_ShouldStillReconnect()
    {
        var firstDevice = new FakeMultimeter
        {
            ThrowOnDispose = true
        };

        firstDevice.AddTimeout();
        firstDevice.AddTimeout();
        firstDevice.AddTimeout();

        var secondDevice = new FakeMultimeter();

        var factory = new FakeMultimeterFactory();
        factory.AddDevice(firstDevice);
        factory.AddDevice(secondDevice);

        using var service = new MultimeterConnectionService(
            factory,
            maxConsecutiveFailures: 3,
            readDelay: TimeSpan.FromMilliseconds(1),
            reconnectDelay: TimeSpan.FromMilliseconds(1));

        await service.StartAsync(CancellationToken.None);

        try
        {
            await WaitUntilAsync(
                () => ReferenceEquals(service.CurrentDevice, secondDevice),
                TimeSpan.FromSeconds(3));

            Assert.True(firstDevice.IsDisposed);
            Assert.Equal(3, firstDevice.ReadCount);
            Assert.Equal(2, factory.CreateCount);
        }
        finally
        {
            await service.StopAsync();
        }

        Assert.True(secondDevice.IsDisposed);
    }


}
