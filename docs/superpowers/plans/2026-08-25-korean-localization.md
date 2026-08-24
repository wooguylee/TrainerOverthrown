# Korean Localization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extract and validate Overthrown English text, ship a curated Korean MVP dictionary, and apply it safely at runtime without replacing original bundles.

**Architecture:** A deterministic analysis tool exports source strings and validates translation artifacts. The IL2CPP Helper loads only valid local JSON, creates a Korean-capable TMP fallback font, and applies exact deterministic replacements through a main-thread localization component.

**Tech Stack:** .NET 10 analysis tool, AssetRipper/Unity asset parsing, JSON, BepInEx 6 IL2CPP, Il2CppInterop, HarmonyX, TextMeshPro

**Spec:** `docs/superpowers/specs/2026-08-25-vvoo-overthrown-design.md`

## Global Constraints

- Preserve original Addressables bundles and catalogs byte-for-byte.
- Runtime translation is local-only and never calls an online translation endpoint.
- Preserve every composite-format placeholder, Smart String selector, and TMP rich-text tag.
- Untranslated or invalid entries remain in English.
- Package translation files as owned payload files with hashes.

---

## File map

- `tools/VVooOverthrown.LocalizationTool`: extract, merge, validate, and report translation data.
- `translation/source.en.json`: extracted English source keyed by stable table/key when available.
- `translation/ko.json`: reviewed Korean output used at runtime.
- `translation/glossary.ko.json`: canonical game terms.
- `translation/coverage.json`: generated counts and validation result.
- `src/VVooOverthrown.Helper/Localization`: runtime dictionary, font, and TMP application.

### Task 1: Translation artifact contract and validation

**Files:**
- Create: `tools/VVooOverthrown.LocalizationTool/Models/TranslationEntry.cs`
- Create: `tools/VVooOverthrown.LocalizationTool/Validation/TranslationValidator.cs`
- Test: `tests/VVooOverthrown.LocalizationTool.Tests/TranslationValidatorTests.cs`
- Create: `translation/glossary.ko.json`

**Interfaces:**
- Produces: `TranslationEntry(string Id, string Source, string Korean, string Status)`.
- Produces: `TranslationValidator.Validate(IReadOnlyList<TranslationEntry>)` returning errors and coverage.

- [x] **Step 1: Write failing placeholder and TMP-tag tests**

```csharp
[Fact]
public void RejectsDroppedFormatPlaceholder()
{
    var result = validator.Validate([new("ui.count", "Count: {0}", "수량", "reviewed")]);
    Assert.Contains(result.Errors, error => error.Code == "PLACEHOLDER_MISMATCH");
}
```

- [x] **Step 2: Run tests and verify RED**

Run: `.\.tools\dotnet-sdk\dotnet.exe test tests\VVooOverthrown.LocalizationTool.Tests\VVooOverthrown.LocalizationTool.Tests.csproj`

- [x] **Step 3: Implement exact multiset comparison for placeholders and tags**

Validate unique IDs, non-empty sources, reviewed Korean values, `{name}`/`{0}` placeholder multisets, `<tag>` balance, and accidental source-equals-target values.

- [x] **Step 4: Run tests and commit**

Expected: all validation tests pass.

Commit: `git commit -m "feat: validate Korean translation artifacts"`

### Task 2: English source extraction and Korean MVP data

**Files:**
- Create: `tools/extract_unity_localization.py`
- Create: `tools/extract-localization.ps1`
- Create: `translation/source.en.json`
- Create: `translation/ko.json`
- Create: `translation/coverage.json`
- Test: `tests/tools/test_localization_extractor.py`

**Interfaces:**
- Produces: `extract_unity_localization.py` returning stable ID/source records from pinned UnityPy.
- Consumes the installed English and shared localization bundles without writing them.

- [x] **Step 1: Add a failing fixture extraction test**

```csharp
[Fact]
public void ExtractedEntriesHaveStableUniqueIds()
{
    var entries = extractor.Extract(fixtureEnglishBundle, fixtureSharedBundle);
    Assert.NotEmpty(entries);
    Assert.Equal(entries.Count, entries.Select(entry => entry.Id).Distinct().Count());
}
```

- [x] **Step 2: Run the extraction test and verify RED**

Run: `.\.tools\dotnet-sdk\dotnet.exe test tests\VVooOverthrown.LocalizationTool.Tests\VVooOverthrown.LocalizationTool.Tests.csproj --filter EnglishTableExtractorTests`

- [x] **Step 3: Implement parsing with a pinned asset parser**

Pin the exact parser version and checksum in `tools/tool-manifest.json`. Extract StringTable key IDs and values; if key names are unavailable, emit stable IDs as `<table-guid>:<key-id>`. Sort ordinally before JSON serialization.

- [x] **Step 4: Generate source data and curate the MVP**

Run: `tools\extract-localization.ps1 -GameDir 'W:\Games\Overthrown'`

Translate and review menu, settings, new/load game, difficulty, HUD, inventory, building, research, and common confirmation/error terms. Apply the glossary consistently and keep every token intact.

- [x] **Step 5: Validate, report coverage, and commit**

Run: `.\.tools\dotnet-sdk\dotnet.exe run --project tools\VVooOverthrown.LocalizationTool -- validate translation\source.en.json translation\ko.json translation\coverage.json`

Expected: exit 0, zero token/tag errors, and nonzero reviewed coverage.

Commit: `git commit -m "feat: add Overthrown Korean MVP translations"`

### Task 3: Runtime Korean localization Helper

**Files:**
- Create: `src/VVooOverthrown.Helper/Localization/TranslationCatalog.cs`
- Create: `src/VVooOverthrown.Helper/Localization/KoreanFontProvider.cs`
- Create: `src/VVooOverthrown.Helper/Localization/KoreanLocalizationService.cs`
- Create: `src/VVooOverthrown.Helper/Localization/TextReplacementPolicy.cs`
- Test: `tests/VVooOverthrown.Helper.Tests/TranslationCatalogTests.cs`
- Test: `tests/VVooOverthrown.Helper.Tests/TextReplacementPolicyTests.cs`

**Interfaces:**
- Produces: `TranslationCatalog.TryTranslate(string source, out string korean)`.
- Produces: `TextReplacementPolicy.ShouldReplace(source, current, korean)`.
- Produces: `KoreanLocalizationService.Start()`, `Tick()`, and `Stop()` called only on Unity main thread.

- [ ] **Step 1: Write failing deterministic lookup and idempotence tests**

```csharp
[Fact]
public void AlreadyTranslatedTextIsNotReprocessed()
{
    Assert.False(policy.ShouldReplace("Settings", "설정", "설정"));
}
```

- [ ] **Step 2: Run focused tests and verify RED**

Run: `.\.tools\dotnet-sdk\dotnet.exe test tests\VVooOverthrown.Helper.Tests\VVooOverthrown.Helper.Tests.csproj --filter "TranslationCatalogTests|TextReplacementPolicyTests"`

- [ ] **Step 3: Implement local catalog and TMP main-thread application**

Use an ordinal source-to-Korean map, preserve dynamic suffixes only through explicitly tested patterns, scan active TMP text no faster than four times per second, and create a dynamic fallback from `Malgun Gothic`/`맑은 고딕`. Catch per-component failures so one text object cannot stop the loop.

- [ ] **Step 4: Package and verify runtime initialization**

Run: `tools\build.ps1 -GameDir 'W:\Games\Overthrown'`

Expected: Helper tests pass and the payload contains `translation\ko.json` plus a catalog hash.

- [ ] **Step 5: Commit**

Commit: `git commit -m "feat: apply deterministic Korean localization at runtime"`
