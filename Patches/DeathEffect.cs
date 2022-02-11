using HarmonyLib;

namespace LocalZoom.Patches
{
    [HarmonyPatch(typeof(DeathEffect), "PlayDeath")]
    public class DeathEffectPatch
    {
        public static void Prefix(DeathEffect __instance)
        {
            if (LocalZoom.IsInOfflineModeAndNotSandbox || !LocalZoom.enableShaderSetting)
                return;
            
            LocalZoom.MakeObjectHidden(__instance);
        }
    }
}