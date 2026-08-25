# Numeric Movement Tuning Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace preset multiplier buttons with validated numeric inputs and add independent `0..1000x` regular-jump, special-movement, and gravity controls.

**Architecture:** A Unity-independent `MovementTuningState` class owns jump grouping and three transient multipliers. Three narrow Harmony patches consume that state, `RuntimeHost` exposes it over the existing pipe contract, and a reusable WPF `MultiplierInputViewModel` supplies consistent input parsing and apply-button state for all six numeric controls.

**Tech Stack:** C# 13/.NET 10 tests, C#/.NET 6 BepInEx IL2CPP Helper, Harmony, WPF, xUnit, PowerShell build tooling

**Spec:** `docs/superpowers/specs/2026-08-25-numeric-movement-tuning-design.md`

## Global Constraints

- Every numeric multiplier accepts finite values from `0` through `1000` inclusive.
- Apply jump and gravity changes only to the local `PlayerMovement`.
- Regular jump types are enum values `0..4`; every other jump type is special movement.
- Do not scale `initialVelocity` in the variable-height prefix; `TriggerJumpInternal` owns initial impulse scaling.
- Reset/disconnect/unload returns new transient multipliers to `1x`.
- Run only focused tests and Release builds for the changed Helper/App projects.
- Do not launch the game, run the full solution suite, or create a release ZIP.
- Finish with commits on `master` and push.

---

### Task 1: Movement tuning state and IPC contract

**Files:**
- Create: `src/VVooOverthrown.Helper.Core/Runtime/MovementTuningState.cs`
- Modify: `src/VVooOverthrown.Helper.Core/Features/TrainerCommands.cs`
- Modify: `src/VVooOverthrown.Helper.Core/Features/TrainerRequestValidator.cs`
- Modify: `src/VVooOverthrown.Helper.Core/Transport/PipeContracts.cs`
- Create: `tests/VVooOverthrown.Helper.Tests/MovementTuningStateTests.cs`
- Modify: `tests/VVooOverthrown.Helper.Tests/TrainerRequestValidatorTests.cs`

**Interfaces:**
- Produces: `MovementTuningState.SetRegularJumpMultiplier(float)`, `SetSpecialMovementMultiplier(float)`, `SetGravityMultiplier(float)`, `ScaleJumpVelocity(float,int,bool)`, `ScaleGravityDelta(float,bool)`, and `Reset()`.
- Produces: commands `RegularJumpMultiplier`, `SpecialMovementMultiplier`, `GravityMultiplier` and matching `PipeResponse` properties.

- [x] **Step 1: Write failing state and validation tests**

```csharp
[Fact]
public void RegularAndSpecialJumpTypesUseIndependentMultipliers()
{
    var state = new MovementTuningState();
    state.SetRegularJumpMultiplier(2f);
    state.SetSpecialMovementMultiplier(3f);

    Assert.Equal(20f, state.ScaleJumpVelocity(10f, jumpType: 0, isLocalPlayer: true));
    Assert.Equal(30f, state.ScaleJumpVelocity(10f, jumpType: 5, isLocalPlayer: true));
    Assert.Equal(10f, state.ScaleJumpVelocity(10f, jumpType: 0, isLocalPlayer: false));
}

[Fact]
public void GravityAndResetAreIndependent()
{
    var state = new MovementTuningState();
    state.SetRegularJumpMultiplier(2f);
    state.SetSpecialMovementMultiplier(3f);
    state.SetGravityMultiplier(4f);

    Assert.Equal(-8f, state.ScaleGravityDelta(-2f, isLocalPlayer: true));
    state.Reset();
    Assert.Equal(1f, state.RegularJumpMultiplier);
    Assert.Equal(1f, state.SpecialMovementMultiplier);
    Assert.Equal(1f, state.GravityMultiplier);
}
```

Add validator theories proving all six numeric commands accept `0`, decimal values, and `1000`, while rejecting negative values, `1000.01`, `NaN`, and infinities.

- [x] **Step 2: Run focused tests and verify RED**

Run:

```powershell
& .\.tools\dotnet-sdk\dotnet.exe test tests\VVooOverthrown.Helper.Tests\VVooOverthrown.Helper.Tests.csproj --filter "MovementTuningStateTests|TrainerRequestValidatorTests"
```

Expected: compilation fails because the new state and commands do not exist.

- [x] **Step 3: Implement the pure state and contract**

```csharp
public sealed class MovementTuningState
{
    public const float DefaultMultiplier = 1f;

    public float RegularJumpMultiplier { get; private set; } = DefaultMultiplier;
    public float SpecialMovementMultiplier { get; private set; } = DefaultMultiplier;
    public float GravityMultiplier { get; private set; } = DefaultMultiplier;

    public void SetRegularJumpMultiplier(float value) => RegularJumpMultiplier = value;
    public void SetSpecialMovementMultiplier(float value) => SpecialMovementMultiplier = value;
    public void SetGravityMultiplier(float value) => GravityMultiplier = value;

    public float ScaleJumpVelocity(float value, int jumpType, bool isLocalPlayer) =>
        isLocalPlayer ? value * (jumpType is >= 0 and <= 4
            ? RegularJumpMultiplier
            : SpecialMovementMultiplier) : value;

    public float ScaleGravityDelta(float value, bool isLocalPlayer) =>
        isLocalPlayer ? value * GravityMultiplier : value;

    public void Reset()
    {
        RegularJumpMultiplier = DefaultMultiplier;
        SpecialMovementMultiplier = DefaultMultiplier;
        GravityMultiplier = DefaultMultiplier;
    }
}
```

