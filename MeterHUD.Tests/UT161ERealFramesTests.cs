
using UT161EReader;
using Xunit;

namespace MeterHUD.Tests;

public class UT161ERealFramesTests
{
    // Rzeczywiste ramki z nagrania UT161E
    // z 5 listopada 2026 r.

    private const string DcFrame =
        "ABCD10023020302E3030303100003030300389";

    private const string AcDcDcFrame =
        "ABCD1019302D302E30303034000130303103B2";

    private const string AcDcAcFrame =
        "ABCD10193020302E30383233000130303903B6";

    [Fact]
    public void RealFrame_Dc_IsDecodedCorrectly()
    {
        byte[] data = Convert.FromHexString(DcFrame);

        bool result = UT161EDecoder.TryDecode(
            data, out var measurement);

        Assert.True(result);
        Assert.NotNull(measurement);

        Assert.Equal((byte)0x02, measurement.FunctionCode);
        Assert.Equal("0.0001", measurement.Value);
        Assert.Equal("V DC", UT161EUnits.GetUnit(measurement));
        Assert.Equal(0.0001,
            UT161EUnits.GetBaseValue(measurement));
    }

    [Fact]
    public void RealFrame_AcDc_DcComponent()
    {
        byte[] data = Convert.FromHexString(AcDcDcFrame);

        Assert.True(UT161EDecoder.TryDecode(
            data, out var measurement));

        Assert.NotNull(measurement);

        Assert.Equal((byte)0x19, measurement.FunctionCode);
        Assert.False(measurement.IsAcDcAcComponent);
        Assert.True(measurement.Negative);
        Assert.Equal("-0.0004", measurement.Value);
    }

    [Fact]
    public void RealFrame_AcDc_AcComponent()
    {
        byte[] data = Convert.FromHexString(AcDcAcFrame);

        Assert.True(UT161EDecoder.TryDecode(
            data, out var measurement));

        Assert.NotNull(measurement);

        Assert.Equal((byte)0x19, measurement.FunctionCode);
        Assert.True(measurement.IsAcDcAcComponent);        
        Assert.False(measurement.Negative);
        Assert.Equal("0.0823", measurement.Value);
    }

    [Fact]
    public void RealFrame_EmbeddedInHidReport()
    {
        // Raport HID ma 65 bajtów.
        // Ramka pomiarowa rozpoczyna się od offsetu 2.
        byte[] report = new byte[65];

        byte[] frame = Convert.FromHexString(AcDcAcFrame);

        report[0] = 0x00;
        report[1] = 0x13;

        Array.Copy(
            frame, 0,
            report, 2,
            frame.Length);

        Assert.True(UT161EDecoder.TryDecode(
            report, out var measurement));

        Assert.NotNull(measurement);
        Assert.True(measurement.IsAcDcAcComponent);
        Assert.Equal("0.0823", measurement.Value);
    }

    [Fact]
    public void RealFrame_CorruptedChecksum_IsRejected()
    {
        byte[] frame = Convert.FromHexString(AcDcAcFrame);

        // Zmiana wartości bez aktualizacji checksum.
        frame[8] ^= 0x01;

        Assert.False(UT161EDecoder.TryDecode(
            frame, out var measurement));

        Assert.Null(measurement);
    }

    [Fact]
    public void RealFrame_Truncated_IsRejected()
    {
        byte[] frame = Convert.FromHexString(AcDcAcFrame);

        byte[] truncated = frame[..18];

        Assert.False(UT161EDecoder.TryDecode(
            truncated, out var measurement));

        Assert.Null(measurement);
    }


    [Theory]
    [InlineData(
        "ABCD1019312020342E30323100003030300397",
        "4.021", false)]
    [InlineData(
        "ABCD1019312D20342E303235000030303003A8",
        "-4.025", true)]
    public void RealFrame_AcDc_DcPolarity(
        string hex, string expectedValue, bool expectedNegative)
    {
        byte[] frame = Convert.FromHexString(hex);

        Assert.True(UT161EDecoder.TryDecode(
            frame, out var measurement));

        Assert.NotNull(measurement);
        Assert.Equal((byte)0x19, measurement.FunctionCode);
        Assert.False(measurement.IsAcDcAcComponent);
        Assert.Equal(expectedValue, measurement.Value);
        Assert.Equal(expectedNegative, measurement.Negative);
    }
}
