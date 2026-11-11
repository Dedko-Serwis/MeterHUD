
namespace MeterHUD.Core;

public interface IRecordingDevice
{
    void StopRecording();

    RecordingStoppedEventArgs GetRecordingResult();
}

