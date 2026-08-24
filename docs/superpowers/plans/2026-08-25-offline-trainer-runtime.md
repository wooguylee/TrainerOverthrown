# Offline Trainer Runtime Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Connect the WPF app to an IL2CPP Helper and expose at least one verified player feature that is automatically disabled outside a provably local session.

**Architecture:** BepInEx creates interop assemblies for the pinned game build. A small Helper hosts a user-scoped named pipe, queues commands onto Unity's main thread, evaluates an offline-session guard before every mutation, and advertises only adapters whose runtime dependencies resolve.

**Tech Stack:** BepInEx 6 IL2CPP pinned build, Il2CppInterop, HarmonyX, Mirror reflection, named pipes, C# 14/.NET 6 Helper plus .NET 10 WPF app, xUnit

**Spec:** `docs/superpowers/specs/2026-08-25-vvoo-overthrown-design.md`

## Global Constraints

- All mutation paths fail closed unless the supported build and local-only session are both proven.
- Never expose arbitrary addresses, arbitrary method calls, or multiplayer mutation.
- Every Unity object access occurs on Unity's main thread.
- Each feature checks only its own required game objects.
- Reset active changes when a remote participant appears, the pipe disconnects, or the Helper unloads.

---

## File map

- `tools/fetch-bepinex.ps1`: pinned official BepInEx archive download and SHA verification.
- `tools/stage-helper.ps1`: copies only compiled owned payload files.
- `docs/analysis/current-build-api-map.md`: evidence for types, fields, methods, and multiplayer predicates.
- `src/VVooOverthrown.Helper/Runtime`: plugin lifecycle and main-thread dispatcher.
- `src/VVooOverthrown.Helper/Safety`: build and session guards.
- `src/VVooOverthrown.Helper/Transport`: pipe server and command router.
- `src/VVooOverthrown.Helper/Features`: capability adapters.
- `src/VVooOverthrown.Core/Transport`: external pipe client.
- `src/VVooOverthrown.App/Features`: capability-driven controls.

### Task 1: Pin BepInEx and map the current game API

**Files:**
- Create: `tools/fetch-bepinex.ps1`
- Create: `tools/stage-helper.ps1`
- Create: `tools/tool-manifest.json`
- Create: `docs/analysis/current-build-api-map.md`
- Create: `src/VVooOverthrown.Helper/VVooOverthrown.Helper.csproj`

**Interfaces:**
- Produces: `.artifacts/bepinex` verified loader payload.
- Produces: `.artifacts/game-interop` generated assemblies for the current three-file build fingerprint.
- Produces: an evidence table mapping each planned capability to exact runtime members or `unsupported`.

- [x] **Step 1: Implement pinned download verification before extraction**

The script downloads to a unique temporary file, verifies the recorded SHA-256, extracts to a unique staging directory, validates required BepInEx files, and atomically moves the result to `.artifacts\bepinex`.

- [x] **Step 2: Install only the loader payload and perform the compatibility launch**

Create a save backup, record installed loader files in the same owned manifest contract, launch `Overthrown.exe`, and wait up to five minutes for BepInEx `LogOutput.log` and generated interop assemblies. Stop on a crash or loader error and remove only owned loader files.

- [x] **Step 3: Record static and generated-interop evidence**

Search generated assemblies for player vitals, stamina, movement, inventory, time, build/research, and Mirror connection APIs. For every candidate record assembly, fully qualified type, member signature, read/write authority, initialization requirement, and multiplayer relevance.

- [x] **Step 4: Create the minimal Helper project and compile it**

```csharp
[BepInPlugin("local.vvoooverthrown.helper", "VVooOverthrown Helper", "0.1.0")]
public sealed class Plugin : BasePlugin
{
    public override void Load() => Log.LogInfo("VVooOverthrown helper loaded");
}
```

Run: `tools\build.ps1 -GameDir 'W:\Games\Overthrown' -HelperOnly`

Expected: zero compiler errors and the DLL is staged under the owned plugin folder.

- [x] **Step 5: Commit**

Commit: `git commit -m "build: pin IL2CPP loader and map game API"`

### Task 2: Offline guard, pipe transport, and handshake

**Files:**
- Create: `src/VVooOverthrown.Helper/Safety/OfflineSessionGuard.cs`
- Create: `src/VVooOverthrown.Helper/Runtime/MainThreadDispatcher.cs`
- Create: `src/VVooOverthrown.Helper/Transport/HelperPipeServer.cs`
- Create: `src/VVooOverthrown.Helper/Transport/CommandRouter.cs`
- Create: `src/VVooOverthrown.Core/Transport/TrainerPipeClient.cs`
- Test: `tests/VVooOverthrown.Helper.Tests/OfflineSessionGuardTests.cs`
- Test: `tests/VVooOverthrown.Core.Tests/TrainerPipeClientTests.cs`

**Interfaces:**
- Produces: `OfflineSessionGuard.Evaluate(SessionSnapshot)` returning `Allowed`, `RemoteParticipant`, or `Uncertain`.
- Produces: `HelperPipeServer` at `VVooOverthrown.<current-user-sid>.<pid>`.
- Produces: `TrainerPipeClient.ConnectAsync(pid, cancellationToken)` and `SendAsync(CommandRequest, cancellationToken)`.

- [x] **Step 1: Write failing guard matrix tests**

