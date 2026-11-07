using UT161EReader;
using Xunit;

namespace MultimeterOverlay.Tests;

public sealed class UT161EHidLoggerTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "MultimeterOverlay.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void Start_CreatesCsvWithHeader()
    {
        using var logger = new UT161EHidLogger(_directory);

        logger.Start();

        Assert.True(logger.IsRecording);
        Assert.Null(logger.LastError);
        Assert.NotNull(logger.FilePath);
        Assert.True(File.Exists(logger.FilePath));
        Assert.Equal(0, logger.FrameCount);

        logger.Stop();
        Assert.Equal("Timestamp;Direction;Length;Hex", File.ReadAllLines(logger.FilePath)[0]);
    }

    [Fact]
    public void Log_WritesHexFramesAndIncrementsCounter()
    {
        using var logger = new UT161EHidLogger(_directory);
        logger.Start();

        logger.Log("OUT", new byte[] { 0x00, 0xAB, 0xCD });
        logger.Log("IN", new byte[] { 0x01, 0xFF });

        Assert.Equal(2, logger.FrameCount);
        logger.Stop();

        string[] lines = File.ReadAllLines(Assert.IsType<string>(logger.FilePath));
        Assert.Equal(3, lines.Length);
        Assert.Matches(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3};OUT;3;00ABCD$", lines[1]);
        Assert.Matches(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3};IN;2;01FF$", lines[2]);
    }

    [Fact]
    public void Stop_ClosesFileAndPreventsFurtherWrites()
    {
        using var logger = new UT161EHidLogger(_directory);
        logger.Start();
        logger.Log("IN", new byte[] { 0x01 });
        logger.Stop();

        Assert.False(logger.IsRecording);
        logger.Log("IN", new byte[] { 0x02 });
        Assert.Equal(1, logger.FrameCount);

        string path = Assert.IsType<string>(logger.FilePath);
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Assert.True(stream.CanWrite);
        }
        Assert.Equal(2, File.ReadAllLines(path).Length);
    }

    [Fact]
    public void StartAfterStop_ResetsCounterAndCreatesNewFile()
    {
        using var logger = new UT161EHidLogger(_directory);
        logger.Start();
        logger.Log("OUT", new byte[] { 0xAA });
        string firstPath = Assert.IsType<string>(logger.FilePath);
        logger.Stop();

        logger.Start();
        string secondPath = Assert.IsType<string>(logger.FilePath);
        Assert.True(logger.IsRecording);
        Assert.Equal(0, logger.FrameCount);
        Assert.Null(logger.LastError);
        Assert.NotEqual(firstPath, secondPath);
        logger.Stop();

        Assert.Equal(2, File.ReadAllLines(firstPath).Length);
        Assert.Single(File.ReadAllLines(secondPath));
    }

    [Fact]
    public void Dispose_ClosesFile()
    {
        var logger = new UT161EHidLogger(_directory);
        logger.Start();
        logger.Log("OUT", new byte[] { 0x01, 0x02 });
        string path = Assert.IsType<string>(logger.FilePath);

        logger.Dispose();

        Assert.False(logger.IsRecording);
        Assert.Equal(2, File.ReadAllLines(path).Length);
        using var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        Assert.True(stream.CanWrite);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }
}
