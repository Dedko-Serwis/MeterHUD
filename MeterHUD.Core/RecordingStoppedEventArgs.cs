
namespace MeterHUD.Core;

public sealed class RecordingStoppedEventArgs(
    bool wasSavedSuccessfully,
    int frameCount,
    string? error) : EventArgs
{
    public bool WasSavedSuccessfully { get; } = wasSavedSuccessfully;

    public int FrameCount { get; } = frameCount;

    public string? Error { get; } = error;
}
