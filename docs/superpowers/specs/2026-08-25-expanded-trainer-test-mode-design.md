# Expanded Trainer Test Mode Design

## Goal

Turn the current two-command trainer into a broad test console for the supported
Overthrown build. The desktop app must expose features in clear groups and must
not disable mutations merely because the game's network/session flags are
ambiguous.

## Safety boundary

- Keep the pinned-build hash guard. Calling generated IL2CPP APIs from a different
  game build can crash the game and is outside test-mode scope.
- Treat `OfflineSessionGuard` output as diagnostics only. It must not gate trainer
  commands or automatically reset active runtime changes.
- Require the concrete target object for each command. Return a specific
  `FEATURE_UNAVAILABLE` error while a player, inventory, or global resource store
  is not loaded.
- Restore transient changes (invulnerability, stamina factor, movement multiplier,
  and time scale) when reset/disconnect/unload runs.
- Resource edits intentionally change gameplay state. They are never performed by
  status refresh or live verification, and the UI labels them as persistent.

## Feature groups

### Player

- God mode: maintain local player's invulnerability and refill health.
- Heal: refill the local player's current health once.
- Stamina factor: set `DifficultyManager.NetworkplayerStaminaFactor` from zero
  (no stamina consumption) through 100 and latch the original value for reset.

### Movement and time

- Movement multiplier: Harmony-postfix `PlayerMovement.UpdateSpeedFactor` and
  multiply the freshly calculated local-player speed factor.
- World time scale: set `GameTime.gameTimeScale`, preserving its original value.

### Inventory

- Query a selected `ResourceType` in the local `PlayerInventory`.
- Set the selected resource amount, or add an amount, using the generated
  `InventoryItem`/`SyncList` API and the resource's default item.
- Verify the observed amount after each persistent mutation. A clamp or refused
  write is reported as `RESOURCE_PARTIAL_APPLY`, never as a successful change.

### Kingdom resources

- Query `GlobalResourceStorage.GetStoredAmount` for a selected resource.
- Set or add via the game's `Deposit` and `Withdraw` paths.

### Diagnostics

- Report test-mode state, raw session decision, offline/host/connection signals,
  supported capabilities, and current transient values.
- Reset all transient trainer changes without claiming to undo resource edits.

## IPC contract

`PipeRequest` keeps the existing command fields and adds `ResourceType` and
`Amount`. Commands are `status`, `reset`, `godMode`, `heal`, `staminaFactor`,
`movementSpeed`, `timeScale`, `inventoryQuery`, `inventorySet`, `inventoryAdd`,
`kingdomQuery`, `kingdomSet`, and `kingdomAdd`.

`PipeResponse` reports all current transient values plus raw diagnostics and the
selected inventory/kingdom amount when a resource command is executed.

## Desktop UX

Helper connection and its status remain above a `TabControl`. Tabs are named
`플레이어`, `이동·시간`, `인벤토리`, `왕국 자원`, and `진단`. All mutation controls
are enabled after a successful Helper response, regardless of the raw session
decision. Each resource tab has a resource selector, amount input, query, set,
and add buttons. Amount input is validated as text before any persistent command;
the entered delta and the observed result are displayed separately.

## Verification

- Unit-test command classification, range/resource validation, IPC round trips,
  and ViewModel test-mode enablement.
- Build the Helper and WPF app against the installed supported game interop.
- Install the staged payload, reconnect to the live game, verify `status`, and run
  resource queries only. Do not mutate resource state during automated live checks.
