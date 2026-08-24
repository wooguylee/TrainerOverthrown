namespace VVooOverthrown.Core.State;

public sealed class TrainerMainState
{
    private TrainerMainState(TrainerStage stage, string message, bool canInstall, bool canRemove)
    {
        Stage = stage;
        Message = message;
        CanInstall = canInstall;
        CanRemove = canRemove;
    }

    public TrainerStage Stage { get; }

    public string Message { get; }

    public bool CanInstall { get; }

    public bool CanRemove { get; }

    public static TrainerMainState Evaluate(
        bool pathValid,
        bool buildSupported,
        bool installed,
        bool gameRunning,
        bool helperConnected)
    {
        if (!pathValid)
        {
            return new TrainerMainState(TrainerStage.GamePathRequired, "게임 경로 확인 필요", false, false);
        }

        if (!buildSupported)
        {
            return new TrainerMainState(TrainerStage.UnsupportedBuild, "지원하지 않는 게임 빌드", false, false);
        }

        if (!installed)
        {
            return new TrainerMainState(TrainerStage.InstallAvailable, "한글 패치 설치 가능", true, false);
        }

        if (!gameRunning)
        {
            return new TrainerMainState(TrainerStage.GameLaunchRequired, "게임 실행 필요", false, true);
        }

        if (!helperConnected)
        {
            return new TrainerMainState(TrainerStage.HelperConnectionRequired, "게임 연결 필요", false, false);
        }

        return new TrainerMainState(TrainerStage.Ready, "연결됨 · 싱글플레이 확인 중", false, false);
    }
}

public enum TrainerStage
{
    GamePathRequired,
    UnsupportedBuild,
    InstallAvailable,
    GameLaunchRequired,
    HelperConnectionRequired,
    Ready
}

