
namespace MeterHUD.Core;

public interface IMultimeterConnectionService : IDisposable
{
    bool IsConnected { get; }

    IMultimeter? CurrentDevice { get; }

    event EventHandler<MultimeterReading>? ReadingReceived;

    event EventHandler<bool>? ConnectionChanged;
    event EventHandler? ReadingMissing;
    event EventHandler<RecordingStoppedEventArgs>? RecordingStopped;

    Task StartAsync(CancellationToken cancellationToken);

    Task StopAsync();
}
