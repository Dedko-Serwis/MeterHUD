
namespace UT161EReader;

/// <summary>
/// Definicja zakresu pomiarowego multimetru.
/// </summary>
public sealed record UT161ERange(
    byte FunctionCode,
    byte RangeCode,
    string Unit,
    double Multiplier,
    int? Decimals = null,
    bool Confirmed = true)
{
    public double? Resolution =>
        Decimals is int decimals
            ? Math.Pow(10, -decimals)
            : null;
}
