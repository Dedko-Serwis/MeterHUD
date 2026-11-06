
using HidSharp;

namespace UT161EReader;

public sealed class UT161EHidReader : IDisposable
{
    private const int VendorId = 0x1A86;
    private const int ProductId = 0xE429;

    private static readonly byte[] Command =
    [
        0xAB, 0xCD, 0x03, 0x5E, 0x01, 0xD9
    ];

    private readonly HidStream _stream;
    private readonly byte[] _input;
    private readonly byte[] _output;
    private readonly UT161EHidLogger _logger = new();
    public UT161EHidLogger Logger => _logger;

    public UT161EHidReader()
    {
        var device = DeviceList.Local
            .GetHidDevices(VendorId, ProductId)
            .FirstOrDefault()
            ?? throw new InvalidOperationException(
                "Nie znaleziono miernika UT161E.");

        if (!device.TryOpen(out HidStream? stream) ||
            stream is null)
        {
            throw new IOException(
                "Nie można otworzyć urządzenia HID.");
        }

        _stream = stream;
        _stream.ReadTimeout = 3000;
        _stream.WriteTimeout = 3000;

        _input = new byte[
            device.GetMaxInputReportLength()];

        _output = new byte[
            device.GetMaxOutputReportLength()];

        if (_input.Length < 19 ||
            _output.Length < Command.Length + 2)
        {
            _stream.Dispose();
            throw new InvalidOperationException(
                "Nieprawidłowe rozmiary raportów HID.");
        }

        // Bajt 0: Report ID
        // Bajt 1: długość polecenia
        // Bajty 2-7: polecenie UT161E
        _output[0] = 0x00;
        _output[1] = 0x06;

        Array.Copy(
            Command, 0,
            _output, 2,
            Command.Length);
    }

    public UT161EMeasurement? ReadMeasurement()
    {
        _logger.Log("OUT", _output);
        _stream.Write(_output);

        int count = _stream.Read(
            _input, 0, _input.Length);

        if (count <= 0)
            return null;

        _logger.Log("IN", _input.AsSpan(0, count));

        return UT161EDecoder.TryDecode(
            _input.AsSpan(0, count),
            out var measurement)
            ? measurement
            : null;
    }

    public void Dispose()
    {
        _logger.Dispose();
        _stream.Dispose();
    }
}
