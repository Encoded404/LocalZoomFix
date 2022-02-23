using HarmonyLib;
using MapEmbiggener.Controllers;

namespace LocalZoom.Patches
{
    [HarmonyPatch(typeof(Player), "FullReset")]
    class PlayerPatchFullReset
    {
        static void Postfix(Player __instance)
        {
            if (LocalZoom.IsInOfflineModeAndNotSandbox || !LocalZoom.enableShaderSetting || __instance is null)
            {
                return;
            }
            if (ControllerManager.CurrentCameraControllerID != MyCameraController.ControllerID) { return; }
            ((MyCameraController)ControllerManager.CurrentCameraController).ResetZoomLevel(__instance);
        }
    }
}
