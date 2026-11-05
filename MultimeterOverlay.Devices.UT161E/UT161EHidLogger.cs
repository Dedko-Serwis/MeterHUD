using System.Globalization;
using System.Text;

namespace UT161EReader;

public sealed class UT161EHidLogger : IDisposable
{
    private readonly object _sync = new();
    private StreamWriter? _writer;

    public string? FilePath { get; private set; }
    public int FrameCount { get; private set; }
    public bool IsRecording { get; private set; }

    public static string LogDirectory => Path.Combine(
        Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData),
        "MultimeterOverlay",
        "Logs");

    public void Start()
    {
        lock (_sync)
        {
            if (IsRecording)
                return;

            Directory.CreateDirectory(LogDirectory);

            FilePath = Path.Combine(
                LogDirectory,
                $"UT161E_{DateTime.Now:yyyyMMdd_HHmmss_fff}.csv");

            _writer = new StreamWriter(
                FilePath,
                append: false,
                encoding: new UTF8Encoding(false))
            {
                AutoFlush = true
            };

            _writer.WriteLine("Timestamp;Direction;Length;Hex");

            FrameCount = 0;
            IsRecording = true;
        }
    }

    public void Stop()
    {
        lock (_sync)
        {
            IsRecording = false;
            _writer?.Dispose();
            _writer = null;
        }
    }

    public void Log(string direction, ReadOnlySpan<byte> data)
    {
        lock (_sync)
        {
            if (!IsRecording || _writer is null)
                return;

            string timestamp = DateTime.Now.ToString(
                "yyyy-MM-dd HH:mm:ss.fff",
                CultureInfo.InvariantCulture);

            _writer.WriteLine(
                $"{timestamp};{direction};{data.Length};{Convert.ToHexString(data)}");

            FrameCount++;
        }
    }

    public void Dispose() => Stop();
}