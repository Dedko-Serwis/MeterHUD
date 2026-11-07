
using MultimeterOverlay.Core;

namespace UT161EReader;

public sealed class UT161EMultimeterFactory : IMultimeterFactory
{
    private readonly Func<string?>? _logDirectoryProvider;

    public UT161EMultimeterFactory(Func<string?>? logDirectoryProvider = null)
    {
        _logDirectoryProvider = logDirectoryProvider;
    }

    public IMultimeter Create()
    {
        return new UT161EMultimeter(_logDirectoryProvider);
    }
}

