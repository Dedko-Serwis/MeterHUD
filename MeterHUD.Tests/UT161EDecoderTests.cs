
using System.Text;
using UT161EReader;
using Xunit;

namespace MeterHUD.Tests;

public class UT161EDecoderTests
{
    // Tworzy poprawną ramkę protokołu UT161E.
    // Używamy jej do testów jednostkowych,
    // niezależnie od urządzenia USB.
    private static byte[] CreateFrame(
        byte function = 0x02,
        byte range = 0x30,
        string value = "0.1234",
        byte flags14 = 0x30,
        byte flags15 = 0x30,
        byte flags16 = 0x30)
    {
        var frame = new byte[19];

        frame[0] = 0xAB;
        frame[1] = 0xCD;
        frame[2] = 0x10;
        frame[3] = function;
        frame[4] = range;

        byte[] valueBytes = Encoding.ASCII.GetBytes(
            value.PadLeft(7));

        Array.Copy(valueBytes, 0, frame, 5, 7);

        frame[12] = 0x30;
        frame[13] = 0x30;
        frame[14] = flags14;
        frame[15] = flags15;
        frame[16] = flags16;
        frame[17] = 0x03;

        int checksum = 0;

        for (int i = 0; i < 17; i++)
            checksum += frame[i];

        frame[18] = (byte)checksum;

        return frame;
    }

    [Fact]
    public void Decode_ValidDcFrame_ReturnsCorrectValue()
    {
        byte[] frame = CreateFrame(
            function: 0x02,
            value: "1.2345");

        bool success = UT161EDecoder.TryDecode(
            frame, out var measurement);

        Assert.True(success);
        Assert.NotNull(measurement);
        Assert.Equal("1.2345", measurement.Value);
        Assert.Equal("Napięcie DC", measurement.Function);
        Assert.Equal((byte)0x02, measurement.FunctionCode);
    }

    [Fact]
    public void Decode_AcFrame_ReturnsCorrectFunction()
    {
        byte[] frame = CreateFrame(
            function: 0x00,
            value: "12.345");

        Assert.True(UT161EDecoder.TryDecode(
            frame, out var measurement));

        Assert.NotNull(measurement);
        Assert.Equal("Napięcie AC", measurement.Function);
    }

    [Theory]
    [InlineData(0x31, false)] // DC
    [InlineData(0x39, true)]  // AC
    public void Decode_AcDc_DetectsComponent(
        byte flags16,
        bool expectedAc)
    {
        byte[] frame = CreateFrame(
            function: 0x19,
            flags16: flags16);

        Assert.True(UT161EDecoder.TryDecode(
            frame, out var measurement));

        Assert.NotNull(measurement);
        Assert.Equal((byte)0x19, measurement.FunctionCode);
        Assert.Equal(expectedAc,
            measurement.IsAcDcAcComponent);
    }

    [Fact]
    public void Decode_MinFlag_IsDetected()
    {
        byte[] frame = CreateFrame(flags14: 0x34);

        Assert.True(UT161EDecoder.TryDecode(
            frame, out var measurement));

        Assert.NotNull(measurement);
        Assert.True(measurement.IsMin);
        Assert.False(measurement.IsMax);
    }

    [Fact]
    public void Decode_MaxFlag_IsDetected()
    {
        byte[] frame = CreateFrame(flags14: 0x38);

        Assert.True(UT161EDecoder.TryDecode(
            frame, out var measurement));

        Assert.NotNull(measurement);
        Assert.True(measurement.IsMax);
        Assert.False(measurement.IsMin);
    }

    [Fact]
    public void Decode_HoldFlag_IsDetected()
    {
        byte[] frame = CreateFrame(flags14: 0x32);

        Assert.True(UT161EDecoder.TryDecode(
            frame, out var measurement));

        Assert.NotNull(measurement);
        Assert.True(measurement.Hold);
    }

    [Fact]
    public void Decode_InvalidChecksum_IsRejected()
    {
        byte[] frame = CreateFrame();

        frame[18] ^= 0xFF;

        bool success = UT161EDecoder.TryDecode(
            frame, out var measurement);

        Assert.False(success);
        Assert.Null(measurement);
    }

    [Fact]
    public void Decode_InvalidNumericValue_IsRejected()
    {
        byte[] frame = CreateFrame(value: "ABCDEF");

        bool success = UT161EDecoder.TryDecode(
            frame, out var measurement);

        Assert.False(success);
        Assert.Null(measurement);
    }
}
