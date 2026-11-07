
using MultimeterOverlay.Core;

namespace UT161EReader;

public sealed class UT161EMultimeterFactory : IMultimeterFactory
{
    public IMultimeter Create()
    {
        return new UT161EMultimeter();
    }
}

