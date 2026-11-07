
using MultimeterOverlay.Core;

namespace UT161EReader;

/// <summary>
/// Adapter multimetru UNI-T UT161E do wspólnego interfejsu.
/// </summary>
public sealed class UT161EMultimeter : IMultimeter
{
    private readonly UT161EHidReader _reader;
    private readonly SemaphoreSlim _readLock = new(1, 1);

    private bool _disposed;

    public string Name => "UNI-T UT161E";
    public UT161EHidLogger Logger => _reader.Logger;

    public bool IsConnected => !_disposed;

    public UT161EMultimeter()
    {
        _reader = new UT161EHidReader();
    }

    public async Task<MultimeterReading?> ReadAsync(
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(
            _disposed, this);

        await _readLock.WaitAsync(cancellationToken);

        try
        {
            ObjectDisposedException.ThrowIf(
                _disposed, this);

            // HidSharp używa synchronicznego odczytu.
            // Uruchamiamy go poza wątkiem UI.
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var measurement = _reader.ReadMeasurement();

                if (measurement is null)
                    return null;

                return new MultimeterReading
                {
                    Value = UT161EUnits.GetBaseValue(measurement),

                    DisplayValue = measurement.Overload
                        ? "OL"
                        : measurement.Negative
                            ? "-" + measurement.Value.TrimStart('-')
                            : measurement.Value,

                    Unit = UT161EUnits.GetUnit(measurement),

                    Function = measurement.Function,

                    FunctionCode = measurement.FunctionCode,

                    IsOverload = measurement.Overload,

                    IsHold = measurement.Hold,

                    IsMin = measurement.IsMin,

                    IsMax = measurement.IsMax,

                    IsAutoRange = !measurement.ManualRange,

                    Timestamp = DateTime.Now,

                    IsAcDc = measurement.FunctionCode == 0x19,

                    IsAcComponent = measurement.FunctionCode == 0x19  ? measurement.IsAcDcAcComponent : null
                };
            }, cancellationToken);
        }
        finally
        {
            _readLock.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _reader.Dispose();

        GC.SuppressFinalize(this);
    }
}
