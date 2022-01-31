using HarmonyLib;
using UnboundLib;

namespace LocalZoom.Patches
{
    [HarmonyPatch(typeof(ProjectileHit), "Start")]
    public class ProjectileHitPatch
    {
        public static void Postfix(ProjectileHit __instance)
        {
            if (LocalZoom.IsInOfflineModeAndNotSandbox || !LocalZoom.enableCameraSetting)
                return;
            
            LocalZoom.MakeObjectHidden(__instance);
        }
    }
}