# Infinite Ctrl Movement Design

## Goal

Add a reversible `Ctrl 신속 이동 무한` trainer toggle that combines the
confirmed `100x` player stamina recovery factor with removal of the local
player's dash cooldown. Keep normal invalid-state checks such as attacks,
knockdown, death, and build-mode restrictions.

## Proven game behavior

- Ctrl uses the game's dash-to-run path (`Dash`, `Dash to run`, and `Sprint`).
- `DifficultyManager.NetworkplayerStaminaFactor` is a recovery factor, not a
  consumption factor. The shipped peaceful and relaxing presets use `2`, while
  challenging uses `1`; the user also confirmed that `100x` recovers stamina
  very quickly.
- `PlayerMovement` separately exposes `timeSinceDash`, `dashCooldown`, and
  `airDashUsed`. The feature may satisfy the elapsed cooldown but must not clear
  `airDashUsed`, so it does not introduce unlimited airborne dashes.

## Runtime behavior

- Add the command `infiniteCtrlMovement` and capability
  `movement.infiniteCtrl`.
- Enabling requires both a loaded local `PlayerMovement` and
  `DifficultyManager.Instance`.
- On first enable, remember the current stamina recovery factor, set it to
  `100x`, and mark the feature active.
- A Harmony postfix on `PlayerMovement.UpdateTimers` raises only the local
  player's `timeSinceDash` to at least `dashCooldown` while active. It does not
  override `CanDash`, `CanStartSprinting`, `CanContinueSprinting`, or
  `airDashUsed`.
- Disabling restores the factor captured when the toggle was enabled and stops
  adjusting the dash timer.
- Setting a manual stamina recovery factor while the toggle is active disables
  this toggle first, restores its captured factor, then applies the requested
  manual value.
- `reset`, pipe disconnect, Helper destruction, and app close disable the toggle
  and participate in the existing transient-value restoration flow.

## UI behavior

- In `이동·시간`, add `Ctrl 신속 이동 무한` with explicit on/off buttons and
  an active-state indicator.
- Rename `기력 소모 배율` to `기력 회복 배율`.
- Explain that `0x` disables recovery, `1x` is the original value, and `100x`
  is near-instant recovery.
- Status responses expose `InfiniteCtrlMovementEnabled`; the app treats the
  Helper response as authoritative.

## Verification boundary

- Test the pure enable/disable and cooldown-readiness state, command validation,
  response propagation, and WPF construction.
- Build the compiled Helper and app, stage/install the changed payload, and
  verify the Helper loads without BepInEx errors.
- Do not create or regenerate a release ZIP.
