
using MeterHUD.Core;
using System.Collections.Concurrent;
using Xunit;

namespace MeterHUD.Tests;

public sealed class RecordingDeviceTests
{
    private sealed class FakeRecordingDevice : IMultimeter, IRecordingDevice
    {
        public ConcurrentQueue<string> Operations { get; } = new();

        public string Name => "Fake Recording Device";
        public bool IsConnected => true;

        public Task<MultimeterReading?> ReadAsync(
            CancellationToken cancellationToken)
        {
            return Task.FromResult<MultimeterReading?>(null);
        }

        public void StopRecording()
        {
            Operations.Enqueue("StopRecording");
        }

        public void Dispose()
        {
            Operations.Enqueue("Dispose");
        }

        public RecordingStoppedEventArgs GetRecordingResult()
        {
            return new RecordingStoppedEventArgs(
                true,
                1,
                null);
        }
    }

    [Fact]
    public async Task Disconnect_ShouldStopRecordingBeforeDispose()
    {
        var device = new FakeRecordingDevice();

        var factory = new FakeMultimeterFactory();
        factory.AddDevice(device);

        using var service = new MultimeterConnectionService(
            factory,
            maxConsecutiveFailures: 3,
            readDelay: TimeSpan.FromMilliseconds(1),
            reconnectDelay: TimeSpan.FromMilliseconds(100));

        await service.StartAsync(CancellationToken.None);

        try
        {
            await WaitUntilAsync(
                () => device.Operations.Contains("Dispose"),
                TimeSpan.FromSeconds(3));
        }
        finally
        {
            await service.StopAsync();
        }

        Assert.Equal(
            ["StopRecording", "Dispose"],
            device.Operations);
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
}