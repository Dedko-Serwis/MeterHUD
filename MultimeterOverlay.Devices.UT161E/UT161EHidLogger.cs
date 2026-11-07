
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
    public string? LastError { get; private set; }

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


            LastError = null;
            CloseWriter();

            if (LastError is not null)
            {
                IsRecording = false;
                return;
            }

            FilePath = null;
            FrameCount = 0;


            try
            {
                Directory.CreateDirectory(LogDirectory);

                string path = Path.Combine(
                    LogDirectory,
                    $"UT161E_{DateTime.Now:yyyyMMdd_HHmmss_fff}.csv");

                _writer = new StreamWriter(
                    path,
                    append: false,
                    encoding: new UTF8Encoding(false))
                {
                    AutoFlush = true
                };

                _writer.WriteLine("Timestamp;Direction;Length;Hex");
                FilePath = path;
                IsRecording = true;

            }
            catch (Exception ex) when (IsFileError(ex))
            {
                LastError = ex.Message;
                IsRecording = false;
                CloseWriter();
            }
        }
    }

    public void Stop()
    {
        lock (_sync)
        {
            IsRecording = false;
            CloseWriter();
        }
    }

    public void Log(string direction, ReadOnlySpan<byte> data)
    {
        lock (_sync)
        {
            if (!IsRecording || _writer is null)
                return;

            try
            {
                string timestamp = DateTime.Now.ToString(
                    "yyyy-MM-dd HH:mm:ss.fff",
                    CultureInfo.InvariantCulture);

                _writer.WriteLine(
                    $"{timestamp};{direction};{data.Length};{Convert.ToHexString(data)}");

                FrameCount++;
            }
            catch (Exception ex) when (IsFileError(ex))
            {
                LastError = ex.Message;
                IsRecording = false;
                CloseWriter();
            }
        }
    }

    private void CloseWriter()
    {
        StreamWriter? writer = _writer;
        _writer = null;

        if (writer is null)
            return;

        try
        {
            writer.Dispose();
        }
        catch (Exception ex) when (IsFileError(ex))
        {
            LastError ??= ex.Message;
        }
    }

    private static bool IsFileError(Exception ex) =>
        ex is IOException
        or UnauthorizedAccessException
        or System.Security.SecurityException
        or ObjectDisposedException;

    public void Dispose() => Stop();
}
