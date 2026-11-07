
namespace MultimeterOverlay.Core;

public interface IRecordingDevice
{
    void StopRecording();

    RecordingStoppedEventArgs GetRecordingResult();
}

