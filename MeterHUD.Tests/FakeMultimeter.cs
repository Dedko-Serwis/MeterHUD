
using MeterHUD.Core;

namespace MeterHUD.Tests;

public sealed class FakeMultimeter : IMultimeter
{
    private readonly Queue<object?> _responses = new();

    public string Name => "Fake Multimeter";

    public bool IsConnected => !IsDisposed;

    public bool IsDisposed { get; private set; }

    public int ReadCount { get; private set; }
    public bool ThrowOnDispose { get; set; }
    public void AddReading(MultimeterReading reading)
    {
        _responses.Enqueue(reading);
    }

    public void AddNoData()
    {
        _responses.Enqueue(null);
    }

    public void AddTimeout()
    {
        _responses.Enqueue(new TimeoutException(
            "Symulowany timeout HID."));
    }

    public void AddError(Exception exception)
    {
        _responses.Enqueue(exception);
    }

    public Task<MultimeterReading?> ReadAsync(
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        ReadCount++;

        if (_responses.Count == 0)
            return Task.FromResult<MultimeterReading?>(null);

        object? response = _responses.Dequeue();

        if (response is Exception exception)
            return Task.FromException<MultimeterReading?>(
                exception);

        return Task.FromResult(
            (MultimeterReading?)response);
    }

    public void Dispose()
    {
        IsDisposed = true;

        if (ThrowOnDispose)
            throw new IOException("Symulowany błąd zamykania HID.");
    }
}
