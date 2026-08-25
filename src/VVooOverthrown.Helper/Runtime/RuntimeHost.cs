using System.Collections.Concurrent;
using System.Diagnostics;
using BepInEx.Logging;
using Il2CppInterop.Runtime.Attributes;
using Mirror;
using UnityEngine;
using VVooOverthrown.Helper.Features;
using VVooOverthrown.Helper.Localization;
using VVooOverthrown.Helper.Safety;
using VVooOverthrown.Helper.Transport;

namespace VVooOverthrown.Helper.Runtime;

public sealed class RuntimeHost : MonoBehaviour
{
    private static readonly string[] Capabilities =
    {
        "player.godMode",
        "player.health",
        "player.staminaFactor",
        "movement.speedMultiplier",
        "world.timeScale",
        "inventory.resource",
        "kingdom.resource",
        "diagnostics.session",
    };

    private readonly ConcurrentQueue<PendingCommand> _commands = new();
    private readonly OfflineSessionGuard _guard = new();
    private readonly OriginalValueLatch<bool> _originalInvulnerability = new();
    private readonly TargetValueLatch<DifficultyManager, float> _staminaFactorLatch = new();
    private HelperPipeServer _server;
    private ManualLogSource _log;
    private bool _godModeEnabled;
    private bool _timeScaleChanged;
    private float _originalTimeScale = 1f;
    private int _disconnectResetRequested;
    private Damageable _godModeTarget;
    private StringTableLocalizationBootstrap _stringTableLocalization;
    private float _nextGodModeTargetLookupTime;

    public RuntimeHost(IntPtr pointer) : base(pointer)
    {
    }

    [HideFromIl2Cpp]
    public void Initialize(ManualLogSource log, TranslationCatalog catalog)
    {
        _log = log;
        if (catalog is not null)
        {
            _stringTableLocalization = new StringTableLocalizationBootstrap(catalog);
        }

        var pipeName = "VVooOverthrown." + Process.GetCurrentProcess().Id;
        _server = new HelperPipeServer(pipeName, EnqueueAsync, RequestDisconnectResetAsync);
        _server.Start();
        _log.LogInfo("Trainer pipe ready: " + pipeName);
    }

    [HideFromIl2Cpp]
    private Task<PipeResponse> EnqueueAsync(PipeRequest request)
    {
        if (_commands.Count >= 64)
        {
            return Task.FromResult(SimpleError("QUEUE_FULL", "명령 대기열이 가득 찼습니다."));
        }

        var completion = new TaskCompletionSource<PipeResponse>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        _commands.Enqueue(new PendingCommand(request, completion));
        return completion.Task;
    }

    public void Update()
    {
        if (Interlocked.Exchange(ref _disconnectResetRequested, 0) != 0)
        {
            ResetTransientChanges();
        }

        for (var processed = 0; processed < 16 && _commands.TryDequeue(out var command); processed++)
        {
            try
            {
                command.Completion.TrySetResult(Execute(command.Request));
            }
            catch (Exception exception)
            {
                _log.LogError("Trainer command failed: " + exception.GetType().Name);
                command.Completion.TrySetResult(SimpleError("COMMAND_FAILED", "명령을 적용하지 못했습니다."));
            }
        }

        ApplyStringTableLocalization();
        MaintainGodMode();
    }

    [HideFromIl2Cpp]
    private void ApplyStringTableLocalization()
    {
        var localization = _stringTableLocalization;
        if (localization is null || !localization.TryAdvance(out var result))
        {
            return;
        }

        _stringTableLocalization = null;
        if (result.Success)
        {
            _log.LogInfo(
                "String table localization applied once; " +
                $"replacements={result.Replacements}, " +
                $"alreadyLocalized={result.AlreadyLocalized}, " +
                $"missing={result.Missing}, " +
                $"tables={result.MatchedTables}");
        }
        else
        {
            _log.LogWarning("String table localization unavailable: " + result.Failure);
        }
    }

    [HideFromIl2Cpp]
    private Task RequestDisconnectResetAsync()
    {
        Interlocked.Exchange(ref _disconnectResetRequested, 1);
        return Task.CompletedTask;
    }

