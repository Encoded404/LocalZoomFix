using HarmonyLib;

namespace LocalZoom.Patches
{
    [HarmonyPatch(typeof(DeathEffect), "PlayDeath")]
    public class DeathEffectPatch
    {
        public static void Prefix(DeathEffect __instance)
        {
            LocalZoom.MakeObjectHidden(__instance);
        }
    }
}