namespace VVooOverthrown.Helper.Localization;

public static class TextReplacementPolicy
{
    public static bool ShouldReplace(string source, string current, string korean) =>
        !string.IsNullOrEmpty(source) &&
        !string.IsNullOrEmpty(korean) &&
        !source.Equals(korean, StringComparison.Ordinal) &&
        current.Equals(source, StringComparison.Ordinal);
}
