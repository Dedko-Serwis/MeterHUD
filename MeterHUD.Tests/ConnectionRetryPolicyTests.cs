
using MeterHUD.Core;
using Xunit;

namespace MeterHUD.Tests;

public sealed class ConnectionRetryPolicyTests
{
    [Fact]
    public void ThreeFailures_ShouldTriggerReconnect()
    {
        var policy = new ConnectionRetryPolicy(3);

        Assert.False(policy.ShouldReconnect);

        policy.ReportFailure();
        Assert.False(policy.ShouldReconnect);

        policy.ReportFailure();
        Assert.False(policy.ShouldReconnect);

        policy.ReportFailure();
        Assert.True(policy.ShouldReconnect);
        Assert.Equal(3, policy.ConsecutiveFailures);
    }

    [Fact]
    public void Success_ShouldResetFailureCounter()
    {
        var policy = new ConnectionRetryPolicy(3);

        policy.ReportFailure();
        policy.ReportFailure();

        policy.ReportSuccess();

        Assert.Equal(0, policy.ConsecutiveFailures);
        Assert.False(policy.ShouldReconnect);
    }

    [Fact]
    public void Reset_ShouldAllowThreeNewFailures()
    {
        var policy = new ConnectionRetryPolicy(3);

        for (int i = 0; i < 3; i++)
            policy.ReportFailure();

        Assert.True(policy.ShouldReconnect);

        policy.Reset();

        Assert.Equal(0, policy.ConsecutiveFailures);
        Assert.False(policy.ShouldReconnect);

        for (int i = 0; i < 3; i++)
            policy.ReportFailure();

        Assert.True(policy.ShouldReconnect);
    }

    [Fact]
    public void FailureCounter_ShouldNotExceedLimit()
    {
        var policy = new ConnectionRetryPolicy(3);

        for (int i = 0; i < 10; i++)
            policy.ReportFailure();

        Assert.Equal(3, policy.ConsecutiveFailures);
        Assert.True(policy.ShouldReconnect);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidLimit_ShouldThrow(int limit)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ConnectionRetryPolicy(limit));
    }
}
