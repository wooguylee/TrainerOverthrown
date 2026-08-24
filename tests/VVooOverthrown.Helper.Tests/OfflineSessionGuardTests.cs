using VVooOverthrown.Helper.Safety;
using Xunit;

namespace VVooOverthrown.Helper.Tests;

public sealed class OfflineSessionGuardTests
{
    [Theory]
    [InlineData(true, true, 1, false, SessionDecision.Allowed)]
    [InlineData(true, true, 2, true, SessionDecision.RemoteParticipant)]
    [InlineData(false, true, 1, false, SessionDecision.Uncertain)]
    [InlineData(true, false, 0, false, SessionDecision.Uncertain)]
    [InlineData(true, true, -1, false, SessionDecision.Uncertain)]
    public void SessionDecisionFailsClosed(
        bool offlineMode,
        bool authoritativeHost,
        int connections,
        bool remoteParticipant,
        SessionDecision expected)
    {
        var snapshot = new SessionSnapshot(
            offlineMode,
            authoritativeHost,
            connections,
            remoteParticipant);

        Assert.Equal(expected, new OfflineSessionGuard().Evaluate(snapshot));
    }
}