    [HideFromIl2Cpp]
    private PipeResponse Execute(PipeRequest request)
    {
        var snapshot = CaptureSessionSnapshot();
        var validation = TrainerRequestValidator.Validate(request);
        if (!validation.IsValid)
        {
            return Error(validation.ErrorCode, validation.Message, snapshot);
        }

        if (Is(request.Command, TrainerCommands.Status))
        {
            return Status(snapshot);
        }

        if (Is(request.Command, TrainerCommands.Reset))
        {
            ResetTransientChanges();
            return Status(snapshot);
        }

        if (Is(request.Command, TrainerCommands.GodMode))
        {
            if (request.Enabled && !TryEnableGodMode())
            {
                return FeatureUnavailable("로컬 플레이어 체력이 아직 준비되지 않았습니다.", snapshot);
            }

            _godModeEnabled = request.Enabled;
            if (!_godModeEnabled)
            {
                RestoreInvulnerability();
            }
            return Status(snapshot);
        }

        if (Is(request.Command, TrainerCommands.Heal))
        {
            var health = FindLocalPlayerHealth();
            if (health?.asDamageable == null)
            {
                return FeatureUnavailable("로컬 플레이어 체력이 아직 준비되지 않았습니다.", snapshot);
            }
            health.asDamageable.currentHealth = health.asDamageable.effectiveMaxHealth;
            return Status(snapshot);
        }

        if (Is(request.Command, TrainerCommands.StaminaFactor))
        {
            if (!DifficultyManager.HasInstance || DifficultyManager.Instance == null)
            {
                return FeatureUnavailable("난이도/기력 관리자가 아직 준비되지 않았습니다.", snapshot);
            }

            var target = DifficultyManager.Instance;
            _staminaFactorLatch.Capture(target, target.NetworkplayerStaminaFactor);
            target.NetworkplayerStaminaFactor = request.Value;
            return Status(snapshot);
        }

        if (Is(request.Command, TrainerCommands.MovementSpeed))
        {
            var movement = FindLocalPlayerMovement();
            if (movement == null)
            {
                return FeatureUnavailable("로컬 플레이어 이동 객체가 아직 준비되지 않았습니다.", snapshot);
            }

            MovementSpeedPatch.Multiplier = request.Value;
            movement.UpdateSpeedFactor();
            return Status(snapshot);
        }

        if (Is(request.Command, TrainerCommands.TimeScale))
        {
            if (!_timeScaleChanged)
            {
                _originalTimeScale = GameTime.gameTimeScale;
                _timeScaleChanged = true;
            }
            GameTime.gameTimeScale = request.Value;
            return Status(snapshot);
        }

        if (request.Command.StartsWith("inventory", StringComparison.OrdinalIgnoreCase))
        {
            return ExecuteInventory(request, snapshot);
        }

        if (request.Command.StartsWith("kingdom", StringComparison.OrdinalIgnoreCase))
        {
            return ExecuteKingdom(request, snapshot);
        }

        return Error("UNKNOWN_COMMAND", "지원하지 않는 명령입니다.", snapshot);
    }

    [HideFromIl2Cpp]
    private PipeResponse ExecuteInventory(PipeRequest request, SessionSnapshot snapshot)
    {
        var inventory = FindLocalPlayerInventory();
        if (inventory == null)
        {
            return FeatureUnavailable("로컬 플레이어 인벤토리가 아직 준비되지 않았습니다.", snapshot);
        }

        var resource = (ResourceType)request.ResourceType;
        inventory.GetStoredAmount(resource, out var currentAmount);
        if (!Is(request.Command, TrainerCommands.InventoryQuery))
        {
            var targetAmount = request.Amount;
            if (Is(request.Command, TrainerCommands.InventoryAdd) &&
                !TryAddAmount(currentAmount, request.Amount, out targetAmount))
            {
                return Error("OUT_OF_RANGE", "변경 후 인벤토리 수량이 지원 범위를 벗어납니다.", snapshot);
            }

            if (targetAmount > currentAmount)
            {
                var resourceData = GlobalResourceStorage.GetResourceData(resource);
                if (resourceData == null || (int)resourceData.DefaultItem == 0)
                {
                    return Error("RESOURCE_ITEM_UNAVAILABLE", "선택한 자원은 인벤토리 기본 아이템이 없습니다.", snapshot);
                }
                inventory.DepositInternal(resourceData.DefaultItem, targetAmount - currentAmount);
            }
            else if (targetAmount < currentAmount)
            {
                inventory.RemoveAmountFromStacks(resource, currentAmount - targetAmount);
            }

            inventory.GetStoredAmount(resource, out currentAmount);
            var verification = ResourceMutationVerifier.Verify(targetAmount, currentAmount);
            if (!verification.IsExact)
            {
                var error = Error(verification.ErrorCode, verification.Message, snapshot);
                error.SelectedResourceType = request.ResourceType;
                error.InventoryAmount = currentAmount;
                return error;
            }
        }

        var response = Status(snapshot);
        response.SelectedResourceType = request.ResourceType;
        response.InventoryAmount = currentAmount;
        return response;
    }

