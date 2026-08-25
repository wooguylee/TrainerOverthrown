# Expanded Trainer Test Mode Implementation Plan

**Goal:** Expand the supported-build trainer into a tabbed test console with
player, movement/time, inventory, kingdom-resource, and diagnostic functions.

**Architecture:** Keep IPC DTOs in `Helper.Core`, isolate request validation as a
pure helper, implement generated-game API access in `Helper`, and expose commands
through `MainViewModel` to a WPF `TabControl`. Raw session detection remains
visible but no longer authorizes or resets commands.

**Tech stack:** .NET 6 Helper, BepInEx IL2CPP, Harmony, generated Overthrown/Mirror
interop, .NET 10 WPF desktop app, xUnit.

---

1. Add failing Helper.Core tests for command mutation classification, numeric
   ranges, resource IDs, and the expanded pipe contract.
2. Add failing App tests proving an `Uncertain` session still enables test-mode
   controls and that resource commands carry the selected resource and amount.
3. Extend `PipeRequest`/`PipeResponse` and implement the pure request validator.
4. Add runtime feature access for heal, stamina factor, movement multiplier,
   inventory resources, and kingdom resources; remove session authorization and
   session-triggered reset.
5. Build a five-tab WPF UI and wire all commands through the ViewModel.
6. Update user-facing documentation for test mode, feature availability, reset
   semantics, and persistent resource edits.
7. Run targeted tests, Release builds, staged installation, and read-only live
   status/resource-query checks.
8. Review the diff, commit the completed change, and push `master`.
