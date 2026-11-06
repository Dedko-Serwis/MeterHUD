
using MultimeterOverlay.Core;

namespace MultimeterOverlay.Services;

public sealed class MultimeterSimulator
{
    private int _index;

    public MultimeterReading Next()
    {
        int step = _index++ % 12;

        return step switch
        {
            0 => Create("12.345", "V DC", "Napięcie DC"),
            1 => Create("-0.125", "V DC", "Napięcie DC"),
            2 => Create("230.15", "V AC", "Napięcie AC"),
            3 => Create("1.234", "kΩ", "Rezystancja"),
            4 => Create("OL", "MΩ", "Rezystancja",
                        overload: true),
            5 => Create("47.52", "µF", "Pojemność"),
            6 => Create("50.00", "Hz", "Częstotliwość"),
            7 => Create("12.45", "mA DC", "Prąd DC"),
            8 => Create("3.300", "V DC", "Napięcie DC",
                        hold: true),
            9 => Create("2.345", "V AC+DC", "Napięcie AC+DC",
                        acDc: true, ac: true),
            10 => Create("-1.234", "V AC+DC", "Napięcie AC+DC",
                         acDc: true, ac: false),
            _ => Create("0.000", "V DC", "Napięcie DC",
                        auto: false)
        };
    }

    public void Reset() => _index = 0;

    private static MultimeterReading Create(
        string value,
        string unit,
        string function,
        bool auto = true,
        bool hold = false,
        bool overload = false,
        bool acDc = false,
        bool? ac = null)
    {
        return new MultimeterReading
        {
            DisplayValue = value,
            Unit = unit,
            Function = function,
            IsAutoRange = auto,
            IsHold = hold,
            IsOverload = overload,
            IsAcDc = acDc,
            IsAcComponent = ac,
            Timestamp = DateTime.Now
        };
    }
}