    [HideFromIl2Cpp]
    private PipeResponse ExecuteKingdom(PipeRequest request, SessionSnapshot snapshot)
    {
        if (!GlobalResourceStorage.HasInstance || GlobalResourceStorage.Instance == null)
        {
            return FeatureUnavailable("왕국 자원 저장소가 아직 준비되지 않았습니다.", snapshot);
        }

        var resource = (ResourceType)request.ResourceType;
        var currentAmount = GlobalResourceStorage.GetStoredAmount(resource);
        if (!Is(request.Command, TrainerCommands.KingdomQuery))
        {
            var targetAmount = request.Amount;
            if (Is(request.Command, TrainerCommands.KingdomAdd) &&
                !TryAddAmount(currentAmount, request.Amount, out targetAmount))
            {
                return Error("OUT_OF_RANGE", "변경 후 왕국 자원 수량이 지원 범위를 벗어납니다.", snapshot);
            }

            if (targetAmount > currentAmount)
            {
                GlobalResourceStorage.Deposit(resource, targetAmount - currentAmount);
            }
            else if (targetAmount < currentAmount)
            {
                GlobalResourceStorage.Withdraw(resource, currentAmount - targetAmount);
            }
            currentAmount = GlobalResourceStorage.GetStoredAmount(resource);
            var verification = ResourceMutationVerifier.Verify(targetAmount, currentAmount);
            if (!verification.IsExact)
            {
                var error = Error(verification.ErrorCode, verification.Message, snapshot);
                error.SelectedResourceType = request.ResourceType;
                error.KingdomAmount = currentAmount;
                return error;
            }
        }

        var response = Status(snapshot);
        response.SelectedResourceType = request.ResourceType;
        response.KingdomAmount = currentAmount;
        return response;
    }

    [HideFromIl2Cpp]
    private SessionSnapshot CaptureSessionSnapshot()
    {
        var count = NetworkServer.connections == null ? -1 : NetworkServer.connections.Count;
        return new SessionSnapshot(
            BNetworkManager.OfflineMode,
            NetworkServer.activeHost && NetworkClient.active,
            count,
            count > 1);
    }

    [HideFromIl2Cpp]
    private bool TryEnableGodMode()
    {
        if (_godModeTarget != null)
        {
            return true;
        }

        var health = FindLocalPlayerHealth();
        if (health?.asDamageable == null)
        {
            return false;
        }

        _godModeTarget = health.asDamageable;
        _originalInvulnerability.Capture(_godModeTarget.isInvulnerable);
        _godModeTarget.isInvulnerable = true;
        _godModeTarget.currentHealth = _godModeTarget.effectiveMaxHealth;
        return true;
    }

    [HideFromIl2Cpp]
    private void MaintainGodMode()
    {
        if (!_godModeEnabled)
        {
            return;
        }

        if (_godModeTarget == null)
        {
            _originalInvulnerability.TryTake(out _);
            var now = Time.unscaledTime;
            if (now < _nextGodModeTargetLookupTime)
            {
                return;
            }
            _nextGodModeTargetLookupTime = now + 1f;
            TryEnableGodMode();
            return;
        }

        _godModeTarget.isInvulnerable = true;
        _godModeTarget.currentHealth = _godModeTarget.effectiveMaxHealth;
    }

    [HideFromIl2Cpp]
    private static PlayerHealth FindLocalPlayerHealth()
    {
        var players = UnityEngine.Object.FindObjectsOfType<PlayerHealth>();
        for (var index = 0; index < players.Length; index++)
        {
            var player = players[index];
            if (player != null && player.isLocalPlayer)
            {
                return player;
            }
        }
        return null;
    }

