
using MeterHUD.Core;

namespace MeterHUD.Services;

public sealed class MultimeterSimulator
{
    private int _index;


    public MultimeterReading Next()
    {
        int step = _index++ % 13;

        return step switch
        {
            0 => Create("12.345", "V DC", "Napięcie DC", 0x02),
            1 => Create("-0.125", "V DC", "Napięcie DC", 0x02),
            2 => Create("230.15", "V AC", "Napięcie AC", 0x00),
            3 => Create("1.234", "kΩ", "Rezystancja", 0x06),
            4 => Create("OL", "MΩ", "Rezystancja", 0x06,
                        overload: true),
            5 => Create("47.52", "µF", "Pojemność", 0x09),
            6 => Create("50.00", "Hz", "Częstotliwość", 0x04),
            7 => Create("12.45", "mA DC", "Prąd DC", 0x0E),
            8 => Create("3.300", "V DC", "Napięcie DC", 0x02,
                        hold: true),
            9 => Create("2.345", "V AC+DC", "Napięcie AC+DC", 0x19,
                        acDc: true, ac: true),
            10 => Create("-1.234", "V AC+DC", "Napięcie AC+DC", 0x19,
                         acDc: true, ac: false),
            11 => Create("0.612", "V", "Test diody", 0x08),
            _ => Create("0.000", "V DC", "Napięcie DC", 0x02,
                        auto: false)
        };
    }


    public void Reset() => _index = 0;


    private static MultimeterReading Create(
        string value,
        string unit,
        string function,
        byte functionCode,
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
            FunctionCode = functionCode,
            IsAutoRange = auto,
            IsHold = hold,
            IsOverload = overload,
            IsAcDc = acDc,
            IsAcComponent = ac,
            Timestamp = DateTime.Now
        };
    }
}
