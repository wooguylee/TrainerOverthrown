namespace VVooOverthrown.Helper.Safety;

public enum SessionDecision
{
    Allowed,
    RemoteParticipant,
    Uncertain,
}

public sealed class SessionSnapshot
{
    public SessionSnapshot(
        bool offlineMode,
        bool authoritativeHost,
        int connectionCount,
        bool remoteParticipantDetected)
    {
        OfflineMode = offlineMode;
        AuthoritativeHost = authoritativeHost;
        ConnectionCount = connectionCount;
        RemoteParticipantDetected = remoteParticipantDetected;
    }

    public bool OfflineMode { get; }

    public bool AuthoritativeHost { get; }

    public int ConnectionCount { get; }

    public bool RemoteParticipantDetected { get; }
}

public sealed class OfflineSessionGuard
{
    public SessionDecision Evaluate(SessionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.RemoteParticipantDetected || snapshot.ConnectionCount > 1)
        {
            return SessionDecision.RemoteParticipant;
        }

        if (!snapshot.OfflineMode ||
            !snapshot.AuthoritativeHost ||
            snapshot.ConnectionCount != 1)
        {
            return SessionDecision.Uncertain;
        }

        return SessionDecision.Allowed;
    }
}
