using HarmonyLib;

namespace VVooOverthrown.Helper.Runtime;

[HarmonyPatch(typeof(PlayerMovement), nameof(PlayerMovement.UpdateSpeedFactor))]
internal static class MovementSpeedPatch
{
    public static float Multiplier { get; set; } = 1f;

    private static void Postfix(PlayerMovement __instance)
    {
        if (__instance != null && __instance.isLocalPlayer && Multiplier != 1f)
        {
            __instance.speedFactor *= Multiplier;
        }
    }
}
