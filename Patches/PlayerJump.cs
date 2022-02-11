using HarmonyLib;

namespace LocalZoom.Patches
{
    [HarmonyPatch(typeof(PlayerJump), "Start")]
    public class PlayerJumpPatch
    {
        public static void Postfix(PlayerJump __instance)
        {
            if (LocalZoom.IsInOfflineModeAndNotSandbox || !LocalZoom.enableShaderSetting)
                return;
            
            LocalZoom.MakeObjectHidden(__instance);
        }
    }
}