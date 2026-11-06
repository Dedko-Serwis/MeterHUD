
using UT161EReader;
using Xunit;

namespace MultimeterOverlay.Tests;

public class UT161EUnitsTests
{
    private static UT161EMeasurement CreateMeasurement(
        byte function,
        byte range,
        string value = "1.2345",
        bool negative = false,
        bool overload = false)
    {
        return new UT161EMeasurement(
            Function: "",
            Value: value,
            FunctionCode: function,
            RangeCode: range,
            UnitCode: 0x30,
            ScaleCode: 0x30,
            Hold: false,
            IsMin: false,
            IsMax: false,
            ManualRange: false,
            Negative: negative,
            Overload: overload,
            Hex: "",
            IsAcDcAcComponent: false
        );
    }

    [Theory]
    [InlineData(0x00, 0x30, "V AC", 1.2345)]
    [InlineData(0x02, 0x30, "V DC", 1.2345)]
    [InlineData(0x01, 0x30, "mV AC", 0.0012345)]
    [InlineData(0x03, 0x30, "mV DC", 0.0012345)]
    [InlineData(0x0C, 0x30, "µA DC", 0.0000012345)]
    [InlineData(0x0D, 0x30, "µA AC", 0.0000012345)]
    [InlineData(0x0E, 0x30, "mA DC", 0.0012345)]
    [InlineData(0x0F, 0x30, "mA AC", 0.0012345)]
    [InlineData(0x10, 0x31, "A DC", 1.2345)]
    [InlineData(0x11, 0x31, "A AC", 1.2345)]
    [InlineData(0x06, 0x30, "Ω", 1.2345)]
    [InlineData(0x06, 0x31, "kΩ", 1234.5)]
    [InlineData(0x06, 0x34, "MΩ", 1234500.0)]
    [InlineData(0x09, 0x30, "nF", 0.0000000012345)]
    [InlineData(0x09, 0x32, "µF", 0.0000012345)]
    [InlineData(0x09, 0x35, "mF", 0.0012345)]
    public void GetBaseValue_ConvertsToSI(
        byte function,
        byte range,
        string expectedUnit,
        double expectedValue)
    {
        var measurement = CreateMeasurement(function, range);

        Assert.Equal(expectedUnit, UT161EUnits.GetUnit(measurement));

        double? actual = UT161EUnits.GetBaseValue(measurement);

        Assert.NotNull(actual);

        Assert.Equal(
            expectedValue,
            actual.Value,
            precision: 10);
    }

    [Fact]
    public void GetBaseValue_NegativeValue_ReturnsNegative()
    {
        var measurement = CreateMeasurement(
            0x02, 0x30,
            value: "12.345",
            negative: true);

        Assert.Equal(-12.345,
            UT161EUnits.GetBaseValue(measurement));
    }

    [Fact]
    public void GetBaseValue_Overload_ReturnsNull()
    {
        var measurement = CreateMeasurement(
            0x02, 0x30,
            value: "OL",
            overload: true);

        Assert.Null(UT161EUnits.GetBaseValue(measurement));
    }

    [Fact]
    public void GetBaseValue_InvalidNumber_ReturnsNull()
    {
        var measurement = CreateMeasurement(
            0x02, 0x30,
            value: "ABC");

        Assert.Null(UT161EUnits.GetBaseValue(measurement));
    }

    [Fact]
    public void GetUnit_UnknownFunction_ReturnsQuestionMark()
    {
        var measurement = CreateMeasurement(
            0xFF, 0x30);

        Assert.Equal("?", UT161EUnits.GetUnit(measurement));
        Assert.Null(UT161EUnits.GetBaseValue(measurement));
    }

    [Fact]
    public void GetResolution_KnownRange_ReturnsResolution()
    {
        var measurement = CreateMeasurement(
            0x02, 0x30);

        Assert.Equal(
            0.0001,
            UT161EUnits.GetResolution(measurement));
    }

    [Fact]
    public void GetResolution_UnknownRange_ReturnsNull()
    {
        var measurement = CreateMeasurement(
            0xFF, 0xFF);

        Assert.Null(UT161EUnits.GetResolution(measurement));
    }
}
