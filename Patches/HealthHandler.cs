using HarmonyLib;

namespace LocalZoom.Patches
{
    [HarmonyPatch(typeof(HealthHandler), "Revive")]
    public class HealthHandlerPatch
    {
        public static void Postfix(HealthHandler __instance, bool isFullRevive)
        {
            if (isFullRevive)
            {
                LocalZoom.instance.enableResetCamera = false;
            }
        }
    }
}