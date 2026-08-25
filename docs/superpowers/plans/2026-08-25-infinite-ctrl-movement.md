# Infinite Ctrl Movement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a reversible local-player Ctrl dash/run unlimited toggle using `100x` stamina recovery and a satisfied dash cooldown timer.

**Architecture:** A pure `InfiniteCtrlMovementState` owns enable/disable recovery-factor restoration and dash-timer readiness. `RuntimeHost` routes the trainer command and a narrow Harmony postfix applies the timer result only to the local `PlayerMovement`; protocol and WPF layers report and control the authoritative Helper state.

**Tech Stack:** C# 13/.NET 10 tests, C#/.NET 6 BepInEx IL2CPP Helper, Harmony, WPF, xUnit, PowerShell build tooling

**Spec:** `docs/superpowers/specs/2026-08-25-infinite-ctrl-movement-design.md`

## Global Constraints

- Affect only the local `PlayerMovement`; do not patch remote players.
- Preserve normal `CanDash`/sprint restrictions and do not clear `airDashUsed`.
- `100x` is the enabled stamina recovery factor; disabling and reset restore the captured value.
- Correct all user-facing stamina copy from consumption to recovery semantics.
- Build compiled Helper/app changes and install them, but do not create a release ZIP.
- Finish with commits and push the verified result.

---

### Task 1: Pure state and trainer protocol

**Files:**
- Create: `src/VVooOverthrown.Helper.Core/Runtime/InfiniteCtrlMovementState.cs`
- Modify: `src/VVooOverthrown.Helper.Core/Features/TrainerCommands.cs`
- Modify: `src/VVooOverthrown.Helper.Core/Features/TrainerRequestValidator.cs`
- Modify: `src/VVooOverthrown.Helper.Core/Transport/PipeContracts.cs`
- Create: `tests/VVooOverthrown.Helper.Tests/InfiniteCtrlMovementStateTests.cs`
- Modify: `tests/VVooOverthrown.Helper.Tests/TrainerRequestValidatorTests.cs`

**Interfaces:**
- Produces: `InfiniteCtrlMovementState.Enable(float currentFactor) -> float`, `TryDisable(out float restoreFactor) -> bool`, and `ReadyDashTimer(float current, float cooldown) -> float`.
- Produces: `TrainerCommands.InfiniteCtrlMovement` and `PipeResponse.InfiniteCtrlMovementEnabled`.

- [x] **Step 1: Write failing state and protocol tests**

```csharp
[Fact]
public void EnableUsesHundredTimesAndDisableRestoresCapturedFactor()
{
    var state = new InfiniteCtrlMovementState();
    Assert.Equal(100f, state.Enable(2f));
    Assert.True(state.TryDisable(out var restored));
    Assert.Equal(2f, restored);
}

[Fact]
public void EnabledStateMakesElapsedDashTimerReadyWithoutReducingIt()
{
    var state = new InfiniteCtrlMovementState();
    state.Enable(1f);
    Assert.Equal(0.8f, state.ReadyDashTimer(0.2f, 0.8f));
    Assert.Equal(1.2f, state.ReadyDashTimer(1.2f, 0.8f));
}

[Fact]
public void RepeatedEnablePreservesFirstRestoreFactor()
{
    var state = new InfiniteCtrlMovementState();
    state.Enable(2f);
    state.Enable(100f);
    Assert.True(state.TryDisable(out var restored));
    Assert.Equal(2f, restored);
}
```

Add `TrainerCommands.InfiniteCtrlMovement` to the mutation-classification theory and validate a request using its `Enabled` field.

- [x] **Step 2: Run the focused tests and verify RED**

Run: `dotnet test tests/VVooOverthrown.Helper.Tests/VVooOverthrown.Helper.Tests.csproj --filter "InfiniteCtrlMovementStateTests|TrainerRequestValidatorTests"`

Expected: compilation/test failure because the state, command, and response field do not exist.

- [x] **Step 3: Implement the minimum state and protocol**

```csharp
public sealed class InfiniteCtrlMovementState
{
    public const float RecoveryFactor = 100f;
    private float _restoreFactor;
    public bool Enabled { get; private set; }

    public float Enable(float currentFactor)
    {
        if (!Enabled) _restoreFactor = currentFactor;
        Enabled = true;
        return RecoveryFactor;
    }

    public bool TryDisable(out float restoreFactor)
    {
        restoreFactor = _restoreFactor;
        if (!Enabled) return false;
        Enabled = false;
        return true;
    }

    public float ReadyDashTimer(float current, float cooldown) =>
        Enabled ? Math.Max(current, cooldown) : current;
}
```

Register the toggle command as a supported mutation and add the boolean response property.

- [x] **Step 4: Run the focused tests and verify GREEN**

Run the Step 2 command. Expected: all selected tests pass with zero warnings/errors.

- [x] **Step 5: Commit**

```powershell
git add src/VVooOverthrown.Helper.Core tests/VVooOverthrown.Helper.Tests
git commit -m "feat: add infinite ctrl movement protocol"
```

### Task 2: Local runtime application and restoration

**Files:**
- Create: `src/VVooOverthrown.Helper/Runtime/InfiniteCtrlMovementPatch.cs`
- Modify: `src/VVooOverthrown.Helper/Runtime/RuntimeHost.cs`
- Modify: `tests/VVooOverthrown.Helper.Tests/InfiniteCtrlMovementStateTests.cs`

**Interfaces:**
- Consumes: Task 1 `InfiniteCtrlMovementState` and protocol symbols.
- Produces: capability `movement.infiniteCtrl`; RuntimeHost enable/disable/reset behavior; Harmony postfix for `PlayerMovement.UpdateTimers`.

