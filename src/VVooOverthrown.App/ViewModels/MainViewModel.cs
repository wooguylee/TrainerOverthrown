using System.ComponentModel;
using System.Runtime.CompilerServices;
using VVooOverthrown.App.Services;
using VVooOverthrown.Core.Discovery;
using VVooOverthrown.Core.State;

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
