
using MultimeterOverlay.Core;

namespace MultimeterOverlay.Services;

public static class MeasurementLocalizationService
{
    public static string GetFunctionName(MultimeterReading reading)
    {
        string? key = reading.FunctionCode switch
        {
            0x00 or 0x01 => "MeasurementVoltageAC",
            0x02 or 0x03 => "MeasurementVoltageDC",

            0x04 => "MeasurementFrequency",
            0x05 => "MeasurementDutyCycle",

            0x06 => "MeasurementResistance",
            0x07 => "MeasurementContinuity",
            0x08 => "MeasurementDiode",
            0x09 => "MeasurementCapacitance",

            0x0C or 0x0E or 0x10 => "MeasurementCurrentDC",
            0x0D or 0x0F or 0x11 => "MeasurementCurrentAC",

            0x12 => "MeasurementTransistor",

            0x18 => "MeasurementVoltageACLPF",
            0x19 => "MeasurementVoltageACDC",

            _ => null
        };

        if (key is null)
        {
            return reading.Function;
        }

        return LocalizationService.Instance[key];
    }
}
