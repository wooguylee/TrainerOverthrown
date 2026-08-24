# Foundation App and Safe Installation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a runnable Korean WPF application that verifies the current Overthrown build and safely installs or removes an owned payload.

**Architecture:** A self-contained WPF app consumes a testable Core library and a small shared Protocol library. Core owns all filesystem and process behavior; the UI only calls explicit services and renders immutable state.

**Tech Stack:** C# 14, .NET 10.0.302, WPF, System.Text.Json, SHA-256, xUnit

**Spec:** `docs/superpowers/specs/2026-08-25-vvoo-overthrown-design.md`

## Global Constraints

- Target only the current Windows x64 build in `W:\Games\Overthrown`.
- Never replace `Overthrown.exe` or `GameAssembly.dll`.
- Refuse mutations when the three-file supported-build fingerprint differs.
- Use a unique staging directory and an owned-file manifest for every installation.
- Never delete a user-modified installed file during removal.
- Keep generated SDKs, packages, analysis output, and releases out of Git.

---

## File map

- `global.json`, `Directory.Build.props`, `VVooOverthrown.slnx`: deterministic solution configuration.
- `tools/bootstrap-dotnet.ps1`, `tools/build.ps1`, `tools/package.ps1`: reproducible Windows workflow.
- `src/VVooOverthrown.Protocol`: versioned messages and pipe framing.
- `src/VVooOverthrown.Core/Build`: game path and hash validation.
- `src/VVooOverthrown.Core/Installation`: payload manifest, transactional install, conservative removal.
- `src/VVooOverthrown.Core/Saves`: LocalLow backup and hash verification.
- `src/VVooOverthrown.App`: Korean WPF shell and status state machine.
- `tests/VVooOverthrown.Protocol.Tests`, `tests/VVooOverthrown.Core.Tests`, `tests/VVooOverthrown.App.Tests`: focused regression tests.

### Task 1: Reproducible solution and protocol

**Files:**
- Create: `global.json`
- Create: `Directory.Build.props`
- Create: `VVooOverthrown.slnx`
- Create: `tools/bootstrap-dotnet.ps1`
- Create: `src/VVooOverthrown.Protocol/VVooOverthrown.Protocol.csproj`
- Create: `src/VVooOverthrown.Protocol/Messages/Envelope.cs`
- Create: `src/VVooOverthrown.Protocol/Transport/LengthPrefix.cs`
- Test: `tests/VVooOverthrown.Protocol.Tests/LengthPrefixTests.cs`

**Interfaces:**
- Produces: `Envelope(string Type, string RequestId, JsonElement Payload)`.
- Produces: `LengthPrefix.WriteAsync(Stream, ReadOnlyMemory<byte>, CancellationToken)` and `ReadAsync(Stream, int, CancellationToken)`.

- [x] **Step 1: Add a failing framing test**

```csharp
[Fact]
public async Task RoundTripPreservesUtf8Payload()
{
    await using var stream = new MemoryStream();
    var expected = Encoding.UTF8.GetBytes("{\"message\":\"한글\"}");
    await LengthPrefix.WriteAsync(stream, expected, default);
    stream.Position = 0;
    Assert.Equal(expected, await LengthPrefix.ReadAsync(stream, 1024, default));
}
```

- [x] **Step 2: Bootstrap SDK and prove RED**

Run: `powershell -NoProfile -File tools/bootstrap-dotnet.ps1`

Run: `.\.tools\dotnet-sdk\dotnet.exe test tests\VVooOverthrown.Protocol.Tests\VVooOverthrown.Protocol.Tests.csproj`

Expected: compile failure because `LengthPrefix` does not exist.

- [x] **Step 3: Implement four-byte little-endian framing with a 1 MiB hard limit**

```csharp
public static class LengthPrefix
{
    public const int MaxFrameLength = 1024 * 1024;
    public static Task WriteAsync(Stream stream, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken);
    public static Task<byte[]> ReadAsync(Stream stream, int maximumLength, CancellationToken cancellationToken);
}
```

- [x] **Step 4: Run protocol tests and commit**

Run: `.\.tools\dotnet-sdk\dotnet.exe test tests\VVooOverthrown.Protocol.Tests\VVooOverthrown.Protocol.Tests.csproj`

Expected: all tests pass.

Commit: `git commit -m "feat: add trainer protocol framing"`

### Task 2: Supported build validation

**Files:**
- Create: `src/VVooOverthrown.Core/Build/SupportedBuildProfile.cs`
- Create: `src/VVooOverthrown.Core/Build/GameBuildValidator.cs`
- Create: `src/VVooOverthrown.Core/Discovery/GameLocator.cs`
- Test: `tests/VVooOverthrown.Core.Tests/GameBuildValidatorTests.cs`

**Interfaces:**
- Produces: `GameBuildValidator.ValidateAsync(string gameRoot, SupportedBuildProfile profile, CancellationToken)` returning `BuildValidationResult`.
- Produces: `GameLocator.ValidateRoot(string path)` returning the normalized root or a Korean validation error.

- [x] **Step 1: Write failing exact-hash and mismatch tests**

```csharp
[Fact]
public async Task RejectsChangedGameAssembly()
{
    var result = await validator.ValidateAsync(gameRoot, profile, default);
    Assert.False(result.IsSupported);
    Assert.Contains("GameAssembly.dll", result.Mismatches);
}
```

- [x] **Step 2: Run Core tests and verify RED**

Run: `.\.tools\dotnet-sdk\dotnet.exe test tests\VVooOverthrown.Core.Tests\VVooOverthrown.Core.Tests.csproj --filter GameBuildValidatorTests`