Register the three commands as mutations, expand stamina/movement/time validation to `0..1000`, enforce finite values, and add response fields defaulting to `1f`.

- [x] **Step 4: Run the focused tests and verify GREEN**

Run the Step 2 command. Expected: all selected tests pass.

- [x] **Step 5: Commit**

```powershell
git add src\VVooOverthrown.Helper.Core tests\VVooOverthrown.Helper.Tests
git commit -m "feat: add numeric movement tuning contract"
```

### Task 2: Local-player jump and gravity patches

**Files:**
- Create: `src/VVooOverthrown.Helper/Runtime/MovementTuningPatches.cs`
- Modify: `src/VVooOverthrown.Helper/Runtime/RuntimeHost.cs`
- Modify: `tests/VVooOverthrown.Helper.Tests/MovementTuningStateTests.cs`

**Interfaces:**
- Consumes: Task 1 `MovementTuningState` and new protocol symbols.
- Produces: `MovementTuningState.ScaleVariableHeightBonus(float,int,bool)`, `MovementTuningRuntime.State`, Harmony prefixes/postfix, and RuntimeHost command/status/reset wiring.

- [ ] **Step 1: Add a failing variable-bonus isolation test**

```csharp
[Fact]
public void VariableHeightBonusUsesCurrentJumpGroupAndIgnoresRemotePlayers()
{
    var state = new MovementTuningState();
    state.SetRegularJumpMultiplier(2f);
    state.SetSpecialMovementMultiplier(3f);

    Assert.Equal(8f, state.ScaleVariableHeightBonus(4f, jumpType: 0, isLocalPlayer: true));
    Assert.Equal(12f, state.ScaleVariableHeightBonus(4f, jumpType: 5, isLocalPlayer: true));
    Assert.Equal(4f, state.ScaleVariableHeightBonus(4f, jumpType: 5, isLocalPlayer: false));
}
```

- [ ] **Step 2: Run the state tests and verify RED**

Run:

```powershell
& .\.tools\dotnet-sdk\dotnet.exe test tests\VVooOverthrown.Helper.Tests\VVooOverthrown.Helper.Tests.csproj --filter MovementTuningStateTests
```

Expected: compilation fails because `ScaleVariableHeightBonus` does not exist.

- [ ] **Step 3: Implement Harmony and RuntimeHost wiring**

Add `ScaleVariableHeightBonus` as a direct call to `ScaleJumpVelocity`, then create one runtime file containing:

```csharp
internal static class MovementTuningRuntime
{
    public static MovementTuningState State { get; } = new();
}

[HarmonyPatch(typeof(PlayerMovement), nameof(PlayerMovement.TriggerJumpInternal))]
internal static class JumpVelocityPatch
{
    private static void Prefix(PlayerMovement __instance, ref float jumpVelocity) =>
        jumpVelocity = MovementTuningRuntime.State.ScaleJumpVelocity(
            jumpVelocity, (int)__instance.jumpType, __instance.isLocalPlayer);
}

[HarmonyPatch(typeof(PlayerMovement), nameof(PlayerMovement.TriggerVariableHeightJump))]
internal static class VariableHeightJumpPatch
{
    private static void Prefix(PlayerMovement __instance, ref float holdBonusVelocity) =>
        holdBonusVelocity = MovementTuningRuntime.State.ScaleVariableHeightBonus(
            holdBonusVelocity, (int)__instance.jumpType, __instance.isLocalPlayer);
}

[HarmonyPatch(typeof(PlayerMovement), nameof(PlayerMovement.GetBaseGravitySpeedDelta))]
internal static class GravityMultiplierPatch
{
    private static void Postfix(PlayerMovement __instance, ref float __result) =>
        __result = MovementTuningRuntime.State.ScaleGravityDelta(
            __result, __instance.isLocalPlayer);
}
```

Guard null instances in every patch. In `RuntimeHost`, add the three capabilities, require a loaded local movement object before accepting each command, set the requested state value, report all three response values, and call `MovementTuningRuntime.State.Reset()` from `ResetTransientChanges()`.

- [ ] **Step 4: Build the Helper and run focused tests**

Run:

```powershell
& .\.tools\dotnet-sdk\dotnet.exe test tests\VVooOverthrown.Helper.Tests\VVooOverthrown.Helper.Tests.csproj --filter MovementTuningStateTests
& .\.tools\dotnet-sdk\dotnet.exe build src\VVooOverthrown.Helper\VVooOverthrown.Helper.csproj -c Release --no-restore /p:BepInExCoreDir=Z:\Work\WorkAI\VVooOverthrown\.artifacts\bepinex\BepInEx\core
```

