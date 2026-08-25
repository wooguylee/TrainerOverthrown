using HarmonyLib;

namespace VVooOverthrown.Helper.Runtime;

internal static class MovementTuningRuntime
{
    public static MovementTuningState State { get; } = new();
}

[HarmonyPatch(typeof(PlayerMovement), nameof(PlayerMovement.TriggerJumpInternal))]
internal static class JumpVelocityPatch
{
    private static void Prefix(PlayerMovement __instance, ref float jumpVelocity)
    {
        if (__instance == null)
        {
            return;
        }

        jumpVelocity = MovementTuningRuntime.State.ScaleJumpVelocity(
            jumpVelocity,
            (int)__instance.jumpType,
            __instance.isLocalPlayer);
    }
}

[HarmonyPatch(typeof(PlayerMovement), nameof(PlayerMovement.TriggerVariableHeightJump))]
internal static class VariableHeightJumpPatch
{
    private static void Prefix(PlayerMovement __instance, ref float holdBonusVelocity)
    {
        if (__instance == null)
        {
            return;
        }

        holdBonusVelocity = MovementTuningRuntime.State.ScaleVariableHeightBonus(
            holdBonusVelocity,
            (int)__instance.jumpType,
            __instance.isLocalPlayer);
    }
}

[HarmonyPatch(typeof(PlayerMovement), nameof(PlayerMovement.GetBaseGravitySpeedDelta))]
internal static class GravityMultiplierPatch
{
    private static void Postfix(PlayerMovement __instance, ref float __result)
    {
        if (__instance == null)
        {
            return;
        }

        __result = MovementTuningRuntime.State.ScaleGravityDelta(
            __result,
            __instance.isLocalPlayer);
    }
}