Expected: compile failure for missing validator types.

- [x] **Step 3: Implement streaming SHA-256 validation and safe path normalization**

Use the exact three hashes from `docs/project-journal.md`. Reject missing files, directories masquerading as files, and paths whose final executable is not `Overthrown.exe`.

- [x] **Step 4: Run tests against fixtures and the real game read-only**

Run: `.\.tools\dotnet-sdk\dotnet.exe test tests\VVooOverthrown.Core.Tests\VVooOverthrown.Core.Tests.csproj --filter GameBuildValidatorTests`

Run: `tools\build.ps1 -GameDir 'W:\Games\Overthrown' -ValidateOnly`

Expected: tests pass and the real build reports `Supported`.

- [x] **Step 5: Commit**

Commit: `git commit -m "feat: validate supported Overthrown build"`

### Task 3: Transactional installer, removal, and save backup

**Files:**
- Create: `src/VVooOverthrown.Core/Installation/InstallManifest.cs`
- Create: `src/VVooOverthrown.Core/Installation/PayloadInstaller.cs`
- Create: `src/VVooOverthrown.Core/Saves/SaveBackupService.cs`
- Test: `tests/VVooOverthrown.Core.Tests/PayloadInstallerTests.cs`
- Test: `tests/VVooOverthrown.Core.Tests/SaveBackupServiceTests.cs`

**Interfaces:**
- Produces: `PayloadInstaller.InstallAsync(gameRoot, payloadRoot, build, cancellationToken)`.
- Produces: `PayloadInstaller.RemoveAsync(gameRoot, cancellationToken)`.
- Produces: `SaveBackupService.CreateAsync(sourceRoot, destinationRoot, cancellationToken)` returning file hashes.

- [ ] **Step 1: Write failing rollback and conservative-removal tests**

```csharp
[Fact]
public async Task RemovePreservesInstalledFileChangedByUser()
{
    await installer.InstallAsync(gameRoot, payloadRoot, build, default);
    await File.AppendAllTextAsync(installedPlugin, "user change");
    var result = await installer.RemoveAsync(gameRoot, default);
    Assert.True(File.Exists(installedPlugin));
    Assert.Contains(installedPlugin, result.PreservedModifiedFiles);
}
```

- [ ] **Step 2: Run focused tests and verify RED**

Run: `.\.tools\dotnet-sdk\dotnet.exe test tests\VVooOverthrown.Core.Tests\VVooOverthrown.Core.Tests.csproj --filter "PayloadInstallerTests|SaveBackupServiceTests"`

- [ ] **Step 3: Implement staging, manifest, rollback, and backup hashes**

Reject rooted manifest paths, `..` segments, symlink/reparse-point escapes, running game processes, and unsupported builds. Manifest entries contain relative path, SHA-256, byte length, and owner `VVooOverthrown`.

- [ ] **Step 4: Run focused tests and commit**

Run: `.\.tools\dotnet-sdk\dotnet.exe test tests\VVooOverthrown.Core.Tests\VVooOverthrown.Core.Tests.csproj --filter "PayloadInstallerTests|SaveBackupServiceTests"`

Expected: all tests pass, including an injected mid-copy failure that restores the fixture.

Commit: `git commit -m "feat: add safe payload installation and backup"`

### Task 4: Korean WPF shell and self-contained EXE

**Files:**
- Create: `src/VVooOverthrown.App/App.xaml`
- Create: `src/VVooOverthrown.App/MainWindow.xaml`
- Create: `src/VVooOverthrown.App/ViewModels/MainViewModel.cs`
- Create: `src/VVooOverthrown.Core/State/TrainerMainState.cs`
- Test: `tests/VVooOverthrown.App.Tests/MainViewModelTests.cs`
- Create: `tools/package.ps1`

**Interfaces:**
- Consumes: `GameBuildValidator`, `PayloadInstaller`, `SaveBackupService`.
- Produces: `MainViewModel.RefreshAsync`, `InstallAsync`, `RemoveAsync`, `LaunchGameAsync`.

- [ ] **Step 1: Write failing state-transition tests**

```csharp
[Theory]
[InlineData(false, false, "게임 경로 확인 필요")]
[InlineData(true, false, "지원하지 않는 게임 빌드")]
[InlineData(true, true, "한글 패치 설치 가능")]
public void StatusMessageMatchesBuildState(bool pathValid, bool buildSupported, string expected)
{
    Assert.Equal(expected, TrainerMainState.Evaluate(pathValid, buildSupported).Message);
}
```

- [ ] **Step 2: Run App tests and verify RED**

Run: `.\.tools\dotnet-sdk\dotnet.exe test tests\VVooOverthrown.App.Tests\VVooOverthrown.App.Tests.csproj`

- [ ] **Step 3: Implement accessible Korean UI and commands**

The initial window contains game path, build status, installation status, `설치`, `제거`, `게임 실행`, `연결`, `전체 초기화`, and a read-only event log. Destructive buttons remain disabled unless Core returns an eligible state.

- [ ] **Step 4: Test, publish, and smoke launch**

Run: `tools\build.ps1 -GameDir 'W:\Games\Overthrown'`

Run: `tools\package.ps1 -GameDir 'W:\Games\Overthrown'`

Expected: `.artifacts\release\VVooOverthrown.exe` exists, launches, recognizes the supported game, and exits normally.

- [ ] **Step 5: Commit**

Commit: `git commit -m "feat: add Korean trainer application shell"`
