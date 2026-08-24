using System.ComponentModel;
using System.Runtime.CompilerServices;
using VVooOverthrown.App.Services;
using VVooOverthrown.Core.Discovery;
using VVooOverthrown.Core.State;
using VVooOverthrown.Helper.Transport;

namespace VVooOverthrown.App.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly ITrainerApplicationService _service;
    private string _gamePath = GameLocator.DefaultGameRoot;
    private string _statusMessage = "게임 상태 확인 전";
    private bool _canInstall;
    private bool _canRemove;
    private bool _isBusy;
    private string _eventLog = "VVooOverthrown 준비됨";
    private bool _canConnectHelper;
    private bool _canUseTrainer;
    private bool _godModeEnabled;
    private string _sessionMessage = "Helper 연결 전";
    private string _timeScaleMessage = "1.0x";

    public MainViewModel(ITrainerApplicationService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string GamePath
    {
        get => _gamePath;
        set => SetProperty(ref _gamePath, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool CanInstall
    {
        get => _canInstall;
        private set => SetProperty(ref _canInstall, value);
    }

    public bool CanRemove
    {
        get => _canRemove;
        private set => SetProperty(ref _canRemove, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public string EventLog
    {
        get => _eventLog;
        private set => SetProperty(ref _eventLog, value);
    }

    public bool CanConnectHelper
    {
        get => _canConnectHelper;
        private set => SetProperty(ref _canConnectHelper, value);
    }

    public bool CanUseTrainer
    {
        get => _canUseTrainer;
        private set => SetProperty(ref _canUseTrainer, value);
    }

    public bool GodModeEnabled
    {
        get => _godModeEnabled;
        private set => SetProperty(ref _godModeEnabled, value);
    }

    public string SessionMessage
    {
        get => _sessionMessage;
        private set => SetProperty(ref _sessionMessage, value);
    }

    public string TimeScaleMessage
    {
        get => _timeScaleMessage;
        private set => SetProperty(ref _timeScaleMessage, value);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        try
        {
            var snapshot = await _service.GetSnapshotAsync(GamePath, cancellationToken);
            GamePath = snapshot.GameRoot;
            var state = TrainerMainState.Evaluate(
                snapshot.PathValid,
                snapshot.BuildSupported,
                snapshot.Installed,
                snapshot.GameRunning,
                snapshot.HelperConnected);
            StatusMessage = state.Message;
            CanInstall = state.CanInstall;
            CanRemove = state.CanRemove;
            CanConnectHelper = snapshot.BuildSupported && snapshot.Installed && snapshot.GameRunning;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task InstallAsync(CancellationToken cancellationToken = default)
    {
        await RunOperationAsync(
            "한글 패치와 Helper를 설치했습니다.",
            token => _service.InstallAsync(GamePath, token),
            cancellationToken);
    }

    public async Task RemoveAsync(CancellationToken cancellationToken = default)
    {
        await RunOperationAsync(
            "VVooOverthrown 소유 파일을 제거했습니다.",
            token => _service.RemoveAsync(GamePath, token),
            cancellationToken);
    }

    public void LaunchGame()
    {
        try
        {
            _service.LaunchGame(GamePath);
            AppendLog("게임 실행을 요청했습니다.");
        }
        catch (Exception exception)
        {
            AppendLog($"오류: {exception.Message}");
        }
    }

    public async Task ConnectHelperAsync(CancellationToken cancellationToken = default)
    {
        await RunTrainerOperationAsync(
            token => _service.ConnectHelperAsync(GamePath, token),
            "Helper에 연결했습니다.",
            cancellationToken);
    }

    public async Task SetGodModeAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        await RunTrainerOperationAsync(
            token => _service.SendTrainerCommandAsync(
                new PipeRequest { Command = "godMode", Enabled = enabled }, token),
            enabled ? "플레이어 무적을 켰습니다." : "플레이어 무적을 껐습니다.",
            cancellationToken);
    }

    public async Task SetTimeScaleAsync(float value, CancellationToken cancellationToken = default)
    {
        await RunTrainerOperationAsync(
            token => _service.SendTrainerCommandAsync(
                new PipeRequest { Command = "timeScale", Value = value }, token),
            $"시간 배속을 {value:0.##}x로 설정했습니다.",
            cancellationToken);
    }

    public Task ResetAndDisconnectAsync(CancellationToken cancellationToken = default) =>
        _service.ResetAndDisconnectAsync(cancellationToken);

    private async Task RunTrainerOperationAsync(
        Func<CancellationToken, Task<PipeResponse>> operation,
        string successMessage,
        CancellationToken cancellationToken)
    {
        IsBusy = true;
        try
        {
            var response = await operation(cancellationToken);
            ApplyTrainerResponse(response);
            AppendLog(response.Ok ? successMessage : $"차단: {response.Message} ({response.ErrorCode})");
        }
        catch (Exception exception)
        {
            CanUseTrainer = false;
            SessionMessage = "Helper 연결 실패";
            AppendLog($"오류: {exception.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyTrainerResponse(PipeResponse response)
    {
        var allowed = response.SessionDecision.Equals("Allowed", StringComparison.OrdinalIgnoreCase);
        CanUseTrainer = response.Ok && allowed;
        SessionMessage = response.SessionDecision switch
        {
            "Allowed" => "로컬 싱글플레이 확인됨",
            "RemoteParticipant" => "멀티플레이 감지 · 변경 차단",
            _ => "로컬 세션 미확인 · 변경 차단",
        };
        GodModeEnabled = response.GodModeEnabled;
        TimeScaleMessage = $"{response.TimeScale:0.##}x";
    }

    private async Task RunOperationAsync(
        string successMessage,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        IsBusy = true;
        try
        {
            await operation(cancellationToken);
            AppendLog(successMessage);
        }
        catch (Exception exception)
        {
            AppendLog($"오류: {exception.Message}");
        }
        finally
        {
            IsBusy = false;
            await RefreshAsync(cancellationToken);
        }
    }

    private void AppendLog(string message)
    {
        EventLog = $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}{EventLog}";
    }

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