Expected: focused tests pass and Helper compiles with zero errors.

- [ ] **Step 5: Commit**

```powershell
git add src\VVooOverthrown.Helper tests\VVooOverthrown.Helper.Tests
git commit -m "feat: tune local jump and gravity multipliers"
```

### Task 3: Numeric WPF inputs and focused delivery

**Files:**
- Create: `src/VVooOverthrown.App/ViewModels/MultiplierInputViewModel.cs`
- Modify: `src/VVooOverthrown.App/ViewModels/MainViewModel.cs`
- Modify: `src/VVooOverthrown.App/MainWindow.xaml`
- Modify: `src/VVooOverthrown.App/MainWindow.xaml.cs`
- Create: `tests/VVooOverthrown.App.Tests/MultiplierInputViewModelTests.cs`
- Modify: `tests/VVooOverthrown.App.Tests/MainViewModelTests.cs`
- Modify: `docs/user-guide.md`
- Modify: `docs/analysis/current-build-api-map.md`

**Interfaces:**
- Consumes: Task 1 commands/response and Task 2 runtime values.
- Produces: reusable `MultiplierInputViewModel`, six input properties, and six apply methods on `MainViewModel`.

- [ ] **Step 1: Write failing input and command tests**

```csharp
[Theory]
[InlineData("0", true, 0f)]
[InlineData("1.25", true, 1.25f)]
[InlineData("1000", true, 1000f)]
[InlineData("-1", false, 0f)]
[InlineData("1000.01", false, 0f)]
[InlineData("NaN", false, 0f)]
public void ParsesOnlySupportedFiniteMultiplierValues(string text, bool valid, float expected)
{
    var input = new MultiplierInputViewModel("1");
    input.Text = text;

    Assert.Equal(valid, input.IsValid);
    Assert.Equal(valid, input.TryGetValue(out var actual));
    if (valid) Assert.Equal(expected, actual);
}
```

Add a `MainViewModelTests` case that sets `RegularJumpInput.Text = "2.5"`, enables the trainer through the fake response, calls `ApplyRegularJumpMultiplierAsync()`, and verifies command/value plus the Helper-reported current message.

- [ ] **Step 2: Run focused App tests and verify RED**

Run:

```powershell
& .\.tools\dotnet-sdk\dotnet.exe test tests\VVooOverthrown.App.Tests\VVooOverthrown.App.Tests.csproj --filter "MultiplierInputViewModelTests|NumericMultiplierInputSendsCommand"
```

Expected: compilation fails because the input ViewModel and apply methods do not exist.

- [ ] **Step 3: Implement reusable input state and MainViewModel commands**

`MultiplierInputViewModel` implements `INotifyPropertyChanged`, parses using current culture then invariant culture, validates `float.IsFinite(value) && value is >= 0f and <= 1000f`, and exposes `Text`, `IsValid`, `Message`, `CanApply`, `TryGetValue(out float)`, and `SetTrainerEnabled(bool)`.

`MainViewModel` exposes initialized properties `StaminaFactorInput`, `MovementSpeedInput`, `TimeScaleInput`, `RegularJumpInput`, `SpecialMovementInput`, and `GravityInput`. Its `CanUseTrainer` setter forwards the enabled state to all inputs. Add apply methods that send the matching command only after `TryGetValue`; add three current-value messages and populate them from every `PipeResponse`.

- [ ] **Step 4: Replace preset buttons and add the three movement cards**

Replace each stamina/movement/time `WrapPanel` of preset buttons with a numeric `TextBox`, `적용` button, input-validation message, and Helper-reported current value. Add matching cards for regular jump, special movement, and gravity. Remove obsolete click handlers from `MainWindow.xaml.cs` and add six apply handlers.

Update reset/help copy and both docs with the `0..1000x` inputs, jump grouping, gravity behavior, and reset semantics.

- [ ] **Step 5: Run minimal focused verification**

Run:

```powershell
& .\.tools\dotnet-sdk\dotnet.exe test tests\VVooOverthrown.App.Tests\VVooOverthrown.App.Tests.csproj --filter "MultiplierInputViewModelTests|NumericMultiplierInputSendsCommand"
& .\.tools\dotnet-sdk\dotnet.exe build src\VVooOverthrown.App\VVooOverthrown.App.csproj -c Release --no-restore
& .\.tools\dotnet-sdk\dotnet.exe build src\VVooOverthrown.Helper\VVooOverthrown.Helper.csproj -c Release --no-restore /p:BepInExCoreDir=Z:\Work\WorkAI\VVooOverthrown\.artifacts\bepinex\BepInEx\core
```

Expected: selected tests pass and both changed deliverables compile with zero errors. Do not run the full solution suite, live game, installer, or packaging.

- [ ] **Step 6: Commit, merge, and push**

```powershell
git add src\VVooOverthrown.App tests\VVooOverthrown.App.Tests docs
git commit -m "feat: add numeric movement multiplier inputs"
git checkout master
git merge --ff-only feat/numeric-movement-tuning
git push origin master
```
