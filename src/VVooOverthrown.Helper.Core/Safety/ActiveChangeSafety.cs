namespace VVooOverthrown.Helper.Safety;

public static class ActiveChangeSafety
{
    public static bool ShouldReset(
        bool godModeEnabled,
        bool timeScaleChanged,
        SessionDecision decision) =>
        (godModeEnabled || timeScaleChanged) && decision != SessionDecision.Allowed;
}