```csharp
[Theory]
[InlineData(1, true, false, SessionDecision.Allowed)]
[InlineData(2, true, true, SessionDecision.RemoteParticipant)]
[InlineData(-1, false, false, SessionDecision.Uncertain)]
public void SessionDecisionFailsClosed(int connections, bool authoritative, bool remote, SessionDecision expected)
{
    Assert.Equal(expected, guard.Evaluate(new(connections, authoritative, remote)));
}
```

- [x] **Step 2: Run focused tests and verify RED**

Run: `.\.tools\dotnet-sdk\dotnet.exe test tests\VVooOverthrown.Helper.Tests\VVooOverthrown.Helper.Tests.csproj --filter OfflineSessionGuardTests`

- [x] **Step 3: Implement guard, current-user pipe ACL, framing, and main-thread queue**

Queue a bounded number of commands, apply per-request timeouts, clear mutable state on disconnect, and never include save contents, usernames, lobby IDs, or translated text in diagnostics.

- [x] **Step 4: Run transport and guard tests**

Run: `.\.tools\dotnet-sdk\dotnet.exe test tests\VVooOverthrown.Helper.Tests\VVooOverthrown.Helper.Tests.csproj`

Run: `.\.tools\dotnet-sdk\dotnet.exe test tests\VVooOverthrown.Core.Tests\VVooOverthrown.Core.Tests.csproj --filter TrainerPipeClientTests`

- [x] **Step 5: Commit**

Commit: `git commit -m "feat: add offline-only helper transport"`

### Task 3: Capability adapters and WPF controls

**Files:**
- Create: `src/VVooOverthrown.Helper/Features/IFeatureAdapter.cs`
- Create: `src/VVooOverthrown.Helper/Features/FeatureRegistry.cs`
- Create: `src/VVooOverthrown.Helper/Features/PlayerVitalsAdapter.cs`
- Create only when proven by the API map: `src/VVooOverthrown.Helper/Features/PlayerMovementAdapter.cs`
- Create: `src/VVooOverthrown.App/Features/FeatureDefinition.cs`
- Modify: `src/VVooOverthrown.App/MainWindow.xaml`
- Modify: `src/VVooOverthrown.App/ViewModels/MainViewModel.cs`
- Test: `tests/VVooOverthrown.Helper.Tests/PlayerVitalsAdapterTests.cs`

**Interfaces:**
- Produces: `IFeatureAdapter.Capability`, `TryResolve()`, `Query()`, `Execute(command)`, and `Reset()`.
- Consumes: `OfflineSessionGuard` before every `Set` or `Toggle` command.

- [x] **Step 1: Write failing per-feature dependency and safety tests**

```csharp
[Fact]
public void SetIsRejectedWhenRemoteParticipantExists()
{
    var response = adapter.Execute(SetValue(100), SessionDecision.RemoteParticipant);
    Assert.Equal("MULTIPLAYER_BLOCKED", response.Error?.Code);
    Assert.False(runtime.WasMutated);
}
```

- [x] **Step 2: Run focused tests and verify RED**

Run: `.\.tools\dotnet-sdk\dotnet.exe test tests\VVooOverthrown.Helper.Tests\VVooOverthrown.Helper.Tests.csproj --filter PlayerVitalsAdapterTests`

- [x] **Step 3: Implement only members proven in the API map**

Expose unsupported candidates as absent capabilities. Store original values for runtime toggles and restore them from `Reset`, disconnect handling, multiplayer transition handling, and plugin unload.

- [x] **Step 4: Bind capability-driven Korean controls**

Render a control only when its capability appears in handshake. Display `지원 준비 중` in a separate noninteractive list for requested but unresolved functions.

- [x] **Step 5: Run all tests and commit**

Run: `tools\build.ps1 -GameDir 'W:\Games\Overthrown'`

Expected: all tests pass; no unsupported adapter is advertised.

Commit: `git commit -m "feat: add verified offline trainer capabilities"`

### Task 4: Live smoke test, release, and handoff docs

**Files:**
- Create: `docs/user-guide.md`
- Create: `docs/translation-guide.md`
- Modify: `README.md`
- Modify: `docs/project-journal.md`
- Create: `.artifacts/release/manifest.json`

**Interfaces:**
- Consumes all previous build, installer, localization, transport, and feature interfaces.
- Produces: `.artifacts/release/VVooOverthrown-win-x64.zip` and `.sha256`.

- [x] **Step 1: Run clean automated verification**

Run: `tools\build.ps1 -GameDir 'W:\Games\Overthrown' -Configuration Release`

Expected: Protocol, Core, LocalizationTool, Helper, and App tests all pass with zero build errors.

- [x] **Step 2: Package and inspect the release**

Run: `tools\package.ps1 -GameDir 'W:\Games\Overthrown'`

Reopen the ZIP, verify every manifest hash, verify `VVooOverthrown.exe`, Helper, translation JSON, BepInEx payload, guides, and build profile exist.

- [x] **Step 3: Perform the live smoke test**

With the game closed, create a fresh backup, install the release payload, launch the game, wait for
Helper handshake, confirm representative main-menu translations, query/reset capabilities, and confirm
the uncertain-session guard blocks mutation. The allowed local-world mutation remains a first-play check;
its guard and adapter paths are covered by unit tests. Close the app and game normally.

- [x] **Step 4: Record exact evidence and final limitations**

Append commands, pass counts, hashes, observed translated text, successful capability, remaining unsupported capabilities, installed-file state, and stopped-process confirmation to `docs/project-journal.md`.

- [x] **Step 5: Commit**

Commit: `git commit -m "release: package VVooOverthrown MVP"`
