using VVooOverthrown.Helper.Safety;
using Xunit;

namespace VVooOverthrown.Helper.Tests;

public sealed class ActiveChangeSafetyTests
{
    [Theory]
    [InlineData(false, true, SessionDecision.RemoteParticipant, true)]
    [InlineData(false, true, SessionDecision.Uncertain, true)]
    [InlineData(true, false, SessionDecision.RemoteParticipant, true)]
    [InlineData(true, true, SessionDecision.Allowed, false)]
    [InlineData(false, false, SessionDecision.RemoteParticipant, false)]
    public void ResetsEveryActiveChangeWhenSessionIsNotAllowed(
        bool godModeEnabled,
        bool timeScaleChanged,
        SessionDecision decision,
        bool expected)
    {
        Assert.Equal(
            expected,
            ActiveChangeSafety.ShouldReset(godModeEnabled, timeScaleChanged, decision));
    }
}