- [x] **Step 1: Add a failing remote-player isolation test**

```csharp
[Fact]
public void RemotePlayerDashTimerIsNeverChanged()
{
    var state = new InfiniteCtrlMovementState();
    state.Enable(1f);
    Assert.Equal(0.2f, state.ReadyDashTimer(0.2f, 0.8f, isLocalPlayer: false));
}
```

- [x] **Step 2: Run the state tests and verify RED**

Run: `dotnet test tests/VVooOverthrown.Helper.Tests/VVooOverthrown.Helper.Tests.csproj --filter InfiniteCtrlMovementStateTests`

Expected: compilation fails because the local-player-aware overload does not exist.

- [x] **Step 3: Implement RuntimeHost and Harmony wiring**

Extend `ReadyDashTimer` with `bool isLocalPlayer` and return the current timer for remote players. Create a postfix for `PlayerMovement.UpdateTimers` that assigns:

```csharp
__instance.timeSinceDash = State.ReadyDashTimer(
    __instance.timeSinceDash,
    __instance.dashCooldown,
    __instance.isLocalPlayer);
```

In `RuntimeHost`, require loaded movement and difficulty objects before enabling; set `NetworkplayerStaminaFactor` from `state.Enable(current)`, apply the timer once immediately, report status, and restore through `TryDisable` during explicit off/manual stamina/reset/destruction.

- [x] **Step 4: Build the Helper and run focused tests**

Run: `dotnet test tests/VVooOverthrown.Helper.Tests/VVooOverthrown.Helper.Tests.csproj --filter InfiniteCtrlMovementStateTests`

Run: `dotnet build src/VVooOverthrown.Helper/VVooOverthrown.Helper.csproj --no-restore /p:BepInExCoreDir=Z:\Work\WorkAI\VVooOverthrown\.artifacts\bepinex\BepInEx\core`

Expected: selected tests pass and Helper builds with zero errors.

- [x] **Step 5: Commit**

```powershell
git add src/VVooOverthrown.Helper tests/VVooOverthrown.Helper.Tests
git commit -m "feat: remove local ctrl movement cooldown"
```

### Task 3: WPF controls, corrected copy, and end-to-end verification

**Files:**
- Modify: `src/VVooOverthrown.App/ViewModels/MainViewModel.cs`
- Modify: `src/VVooOverthrown.App/MainWindow.xaml`
- Modify: `src/VVooOverthrown.App/MainWindow.xaml.cs`
- Modify: `tests/VVooOverthrown.App.Tests/MainViewModelTests.cs`
- Modify: `docs/user-guide.md`
- Modify: `docs/superpowers/specs/2026-08-25-expanded-trainer-test-mode-design.md`

**Interfaces:**
- Consumes: Task 1 response flag/command and Task 2 authoritative Helper behavior.
- Produces: `MainViewModel.InfiniteCtrlMovementEnabled` and `SetInfiniteCtrlMovementAsync(bool enabled)`.

- [ ] **Step 1: Write the failing ViewModel command/response test**

```csharp
[Fact]
public async Task InfiniteCtrlMovementSendsToggleAndUsesHelperResponse()
{
    var service = new FakeApplicationService(new ApplicationSnapshot(
        @"W:\Games\Overthrown", true, true, true, true, true))
    {
        ConnectResponse = new PipeResponse
        {
            Ok = true,
            TestModeEnabled = true,
            InfiniteCtrlMovementEnabled = true,
        },
    };
    var viewModel = new MainViewModel(service);
    await viewModel.SetInfiniteCtrlMovementAsync(true);
    Assert.Equal(TrainerCommands.InfiniteCtrlMovement, service.LastRequest!.Command);
    Assert.True(service.LastRequest.Enabled);
    Assert.True(viewModel.InfiniteCtrlMovementEnabled);
}
```

Use the existing complete fake service shape rather than mocking ViewModel behavior.

- [ ] **Step 2: Run the focused App test and verify RED**

Run: `dotnet test tests/VVooOverthrown.App.Tests/VVooOverthrown.App.Tests.csproj --filter InfiniteCtrlMovementSendsToggleAndUsesHelperResponse`

Expected: compilation failure because the ViewModel property/method do not exist.

- [ ] **Step 3: Implement WPF UI and copy corrections**

Add the ViewModel method/property, response assignment, XAML on/off buttons and status indicator, and click handlers. Change copy to `기력 회복 배율`, explain `0x`, `1x`, and `100x`, and include Ctrl movement in reset/help text and user documentation.

- [ ] **Step 4: Run App tests and full verification**

Run: `dotnet test tests/VVooOverthrown.App.Tests/VVooOverthrown.App.Tests.csproj`

Run: `dotnet test VVooOverthrown.slnx`

Run: `powershell -ExecutionPolicy Bypass -File tools/build.ps1`

Expected: all tests pass, compiled app/Helper artifacts build, and no release ZIP is created.

- [ ] **Step 5: Install and inspect the live Helper**

Run: `& .\.artifacts\publish\app\VVooOverthrown.exe --install --game 'W:\Games\Overthrown'`

Launch the app and supported game if they are not running, connect the app, and inspect `W:\Games\Overthrown\BepInEx\LogOutput.log` for one loaded Helper and no error entries. Leave actual in-world Ctrl behavior for the user's save-world test if no player is loaded.

- [ ] **Step 6: Commit**

```powershell
git add src/VVooOverthrown.App tests/VVooOverthrown.App.Tests docs
git commit -m "feat: expose infinite ctrl movement toggle"
```
