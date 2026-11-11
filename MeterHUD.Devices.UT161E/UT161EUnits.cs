
using System.Globalization;

namespace UT161EReader;

public static class UT161EUnits
{
    // Centralna tabela znanych zakresów.
    private static readonly Dictionary<(byte Function, byte Range), UT161ERange>
        Ranges = CreateRanges();

    private static Dictionary<(byte, byte), UT161ERange> CreateRanges()
    {
        var ranges = new Dictionary<(byte, byte), UT161ERange>();

        void Add(
            byte function,
            byte range,
            string unit,
            double multiplier,
            int? decimals = null,
            bool confirmed = true)
        {
            ranges.Add(
                (function, range),
                new UT161ERange(
                    function,
                    range,
                    unit,
                    multiplier,
                    decimals,
                    confirmed));
        }

        // NAPIĘCIE AC
        Add(0x00, 0x30, "V AC", 1, 4);
        Add(0x00, 0x31, "V AC", 1, 3);
        Add(0x00, 0x32, "V AC", 1, 2);
        Add(0x00, 0x33, "V AC", 1, 1);

        // NAPIĘCIE DC
        Add(0x02, 0x30, "V DC", 1, 4);
        Add(0x02, 0x31, "V DC", 1, 3);
        Add(0x02, 0x32, "V DC", 1, 2);
        Add(0x02, 0x33, "V DC", 1, 1);

        // mV AC
        Add(0x01, 0x30, "mV AC", 1e-3, 2);

        // mV DC
        Add(0x03, 0x30, "mV DC", 1e-3);


        // CZĘSTOTLIWOŚĆ
        Add(0x04, 0x30, "Hz", 1, 2);
        Add(0x04, 0x31, "Hz", 1, 1);
        Add(0x04, 0x32, "kHz", 1e3, 4);
        Add(0x04, 0x33, "kHz", 1e3, 3);
        Add(0x04, 0x34, "kHz", 1e3, 2);
        Add(0x04, 0x35, "MHz", 1e6, 4);
        Add(0x04, 0x36, "MHz", 1e6, 3);

        // WYPEŁNIENIE IMPULSU
        Add(0x05, 0x30, "%", 1, 1);

        // REZYSTANCJA
        Add(0x06, 0x30, "Ω", 1);
        Add(0x06, 0x31, "kΩ", 1e3);
        Add(0x06, 0x32, "kΩ", 1e3);
        Add(0x06, 0x33, "kΩ", 1e3);
        Add(0x06, 0x34, "MΩ", 1e6);
        Add(0x06, 0x35, "MΩ", 1e6);
        Add(0x06, 0x36, "MΩ", 1e6);

        // CIĄGŁOŚĆ OBWODU
        Add(0x07, 0x30, "Ω", 1, 2);

        // TEST DIODY
        Add(0x08, 0x30, "V", 1, 4);

        // POJEMNOŚĆ
        Add(0x09, 0x30, "nF", 1e-9, 4);
        Add(0x09, 0x31, "nF", 1e-9, 3, false);
        Add(0x09, 0x32, "µF", 1e-6, 4);
        Add(0x09, 0x33, "µF", 1e-6, 3);
        Add(0x09, 0x34, "µF", 1e-6, 2);
        Add(0x09, 0x35, "mF", 1e-3, 4);
        Add(0x09, 0x36, "mF", 1e-3, 2);

        // PRĄD µA DC
        Add(0x0C, 0x30, "µA DC", 1e-6, 2);
        Add(0x0C, 0x31, "µA DC", 1e-6, 1);

        // PRĄD µA AC
        Add(0x0D, 0x30, "µA AC", 1e-6, 2);
        Add(0x0D, 0x31, "µA AC", 1e-6, 1);

        // PRĄD mA DC
        Add(0x0E, 0x30, "mA DC", 1e-3, 3);
        Add(0x0E, 0x31, "mA DC", 1e-3, 2);

        // PRĄD mA AC
        Add(0x0F, 0x30, "mA AC", 1e-3, 3);

        // PRĄD A DC
        Add(0x10, 0x31, "A DC", 1, 3);

        // PRĄD A AC
        Add(0x11, 0x31, "A AC", 1, 3);

        // NAPIĘCIE AC Z FILTREM LPF
        Add(0x18, 0x30, "V AC", 1, 4);
        Add(0x18, 0x31, "V AC", 1, 3);
        Add(0x18, 0x32, "V AC", 1, 2);
        Add(0x18, 0x33, "V AC", 1, 1);






        // PRĄD
        // Zakresy prądowe nie zostały jeszcze
        // szczegółowo zidentyfikowane.
        // Zachowujemy dotychczasowe mapowanie
        // funkcji niezależnie od RangeCode.

        return ranges;
    }

    /// <summary>
    /// Pobiera definicję zakresu.
    /// </summary>
    public static UT161ERange? GetRange(UT161EMeasurement m)
    {
        return Ranges.GetValueOrDefault(
            (m.FunctionCode, m.RangeCode));
    }

    /// <summary>
    /// Jednostka pomiarowa.
    /// </summary>
    public static string GetUnit(UT161EMeasurement m)
    {
        var range = GetRange(m);

        if (range is not null)
            return range.Unit;

        // Dotychczasowe mapowanie prądu.

        return m.FunctionCode switch
        {
            0x00 => "V AC",
            0x01 => "mV AC",
            0x02 => "V DC",
            0x03 => "mV DC",

            0x04 => "Hz",
            0x05 => "%",

            0x06 or 0x07 => "Ω",
            0x08 => "V",

            0x0C => "µA DC",
            0x0D => "µA AC",
            0x0E => "mA DC",
            0x0F => "mA AC",
            0x10 => "A DC",
            0x11 => "A AC",

            //0x12 => "hFE",
            0x12 => "β",

            0x18 => "V AC",
            0x19 => "V AC+DC",

            _ => "?"
        };

    }

    /// <summary>
    /// Rozdzielczość w jednostce wyświetlanej.
    /// </summary>
    public static double? GetResolution(UT161EMeasurement m)
    {
        return GetRange(m)?.Resolution;
    }

    /// <summary>
    /// Liczba miejsc po przecinku dla pojemności.
    /// </summary>
    public static int? GetCapacitanceDecimals(byte rangeCode)
    {
        return Ranges.TryGetValue(
            (0x09, rangeCode), out var range)
            ? range.Decimals
            : null;
    }

    /// <summary>
    /// Wartość przeliczona na jednostkę podstawową SI.
    /// Napięcie -> V
    /// Prąd -> A
    /// Rezystancja -> Ω
    /// Pojemność -> F
    /// </summary>
    public static double? GetBaseValue(UT161EMeasurement m)
    {
        if (m.Overload)
            return null;

        if (!double.TryParse(
            m.Value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out double value))
        {
            return null;
        }

        if (m.Negative)
            value = -Math.Abs(value);

        var range = GetRange(m);

        if (range is not null)
            return value * range.Multiplier;

        // Obsługa prądu bez potwierdzonych RangeCode.
        double? multiplier = m.FunctionCode switch
        {
            0x0C or 0x0D => 1e-6,
            0x0E or 0x0F => 1e-3,
            0x10 or 0x11 => 1.0,
            _ => null
        };

        return multiplier is double factor
            ? value * factor
            : null;
    }
}
