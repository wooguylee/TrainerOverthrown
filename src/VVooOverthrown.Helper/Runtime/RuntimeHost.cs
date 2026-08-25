using System.Collections.Concurrent;
using System.Diagnostics;
using BepInEx.Logging;
using Il2CppInterop.Runtime.Attributes;
using Mirror;
using UnityEngine;
using VVooOverthrown.Helper.Localization;
using VVooOverthrown.Helper.Safety;
using VVooOverthrown.Helper.Transport;

namespace VVooOverthrown.Helper.Runtime;

public sealed class RuntimeHost : MonoBehaviour
{
    private static readonly string[] Capabilities = { "player.godMode", "world.timeScale" };
    private readonly ConcurrentQueue<PendingCommand> _commands = new();
    private readonly OfflineSessionGuard _guard = new();
    private HelperPipeServer _server;
    private ManualLogSource _log;
    private bool _godModeEnabled;
    private bool _timeScaleChanged;
    private float _originalTimeScale = 1f;
    private int _disconnectResetRequested;
    private Damageable _godModeTarget;
    private readonly OriginalValueLatch<bool> _originalInvulnerability = new();
    private RadialMenuDynamicWheel _radialMenuWheel;
    private float _nextRadialMenuTranslationTime;
    private bool _radialMenuTranslationLogged;

    public RuntimeHost(IntPtr pointer) : base(pointer)
    {
    }

    [HideFromIl2Cpp]
    public void Initialize(ManualLogSource log)
    {
        _log = log;
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
            return Task.FromResult(Error("QUEUE_FULL", "명령 대기열이 가득 찼습니다."));
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
            ResetChanges();
        }

        if (ActiveChangeSafety.ShouldReset(
                _godModeEnabled,
                _timeScaleChanged,
                EvaluateSession()))
        {
            ResetChanges();
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
                command.Completion.TrySetResult(Error("COMMAND_FAILED", "명령을 적용하지 못했습니다."));
            }
        }

        MaintainRadialMenuLocalization();
        MaintainGodMode();
    }

    [HideFromIl2Cpp]
    private void MaintainRadialMenuLocalization()
    {
        var now = Time.unscaledTime;
        if (now < _nextRadialMenuTranslationTime)
        {
            return;
        }
        _nextRadialMenuTranslationTime = now + 0.2f;

        try
        {
            if (_radialMenuWheel == null)
            {
                _radialMenuWheel = UnityEngine.Object.FindObjectOfType<RadialMenuDynamicWheel>();
            }

            var replacements = RadialMenuLocalizationMonitor.Translate(_radialMenuWheel);
            if (replacements > 0 && !_radialMenuTranslationLogged)
            {
                _radialMenuTranslationLogged = true;
                _log.LogInfo($"Radial menu localization applied; replacements={replacements}");
            }
        }
        catch
        {
            _radialMenuWheel = null;
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
        var decision = EvaluateSession();
        if (request.Command.Equals("status", StringComparison.OrdinalIgnoreCase))
        {
            return Status(decision);
        }

        if (request.Command.Equals("reset", StringComparison.OrdinalIgnoreCase))
        {
            ResetChanges();
            return Status(decision);
        }

        if (decision != SessionDecision.Allowed)
        {
            ResetChanges();
            return Error(
                decision == SessionDecision.RemoteParticipant ? "MULTIPLAYER_BLOCKED" : "OFFLINE_NOT_PROVEN",
                "검증된 로컬 싱글플레이 세션에서만 사용할 수 있습니다.",
                decision);
        }

        if (request.Command.Equals("godMode", StringComparison.OrdinalIgnoreCase))
        {
            _godModeEnabled = request.Enabled;
            if (!_godModeEnabled)
            {
                RestoreInvulnerability();
            }
            return Status(decision);
        }

        if (request.Command.Equals("timeScale", StringComparison.OrdinalIgnoreCase))
        {
            if (request.Value < 0.25f || request.Value > 4f)
            {
                return Error("OUT_OF_RANGE", "시간 배속은 0.25~4.0 범위여야 합니다.", decision);
            }

            if (!_timeScaleChanged)
            {
                _originalTimeScale = GameTime.gameTimeScale;
                _timeScaleChanged = true;
            }
            GameTime.gameTimeScale = request.Value;
            return Status(decision);
        }

        return Error("UNKNOWN_COMMAND", "지원하지 않는 명령입니다.", decision);
    }

    [HideFromIl2Cpp]
    private SessionDecision EvaluateSession()
    {
        var count = NetworkServer.connections == null ? -1 : NetworkServer.connections.Count;
        var snapshot = new SessionSnapshot(
            BNetworkManager.OfflineMode,
            NetworkServer.activeHost && NetworkClient.active,
            count,
            count > 1);
        return _guard.Evaluate(snapshot);
    }

    [HideFromIl2Cpp]
    private void MaintainGodMode()
    {
        if (!_godModeEnabled)
        {
            return;
        }

        if (EvaluateSession() != SessionDecision.Allowed)
        {
            ResetChanges();
            return;
        }

        var health = FindLocalPlayerHealth();
        if (health == null || health.asDamageable == null)
        {
            return;
        }

        var damageable = health.asDamageable;
        if (_godModeTarget != null && _godModeTarget != damageable)
        {
            RestoreInvulnerability();
        }
        if (_godModeTarget == null)
        {
            _godModeTarget = damageable;
            _originalInvulnerability.Capture(damageable.isInvulnerable);
        }
        damageable.isInvulnerable = true;
        damageable.currentHealth = damageable.effectiveMaxHealth;
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
    private void RestoreInvulnerability()
    {
        var hadOriginal = _originalInvulnerability.TryTake(out var original);
        if (_godModeTarget != null && hadOriginal)
        {
            _godModeTarget.isInvulnerable = original;
        }
        _godModeTarget = null;
    }

    [HideFromIl2Cpp]
    private void ResetChanges()
    {
        _godModeEnabled = false;
        RestoreInvulnerability();
        if (_timeScaleChanged)
        {
            GameTime.gameTimeScale = _originalTimeScale;
            _timeScaleChanged = false;
        }
    }

    [HideFromIl2Cpp]
    private PipeResponse Status(SessionDecision decision) => new()
    {
        Ok = true,
        SessionDecision = decision.ToString(),
        Capabilities = Capabilities,
        GodModeEnabled = _godModeEnabled,
        TimeScale = GameTime.gameTimeScale,
    };

    [HideFromIl2Cpp]
    private static PipeResponse Error(
        string code,
        string message,
        SessionDecision decision = SessionDecision.Uncertain) => new()
    {
        Ok = false,
        ErrorCode = code,
        Message = message,
        SessionDecision = decision.ToString(),
        Capabilities = Capabilities,
    };

    public void OnDestroy()
    {
        ResetChanges();
        _server?.Dispose();
    }
}
