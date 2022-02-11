using HarmonyLib;
using UnboundLib;

namespace LocalZoom.Patches
{
    [HarmonyPatch(typeof(ArtHandler), "NextArt")]
    public class ArtHandlerPatch
    {
        [HarmonyAfter("pykess.rounds.plugins.performanceimprovements")]
        public static void Postfix(ArtHandler __instance)
        {
            if (LocalZoom.IsInOfflineModeAndNotSandbox || !LocalZoom.enableShaderSetting)
                return;
            foreach (var player in PlayerManager.instance.players)
            {
                LocalZoom.instance.MakeGunHidden(player);
            }
        }
    }
}