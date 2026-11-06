
using System.Globalization;
using System.Text;

namespace UT161EReader;

public sealed record UT161EMeasurement(
    string Function,
    string Value,
    byte FunctionCode,
    byte RangeCode,
    byte UnitCode,
    byte ScaleCode,
bool Hold,
bool IsMin,
bool IsMax,
bool ManualRange,
    bool Negative,
    bool Overload,
    string Hex,
    bool IsAcDcAcComponent);

public static class UT161EDecoder
{
    public static bool TryDecode(
        ReadOnlySpan<byte> report,
        out UT161EMeasurement? measurement)
    {
        measurement = null;

        for (int i = 0; i <= report.Length - 19; i++)
        {
            if (report[i] != 0xAB ||
                report[i + 1] != 0xCD ||
                report[i + 2] != 0x10)
                continue;

            var frame = report.Slice(i, 19);

            // Bajt 17 = 03, bajt 18 = suma kontrolna.
            if (frame[17] != 0x03)
                continue;

            int checksum = 0;

            for (int j = 0; j < 17; j++)
                checksum += frame[j];

            if ((byte)checksum != frame[18])
                continue;

            byte function = frame[3];
            byte range = frame[4];

            string rawValue =
                Encoding.ASCII.GetString(frame.Slice(5, 7));

            string value = rawValue.Trim().Replace(" ", "");

            bool overload =
                value.Contains("OL",
                    StringComparison.OrdinalIgnoreCase) ||
                value.Contains("O.L",
                    StringComparison.OrdinalIgnoreCase);

            if (!overload &&
                !double.TryParse(
                    value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out _))
            {
                continue;
            }

            string functionName = function switch
            {
                0x00 => "Napięcie AC",
                0x01 => "Napięcie AC",
                0x02 => "Napięcie DC",
                0x03 => "Napięcie DC",
                0x06 => "Rezystancja",
                0x07 => "Ciągłość",
                0x08 => "Dioda",
                0x09 => "Pojemność",
                0x04 => "Częstotliwość",
                0x05 => "Wypełnienie %",
                0x0C => "Prąd stały µA",
                0x0D => "Prąd przemienny µA",
                0x0E => "Prąd stały mA",
                0x0F => "Prąd przemienny mA",
                0x10 => "Prąd stały A",
                0x11 => "Prąd przemienny A",
                0x12 => "Wzmocnienie hFE",
                0x18 => "Napięcie AC (LPF)",
                0x19 => "Napięcie AC+DC",
                _ => $"Nieznana (0x{function:X2})"
            };

            // Flagi ustalone na podstawie testów CSV.
            // Ich interpretację można rozszerzyć
            // po sprawdzeniu kolejnych kombinacji.
            bool hold = (frame[14] & 0x02) != 0;
            bool isMin = (frame[14] & 0x04) != 0;
            bool isMax = (frame[14] & 0x08) != 0;

            bool manual = (frame[15] & 0x04) != 0;
            bool negative = value.StartsWith('-');

            // AC+DC: bit 3 bajtu 16 rozróżnia dwie składowe.
            // Przypisanie bitu do AC wymaga jeszcze potwierdzenia.
            bool isAcDcAcComponent =
                function == 0x19 && (frame[16] & 0x08) != 0;

            measurement = new UT161EMeasurement(
                Function: functionName,
                Value: value,
                FunctionCode: function,
                RangeCode: range,
                UnitCode: frame[12],
                ScaleCode: frame[13],
                Hold: hold,
                IsMin: isMin,
                IsMax: isMax,
                ManualRange: manual,
                Negative: negative,
                Overload: overload,                
                Hex: Convert.ToHexString(frame),
                IsAcDcAcComponent: isAcDcAcComponent
            );

            return true;
        }

        return false;
    }
}
