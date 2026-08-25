using HarmonyLib;

namespace VVooOverthrown.Helper.Runtime;

[HarmonyPatch(typeof(PlayerMovement), nameof(PlayerMovement.UpdateTimers))]
internal static class InfiniteCtrlMovementPatch
{
    public static InfiniteCtrlMovementState State { get; } = new();

    private static void Postfix(PlayerMovement __instance) => Apply(__instance);

    public static void Apply(PlayerMovement movement)
    {
        if (movement == null)
        {
            return;
        }

        movement.timeSinceDash = State.ReadyDashTimer(
            movement.timeSinceDash,
            movement.dashCooldown,
            movement.isLocalPlayer);
    }
}
