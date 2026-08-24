namespace VVooOverthrown.Core.Build;

public sealed class BuildValidationResult
{
    public BuildValidationResult(string profileName, IReadOnlyList<string> mismatches)
    {
        ProfileName = profileName;
        Mismatches = mismatches;
    }

    public string ProfileName { get; }

    public IReadOnlyList<string> Mismatches { get; }

    public bool IsSupported => Mismatches.Count == 0;
}

