
using MeterHUD.Core;

namespace MeterHUD.Tests;

public sealed class FakeMultimeterFactory : IMultimeterFactory
{
    private readonly Queue<IMultimeter> _devices = new();

    public int CreateCount { get; private set; }

    public void AddDevice(IMultimeter device)
    {
        ArgumentNullException.ThrowIfNull(device);
        _devices.Enqueue(device);
    }

    public IMultimeter Create()
    {
        CreateCount++;

        if (_devices.Count == 0)
        {
            throw new InvalidOperationException(
                "Brak kolejnego miernika testowego.");
        }

        return _devices.Dequeue();
    }
}
