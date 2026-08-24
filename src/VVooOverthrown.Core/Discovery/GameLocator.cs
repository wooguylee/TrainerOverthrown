namespace VVooOverthrown.Core.Discovery;

public static class GameLocator
{
    public const string DefaultGameRoot = @"W:\Games\Overthrown";

    public static GamePathValidationResult ValidateRoot(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return GamePathValidationResult.Invalid("게임 경로를 입력하세요.");
        }

        string normalizedRoot;
        try
        {
            normalizedRoot = Path.GetFullPath(path);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return GamePathValidationResult.Invalid("게임 경로 형식이 올바르지 않습니다.");
        }

        if (!Directory.Exists(normalizedRoot))
        {
            return GamePathValidationResult.Invalid("게임 폴더를 찾을 수 없습니다.");
        }

        if (!File.Exists(Path.Combine(normalizedRoot, "Overthrown.exe")))
        {
            return GamePathValidationResult.Invalid("Overthrown.exe를 찾을 수 없습니다.");
        }

        if (!Directory.Exists(Path.Combine(normalizedRoot, "Overthrown_Data")))
        {
            return GamePathValidationResult.Invalid("Overthrown_Data 폴더를 찾을 수 없습니다.");
        }

        return GamePathValidationResult.Valid(normalizedRoot);
    }
}

