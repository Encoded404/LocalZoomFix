using HarmonyLib;

namespace LocalZoom.Patches
{
    [HarmonyPatch(typeof(ProjectileHit), "Start")]
    public class ProjectileHitPatch
    {
        public static void Postfix(ProjectileHit __instance)
        {
            LocalZoom.MakeObjectHidden(__instance);
        }
    }
}