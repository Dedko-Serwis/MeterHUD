
namespace MultimeterOverlay.Core;

public sealed class ConnectionRetryPolicy
{
    private readonly int _maxFailures;

    public int ConsecutiveFailures { get; private set; }

    public bool ShouldReconnect =>
        ConsecutiveFailures >= _maxFailures;

    public ConnectionRetryPolicy(int maxFailures)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxFailures);
        _maxFailures = maxFailures;
    }

    public void ReportSuccess()
    {
        ConsecutiveFailures = 0;
    }

    public void ReportFailure()
    {
        if (ConsecutiveFailures < _maxFailures)
            ConsecutiveFailures++;
    }

    public void Reset()
    {
        ConsecutiveFailures = 0;
    }
}