    [HideFromIl2Cpp]
    private static PlayerMovement FindLocalPlayerMovement()
    {
        var players = UnityEngine.Object.FindObjectsOfType<PlayerMovement>();
        for (var index = 0; index < players.Length; index++)
        {
            var player = players[index];
            if (player != null && player.isLocalPlayer)
            {
                return player;
            }
        }
        return null;
    }

    [HideFromIl2Cpp]
    private static PlayerInventory FindLocalPlayerInventory()
    {
        var inventories = UnityEngine.Object.FindObjectsOfType<PlayerInventory>();
        for (var index = 0; index < inventories.Length; index++)
        {
            var inventory = inventories[index];
            if (inventory != null && inventory.isLocalPlayer)
            {
                return inventory;
            }
        }
        return null;
    }

    [HideFromIl2Cpp]
    private void RestoreInvulnerability()
    {
        var hadOriginal = _originalInvulnerability.TryTake(out var original);
        if (_godModeTarget != null && hadOriginal)
        {
            _godModeTarget.isInvulnerable = original;
        }
        _godModeTarget = null;
        _nextGodModeTargetLookupTime = 0f;
    }

    [HideFromIl2Cpp]
    private void ResetTransientChanges()
    {
        _godModeEnabled = false;
        RestoreInvulnerability();

        if (_staminaFactorLatch.TryTake(out var staminaTarget, out var staminaFactor) &&
            staminaTarget != null)
        {
            staminaTarget.NetworkplayerStaminaFactor = staminaFactor;
        }

        MovementSpeedPatch.Multiplier = 1f;
        var movement = FindLocalPlayerMovement();
        if (movement != null)
        {
            movement.UpdateSpeedFactor();
        }

        if (_timeScaleChanged)
        {
            GameTime.gameTimeScale = _originalTimeScale;
            _timeScaleChanged = false;
        }
    }

    [HideFromIl2Cpp]
    private PipeResponse Status(SessionSnapshot snapshot)
    {
        var health = FindLocalPlayerHealth();
        var inventory = FindLocalPlayerInventory();
        return new PipeResponse
        {
            Ok = true,
            TestModeEnabled = true,
            SessionDecision = _guard.Evaluate(snapshot).ToString(),
            OfflineMode = snapshot.OfflineMode,
            AuthoritativeHost = snapshot.AuthoritativeHost,
            ConnectionCount = snapshot.ConnectionCount,
            RemoteParticipant = snapshot.RemoteParticipantDetected,
            Capabilities = Capabilities,
            PlayerReady = health?.asDamageable != null,
            InventoryReady = inventory != null,
            KingdomStorageReady = GlobalResourceStorage.HasInstance,
            GodModeEnabled = _godModeEnabled,
            TimeScale = GameTime.gameTimeScale,
            StaminaFactor = ReadStaminaFactor(),
            MovementSpeedMultiplier = MovementSpeedPatch.Multiplier,
        };
    }

    [HideFromIl2Cpp]
    private static float ReadStaminaFactor()
    {
        return DifficultyManager.HasInstance && DifficultyManager.Instance != null
            ? DifficultyManager.Instance.NetworkplayerStaminaFactor
            : 1f;
    }

    [HideFromIl2Cpp]
    private PipeResponse FeatureUnavailable(string message, SessionSnapshot snapshot) =>
        Error("FEATURE_UNAVAILABLE", message, snapshot);

    [HideFromIl2Cpp]
    private PipeResponse Error(string code, string message, SessionSnapshot snapshot)
    {
        var response = Status(snapshot);
        response.Ok = false;
        response.ErrorCode = code;
        response.Message = message;
        return response;
    }

    [HideFromIl2Cpp]
    private static PipeResponse SimpleError(string code, string message) => new()
    {
        Ok = false,
        TestModeEnabled = true,
        ErrorCode = code,
        Message = message,
        Capabilities = Capabilities,
    };

    [HideFromIl2Cpp]
    private static bool TryAddAmount(int current, int delta, out int result)
    {
        var candidate = (long)current + delta;
        if (candidate is < 0 or > 1_000_000_000)
        {
            result = current;
            return false;
        }
        result = (int)candidate;
        return true;
    }

    [HideFromIl2Cpp]
    private static bool Is(string actual, string expected) =>
        actual.Equals(expected, StringComparison.OrdinalIgnoreCase);

    public void OnDestroy()
    {
        ResetTransientChanges();
        _server?.Dispose();
    }
}
