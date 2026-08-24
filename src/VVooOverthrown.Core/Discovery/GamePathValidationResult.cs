namespace VVooOverthrown.Core.Discovery;

public sealed class GamePathValidationResult
{
    private GamePathValidationResult(bool isValid, string? gameRoot, string? errorMessage)
    {
        IsValid = isValid;
        GameRoot = gameRoot;
        ErrorMessage = errorMessage;
    }

    public bool IsValid { get; }

    public string? GameRoot { get; }

    public string? ErrorMessage { get; }

    public static GamePathValidationResult Valid(string gameRoot) => new(true, gameRoot, null);

    public static GamePathValidationResult Invalid(string errorMessage) => new(false, null, errorMessage);
}

