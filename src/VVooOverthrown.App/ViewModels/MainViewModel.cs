using System.ComponentModel;
using System.Runtime.CompilerServices;
using VVooOverthrown.App.Services;
using VVooOverthrown.Core.Discovery;
using VVooOverthrown.Core.State;
using VVooOverthrown.Helper.Features;
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
    private string _staminaFactorMessage = "1.0x";
    private string _movementSpeedMessage = "1.0x";
    private string _inventoryResultMessage = "조회 전";
    private string _kingdomResultMessage = "조회 전";
    private string _diagnosticMessage = "Helper 연결 후 런타임 상태가 표시됩니다.";
    private string _capabilitiesMessage = "확인 전";
    private string _inventoryAmountInput = "100";
    private string _kingdomAmountInput = "100";
    private bool _isInventoryAmountValid = true;
    private bool _isKingdomAmountValid = true;
    private string _inventoryInputMessage = "0~1,000,000,000 정수";
    private string _kingdomInputMessage = "0~1,000,000,000 정수";
    private TrainerResourceOption _selectedInventoryResource = TrainerResourceOptions.All[0];
    private TrainerResourceOption _selectedKingdomResource = TrainerResourceOptions.All[0];

    public MainViewModel(ITrainerApplicationService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<TrainerResourceOption> ResourceOptions => TrainerResourceOptions.All;

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
        private set
        {
            if (SetProperty(ref _canUseTrainer, value))
            {
                OnPropertyChanged(nameof(CanMutateInventoryResource));
                OnPropertyChanged(nameof(CanMutateKingdomResource));
            }
        }
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

    public string StaminaFactorMessage
    {
        get => _staminaFactorMessage;
        private set => SetProperty(ref _staminaFactorMessage, value);
    }

    public string MovementSpeedMessage
    {
        get => _movementSpeedMessage;
        private set => SetProperty(ref _movementSpeedMessage, value);
    }

    public string InventoryResultMessage
    {
        get => _inventoryResultMessage;
        private set => SetProperty(ref _inventoryResultMessage, value);
    }

    public string KingdomResultMessage
    {
        get => _kingdomResultMessage;
        private set => SetProperty(ref _kingdomResultMessage, value);
    }

    public string DiagnosticMessage
    {
        get => _diagnosticMessage;
        private set => SetProperty(ref _diagnosticMessage, value);
    }

    public string CapabilitiesMessage
    {
        get => _capabilitiesMessage;
        private set => SetProperty(ref _capabilitiesMessage, value);
    }

    public TrainerResourceOption SelectedInventoryResource
    {
        get => _selectedInventoryResource;
        set => SetProperty(ref _selectedInventoryResource, value);
    }

    public TrainerResourceOption SelectedKingdomResource
    {
        get => _selectedKingdomResource;
        set => SetProperty(ref _selectedKingdomResource, value);
    }

    public string InventoryAmountInput
    {
        get => _inventoryAmountInput;
        set
        {
            if (SetProperty(ref _inventoryAmountInput, value))
            {
                ValidateInventoryAmount();
            }
        }
    }

    public string KingdomAmountInput
    {
        get => _kingdomAmountInput;
        set
        {
            if (SetProperty(ref _kingdomAmountInput, value))
            {
                ValidateKingdomAmount();
            }
        }
    }

    public bool IsInventoryAmountValid
    {
        get => _isInventoryAmountValid;
        private set
        {
            if (SetProperty(ref _isInventoryAmountValid, value))
            {
                OnPropertyChanged(nameof(CanMutateInventoryResource));
            }
        }
    }

    public bool IsKingdomAmountValid
    {
        get => _isKingdomAmountValid;
        private set
        {
            if (SetProperty(ref _isKingdomAmountValid, value))
            {
                OnPropertyChanged(nameof(CanMutateKingdomResource));
            }
        }
    }

    public bool CanMutateInventoryResource => CanUseTrainer && IsInventoryAmountValid;

    public bool CanMutateKingdomResource => CanUseTrainer && IsKingdomAmountValid;

    public string InventoryInputMessage
    {
        get => _inventoryInputMessage;
        private set => SetProperty(ref _inventoryInputMessage, value);
    }

    public string KingdomInputMessage
    {
        get => _kingdomInputMessage;
        private set => SetProperty(ref _kingdomInputMessage, value);
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
                new PipeRequest { Command = TrainerCommands.GodMode, Enabled = enabled }, token),
            enabled ? "플레이어 무적을 켰습니다." : "플레이어 무적을 껐습니다.",
            cancellationToken);
    }

    public async Task HealAsync(CancellationToken cancellationToken = default)
    {
        await RunTrainerOperationAsync(
            token => _service.SendTrainerCommandAsync(
                new PipeRequest { Command = TrainerCommands.Heal }, token),
            "플레이어 체력을 회복했습니다.",
            cancellationToken);
    }

    public async Task SetStaminaFactorAsync(float value, CancellationToken cancellationToken = default)
    {
        await RunTrainerOperationAsync(
            token => _service.SendTrainerCommandAsync(
                new PipeRequest { Command = TrainerCommands.StaminaFactor, Value = value }, token),
            $"기력 소모 배율을 {value:0.##}x로 설정했습니다.",
            cancellationToken);
    }

    public async Task SetMovementSpeedAsync(float value, CancellationToken cancellationToken = default)
    {
        await RunTrainerOperationAsync(
            token => _service.SendTrainerCommandAsync(
                new PipeRequest { Command = TrainerCommands.MovementSpeed, Value = value }, token),
            $"이동 속도 배율을 {value:0.##}x로 설정했습니다.",
            cancellationToken);
    }

    public async Task SetTimeScaleAsync(float value, CancellationToken cancellationToken = default)
    {
        await RunTrainerOperationAsync(
            token => _service.SendTrainerCommandAsync(
                new PipeRequest { Command = TrainerCommands.TimeScale, Value = value }, token),
            $"시간 배속을 {value:0.##}x로 설정했습니다.",
            cancellationToken);
    }

    public Task QueryInventoryResourceAsync(CancellationToken cancellationToken = default) =>
        SendInventoryCommandAsync(TrainerCommands.InventoryQuery, 0, "인벤토리 자원을 조회했습니다.", cancellationToken);

    public Task SetInventoryResourceAsync(CancellationToken cancellationToken = default) =>
        TryGetInventoryAmount(out var amount)
            ? SendInventoryCommandAsync(TrainerCommands.InventorySet, amount, "인벤토리 자원을 설정했습니다.", cancellationToken)
            : InvalidInventoryInputAsync();

    public Task AddInventoryResourceAsync(CancellationToken cancellationToken = default) =>
        TryGetInventoryAmount(out var amount)
            ? SendInventoryCommandAsync(TrainerCommands.InventoryAdd, amount, "인벤토리 자원을 추가했습니다.", cancellationToken)
            : InvalidInventoryInputAsync();

    public Task SubtractInventoryResourceAsync(CancellationToken cancellationToken = default) =>
        TryGetInventoryAmount(out var amount)
            ? SendInventoryCommandAsync(TrainerCommands.InventoryAdd, -amount, "인벤토리 자원을 차감했습니다.", cancellationToken)
            : InvalidInventoryInputAsync();

    public Task QueryKingdomResourceAsync(CancellationToken cancellationToken = default) =>
        SendKingdomCommandAsync(TrainerCommands.KingdomQuery, 0, "왕국 자원을 조회했습니다.", cancellationToken);

    public Task SetKingdomResourceAsync(CancellationToken cancellationToken = default) =>
        TryGetKingdomAmount(out var amount)
            ? SendKingdomCommandAsync(TrainerCommands.KingdomSet, amount, "왕국 자원을 설정했습니다.", cancellationToken)
            : InvalidKingdomInputAsync();

    public Task AddKingdomResourceAsync(CancellationToken cancellationToken = default) =>
        TryGetKingdomAmount(out var amount)
            ? SendKingdomCommandAsync(TrainerCommands.KingdomAdd, amount, "왕국 자원을 추가했습니다.", cancellationToken)
            : InvalidKingdomInputAsync();

    public Task SubtractKingdomResourceAsync(CancellationToken cancellationToken = default) =>
        TryGetKingdomAmount(out var amount)
            ? SendKingdomCommandAsync(TrainerCommands.KingdomAdd, -amount, "왕국 자원을 차감했습니다.", cancellationToken)
            : InvalidKingdomInputAsync();

    public async Task ResetTrainerAsync(CancellationToken cancellationToken = default)
    {
        await RunTrainerOperationAsync(
            token => _service.SendTrainerCommandAsync(
                new PipeRequest { Command = TrainerCommands.Reset }, token),
            "무적·기력·이동 속도·시간 배속을 원래 값으로 복원했습니다.",
            cancellationToken);
    }

    public Task ResetAndDisconnectAsync(CancellationToken cancellationToken = default) =>
        _service.ResetAndDisconnectAsync(cancellationToken);

    private async Task SendInventoryCommandAsync(
        string command,
        int amount,
        string successMessage,
        CancellationToken cancellationToken)
    {
        var selected = SelectedInventoryResource;
        var response = await RunTrainerOperationAsync(
            token => _service.SendTrainerCommandAsync(new PipeRequest
            {
                Command = command,
                ResourceType = selected.Value,
                Amount = amount,
            }, token),
            successMessage,
            cancellationToken);
        if (response is not null && response.SelectedResourceType == selected.Value)
        {
            InventoryResultMessage = $"{selected.Label} 실제값: {response.InventoryAmount:N0}";
        }
    }

    private async Task SendKingdomCommandAsync(
        string command,
        int amount,
        string successMessage,
        CancellationToken cancellationToken)
    {
        var selected = SelectedKingdomResource;
        var response = await RunTrainerOperationAsync(
            token => _service.SendTrainerCommandAsync(new PipeRequest
            {
                Command = command,
                ResourceType = selected.Value,
                Amount = amount,
            }, token),
            successMessage,
            cancellationToken);
        if (response is not null && response.SelectedResourceType == selected.Value)
        {
            KingdomResultMessage = $"{selected.Label} 실제값: {response.KingdomAmount:N0}";
        }
    }

    private async Task<PipeResponse?> RunTrainerOperationAsync(
        Func<CancellationToken, Task<PipeResponse>> operation,
        string successMessage,
        CancellationToken cancellationToken)
    {
        IsBusy = true;
        try
        {
            var response = await operation(cancellationToken);
            ApplyTrainerResponse(response);
            AppendLog(response.Ok ? successMessage : $"실패: {response.Message} ({response.ErrorCode})");
            return response;
        }
        catch (Exception exception)
        {
            CanUseTrainer = false;
            SessionMessage = "Helper 연결 실패";
            AppendLog($"오류: {exception.Message}");
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyTrainerResponse(PipeResponse response)
    {
        CanUseTrainer = response.TestModeEnabled;
        SessionMessage = !response.TestModeEnabled
            ? "Helper 버전 불일치 · 업데이트 필요"
            : response.SessionDecision switch
            {
                "Allowed" => "테스트 모드 · 세션 판정: 로컬",
                "RemoteParticipant" => "테스트 모드 · 세션 판정: 원격 참가자 감지",
                _ => "테스트 모드 · 세션 판정: 미확인",
            };

        GodModeEnabled = response.GodModeEnabled;
        TimeScaleMessage = $"{response.TimeScale:0.##}x";
        StaminaFactorMessage = $"{response.StaminaFactor:0.##}x";
        MovementSpeedMessage = $"{response.MovementSpeedMultiplier:0.##}x";
        CapabilitiesMessage = response.Capabilities.Length == 0
            ? "보고된 기능 없음"
            : string.Join(Environment.NewLine, response.Capabilities);
        DiagnosticMessage =
            $"세션 원시 판정: {response.SessionDecision}{Environment.NewLine}" +
            $"OfflineMode: {response.OfflineMode}{Environment.NewLine}" +
            $"AuthoritativeHost: {response.AuthoritativeHost}{Environment.NewLine}" +
            $"Connections: {response.ConnectionCount}{Environment.NewLine}" +
            $"RemoteParticipant: {response.RemoteParticipant}{Environment.NewLine}" +
            $"플레이어: {ReadyText(response.PlayerReady)} · " +
            $"인벤토리: {ReadyText(response.InventoryReady)} · " +
            $"왕국 저장소: {ReadyText(response.KingdomStorageReady)}";
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

    private static string ReadyText(bool ready) => ready ? "준비됨" : "미로드";

    private void ValidateInventoryAmount()
    {
        IsInventoryAmountValid = TryParseResourceAmount(InventoryAmountInput, out _);
        InventoryInputMessage = IsInventoryAmountValid
            ? "0~1,000,000,000 정수"
            : "입력 오류 · 0~1,000,000,000 정수만 사용할 수 있습니다.";
    }

    private void ValidateKingdomAmount()
    {
        IsKingdomAmountValid = TryParseResourceAmount(KingdomAmountInput, out _);
        KingdomInputMessage = IsKingdomAmountValid
            ? "0~1,000,000,000 정수"
            : "입력 오류 · 0~1,000,000,000 정수만 사용할 수 있습니다.";
    }

    private bool TryGetInventoryAmount(out int amount) =>
        TryParseResourceAmount(InventoryAmountInput, out amount);

    private bool TryGetKingdomAmount(out int amount) =>
        TryParseResourceAmount(KingdomAmountInput, out amount);

    private Task InvalidInventoryInputAsync()
    {
        ValidateInventoryAmount();
        AppendLog("실패: 인벤토리 수량 입력을 확인하세요.");
        return Task.CompletedTask;
    }

    private Task InvalidKingdomInputAsync()
    {
        ValidateKingdomAmount();
        AppendLog("실패: 왕국 자원 수량 입력을 확인하세요.");
        return Task.CompletedTask;
    }

    private static bool TryParseResourceAmount(string input, out int amount) =>
        int.TryParse(input, out amount) && amount is >= 0 and <= 1_000_000_000;

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged(string? propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
