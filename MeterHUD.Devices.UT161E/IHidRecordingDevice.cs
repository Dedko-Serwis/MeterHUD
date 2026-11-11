
using MeterHUD.Core;

namespace UT161EReader;

public interface IHidRecordingDevice : IRecordingDevice
{
    UT161EHidLogger Logger { get; }
}

