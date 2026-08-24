namespace VVooOverthrown.App.Services;

public interface ITrainerApplicationService
{
    Task<ApplicationSnapshot> GetSnapshotAsync(string gameRoot, CancellationToken cancellationToken);

    Task InstallAsync(string gameRoot, CancellationToken cancellationToken);

    Task RemoveAsync(string gameRoot, CancellationToken cancellationToken);

    void LaunchGame(string gameRoot);
}

public sealed class ApplicationSnapshot
{
    public ApplicationSnapshot(
        string gameRoot,
        bool pathValid,
        bool buildSupported,
        bool installed,
        bool gameRunning,
        bool helperConnected)
    {
        GameRoot = gameRoot;
        PathValid = pathValid;
        BuildSupported = buildSupported;
        Installed = installed;
        GameRunning = gameRunning;
        HelperConnected = helperConnected;
    }

    public string GameRoot { get; }

    public bool PathValid { get; }

    public bool BuildSupported { get; }

    public bool Installed { get; }

    public bool GameRunning { get; }

    public bool HelperConnected { get; }
}

