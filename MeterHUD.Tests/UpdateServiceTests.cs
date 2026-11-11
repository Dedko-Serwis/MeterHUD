using MeterHUD.Services;

namespace MeterHUD.Tests;

public sealed class UpdateServiceTests
{
    [Theory]
    [InlineData("v0.2.0", 0, 2, 0)]
    [InlineData("0.2.0", 0, 2, 0)]
    [InlineData("V1.10.12", 1, 10, 12)]
    public void ParseReleaseVersion_Valid(string tag, int major, int minor, int patch)
    {
        Assert.True(UpdateService.TryParseReleaseVersion(tag, out var result));
        Assert.Equal(new Version(major, minor, patch), result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("v0.2.0-beta")]
    [InlineData("invalid")]
    [InlineData("0.2")]
    public void ParseReleaseVersion_Invalid(string? tag)
    {
        Assert.False(UpdateService.TryParseReleaseVersion(tag, out _));
    }

    [Fact]
    public void VersionComparison_UsesNumericComponents()
    {
        Assert.True(UpdateService.IsNewer(new Version(0, 2, 0), new Version(0, 10, 0)));
        Assert.False(UpdateService.IsNewer(new Version(0, 2, 0), new Version(0, 2, 0)));
        Assert.False(UpdateService.IsNewer(new Version(1, 0, 0), new Version(0, 9, 9)));
    }

    [Fact]
    public void Schedule_RespectsSettings()
    {
        var now = new DateTimeOffset(2026, 11, 11, 12, 0, 0, TimeSpan.Zero);
        Assert.False(UpdateService.ShouldCheck(0, null, now));
        Assert.True(UpdateService.ShouldCheck(-1, now, now));
        Assert.True(UpdateService.ShouldCheck(7, null, now));
        Assert.False(UpdateService.ShouldCheck(7, now.AddDays(-6), now));
        Assert.True(UpdateService.ShouldCheck(7, now.AddDays(-7), now));
        Assert.False(UpdateService.ShouldCheck(30, now.AddDays(-10), now));
    }

    [Fact]
    public void InstallerValidation_RejectsMissingHashAndForeignHosts()
    {
        var hash = new string('a', 64);
        Assert.True(UpdateService.HasVerifiedInstaller(new UpdateRelease(new Version(0, 3, 0), "", "https://github.com/Dedko-Serwis/MeterHUD/releases/download/v0.3.0/MeterHUD-0.3.0-Setup.exe", hash)));
        Assert.False(UpdateService.HasVerifiedInstaller(new UpdateRelease(new Version(0, 3, 0), "", "https://evil.example/Setup.exe", hash)));
        Assert.False(UpdateService.HasVerifiedInstaller(new UpdateRelease(new Version(0, 3, 0), "", "https://github.com/example", null)));
        Assert.False(UpdateService.HasVerifiedInstaller(new UpdateRelease(new Version(0, 3, 0), "", "https://github.com/example", "wrong")));
    }
}
