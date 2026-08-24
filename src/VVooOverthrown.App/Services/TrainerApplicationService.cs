using System.Diagnostics;
using System.IO;
using VVooOverthrown.Core.Build;
using VVooOverthrown.Core.Discovery;
using VVooOverthrown.Core.Installation;
using VVooOverthrown.Core.Saves;
using VVooOverthrown.Core.Transport;
using VVooOverthrown.Helper.Transport;

namespace VVooOverthrown.App.Services;

public sealed class TrainerApplicationService : ITrainerApplicationService
{
    private readonly SupportedBuildProfile _profile;
    private readonly string _payloadRoot;
    private readonly string _userDataRoot;
    private readonly string _backupRoot;
    private readonly Func<string, bool> _isGameRunning;
    private readonly GameBuildValidator _buildValidator = new();
    private TrainerPipeClient? _trainerClient;

    public TrainerApplicationService()
        : this(
            SupportedBuildProfile.Current,
            Path.Combine(AppContext.BaseDirectory, "payload"),
            GetDefaultUserDataRoot(),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "VVooOverthrown",
                "Backups"),
            IsMatchingGameRunning)
    {
    }

    public TrainerApplicationService(
        SupportedBuildProfile profile,
        string payloadRoot,
        string userDataRoot,
        string backupRoot,
        Func<string, bool> isGameRunning)
    {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _payloadRoot = Path.GetFullPath(payloadRoot);
        _userDataRoot = Path.GetFullPath(userDataRoot);
        _backupRoot = Path.GetFullPath(backupRoot);
        _isGameRunning = isGameRunning ?? throw new ArgumentNullException(nameof(isGameRunning));
    }

    public async Task<ApplicationSnapshot> GetSnapshotAsync(
        string gameRoot,
        CancellationToken cancellationToken)
    {
        var location = GameLocator.ValidateRoot(gameRoot);
        if (!location.IsValid)
        {
            return new ApplicationSnapshot(
                gameRoot,
                pathValid: false,
                buildSupported: false,
                installed: false,
                gameRunning: false,
                helperConnected: false);
        }

        var normalizedRoot = location.GameRoot!;
        var build = await _buildValidator.ValidateAsync(normalizedRoot, _profile, cancellationToken);
        return new ApplicationSnapshot(
            normalizedRoot,
            pathValid: true,
            buildSupported: build.IsSupported,
            installed: File.Exists(PayloadInstaller.GetManifestPath(normalizedRoot)),
            gameRunning: _isGameRunning(normalizedRoot),
            helperConnected: _trainerClient?.IsConnected == true);
    }

    public async Task InstallAsync(string gameRoot, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_payloadRoot))
        {
            throw new DirectoryNotFoundException($"설치 payload를 찾을 수 없습니다: {_payloadRoot}");
        }

        if (Directory.Exists(_userDataRoot))
        {
            await new SaveBackupService().CreateAsync(_userDataRoot, _backupRoot, cancellationToken);
        }

        var installer = new PayloadInstaller(_buildValidator, () => _isGameRunning(gameRoot));
        await installer.InstallAsync(gameRoot, _payloadRoot, _profile, cancellationToken);
    }

    public async Task RemoveAsync(string gameRoot, CancellationToken cancellationToken)
    {
        var installer = new PayloadInstaller(_buildValidator, () => _isGameRunning(gameRoot));
        await installer.RemoveAsync(gameRoot, cancellationToken);
    }

    public void LaunchGame(string gameRoot)
    {
        var location = GameLocator.ValidateRoot(gameRoot);
        if (!location.IsValid)
        {
            throw new InvalidOperationException(location.ErrorMessage);
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = Path.Combine(location.GameRoot!, "Overthrown.exe"),
            WorkingDirectory = location.GameRoot,
            UseShellExecute = true
        });
    }

    public async Task<PipeResponse> ConnectHelperAsync(
        string gameRoot,
        CancellationToken cancellationToken)
    {
        var build = await _buildValidator.ValidateAsync(gameRoot, _profile, cancellationToken);
        if (!build.IsSupported)
        {
            throw new InvalidOperationException("지원하지 않는 게임 빌드에는 연결할 수 없습니다.");
        }

        var processId = FindMatchingGameProcessId(gameRoot)
                        ?? throw new InvalidOperationException("실행 중인 Overthrown을 찾을 수 없습니다.");
        if (_trainerClient is not null)
        {
            await _trainerClient.DisposeAsync();
        }

        _trainerClient = new TrainerPipeClient();
        await _trainerClient.ConnectAsync(processId, cancellationToken);
        return await _trainerClient.SendAsync(
            new PipeRequest { Command = "status" }, cancellationToken);
    }

    public Task<PipeResponse> SendTrainerCommandAsync(
        PipeRequest request,
        CancellationToken cancellationToken) =>
        _trainerClient?.SendAsync(request, cancellationToken)
        ?? throw new InvalidOperationException("Helper에 먼저 연결하세요.");

    public async Task ResetAndDisconnectAsync(CancellationToken cancellationToken)
    {
        if (_trainerClient is null)
        {
            return;
        }

        try
        {
            if (_trainerClient.IsConnected)
            {
                await _trainerClient.SendAsync(
                    new PipeRequest { Command = "reset" }, cancellationToken);
            }
        }
        catch
        {
            // The game may already be closed. Disposal still releases the local pipe.
        }
        finally
        {
            await _trainerClient.DisposeAsync();
            _trainerClient = null;
        }
    }

    private static string GetDefaultUserDataRoot()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appData = Directory.GetParent(local)?.FullName ?? local;
        return Path.Combine(appData, "LocalLow", "Brimstone", "Overthrown");
    }

    private static bool IsMatchingGameRunning(string gameRoot)
    {
        var expectedPath = Path.GetFullPath(Path.Combine(gameRoot, "Overthrown.exe"));
        foreach (var process in Process.GetProcessesByName("Overthrown"))
        {
            using (process)
            {
                try
                {
                    var actualPath = process.MainModule?.FileName;
                    if (actualPath is not null &&
                        Path.GetFullPath(actualPath).Equals(expectedPath, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
                {
                    // A process whose path cannot be verified is not treated as the target installation.
                }
            }
        }

        return false;
    }

    private static int? FindMatchingGameProcessId(string gameRoot)
    {
        var expectedPath = Path.GetFullPath(Path.Combine(gameRoot, "Overthrown.exe"));
        foreach (var process in Process.GetProcessesByName("Overthrown"))
        {
            using (process)
            {
                try
                {
                    if (process.MainModule?.FileName is { } path &&
                        Path.GetFullPath(path).Equals(expectedPath, StringComparison.OrdinalIgnoreCase))
                    {
                        return process.Id;
                    }
                }
                catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
                {
                    // Ignore processes whose executable path cannot be verified.
                }
            }
        }

        return null;
    }
}
