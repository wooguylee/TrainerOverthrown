# Numeric Movement Tuning Design

## Goal

Replace every preset multiplier button with numeric input and an apply button,
then add independent local-player multipliers for regular jumps, special
movement impulses, and gravity. Every multiplier accepts finite values from
`0` through `1000` inclusive.

## Existing multiplier controls

The following preset-button groups become numeric input rows without changing
their existing trainer commands or runtime semantics:

- Stamina recovery factor
- Player movement speed
- World time scale

Each row starts at `1`, accepts decimal input, displays the Helper-reported
applied value, and disables its apply button while the input is invalid.
`Ctrl 신속 이동 무한` remains an explicit on/off control because it is a
compound feature rather than a numeric multiplier.

## New movement controls

The `이동·시간` tab adds three independent numeric rows:

- `일반 점프 배율`: `GroundStationary`, `GroundForward`,
  `GroundStrafeLeft`, `GroundStrafeRight`, and `GroundStrafeBack`.
- `특수 이동 배율`: every other `PlayerMovement.JumpType`, including wall,
  ledge, swimming, shovel bounce, automatic step-up, ledge vault, and twirl.
- `중력 배율`: the local player's base gravity speed delta.

All three default to `1x`. `0x` is intentionally supported for test mode:
regular or special jump impulses become zero in their own group, while gravity
`0x` removes the base gravity delta. Values above `1x` amplify the selected
behavior. A jump velocity multiplier is not a linear height multiplier; high
values can create extremely large jump heights.

## Runtime design

- Add commands `regularJumpMultiplier`, `specialMovementMultiplier`, and
  `gravityMultiplier`.
- Add capabilities `movement.regularJumpMultiplier`,
  `movement.specialMovementMultiplier`, and `movement.gravityMultiplier`.
- A pure `MovementTuningState` owns the three multipliers, validates jump-type
  grouping without Unity dependencies, and resets all values to `1x`.
- A Harmony prefix on `PlayerMovement.TriggerJumpInternal` multiplies its
  `jumpVelocity` argument for the local player according to the current
  `jumpType` group.
- A Harmony prefix on `PlayerMovement.TriggerVariableHeightJump` multiplies
  only `holdBonusVelocity`; the initial impulse continues through
  `TriggerJumpInternal`, preventing double scaling.
- A Harmony postfix on `PlayerMovement.GetBaseGravitySpeedDelta` multiplies its
  result for the local player.
- Remote-player instances are never changed.
- Reset, pipe disconnect, Helper destruction, and app close set all three
  multipliers back to `1x`.

The existing stamina, movement-speed, and time-scale commands expand their
accepted numeric ranges to `0..1000`. All six numeric commands reject negative,
non-finite, or greater-than-`1000` values with `OUT_OF_RANGE`.

## IPC and desktop state

`PipeResponse` adds authoritative `RegularJumpMultiplier`,
`SpecialMovementMultiplier`, and `GravityMultiplier` values. The ViewModel
copies every Helper-reported multiplier into its current-value message after a
successful command or status response.

Each numeric input has its own text, validity flag, validation message, and
apply method. Parsing accepts the current Windows culture and invariant decimal
format. Invalid input never sends a pipe command. Input validation is local UI
feedback; the Helper independently enforces the same `0..1000` range.

## Verification and delivery

- Use test-first focused tests for numeric range validation, movement-tuning
  grouping/scaling/reset behavior, and ViewModel numeric input commands.
- Run only the related Helper/App test filters plus Release compilation of the
  changed Helper and WPF app projects.
- Do not run the full solution test suite or launch the game automatically.
- Do not create a release ZIP.
- Commit the implementation to `master` and push it after verification.
