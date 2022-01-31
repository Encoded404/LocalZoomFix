using System.Linq;
using HarmonyLib;

namespace LocalZoom.Patches
{
    [HarmonyPatch(typeof(PlayerManager), "PlayerDied")]
    public class PlayerManagerPatch
    {
        public static void Postfix(PlayerManager __instance, Player player)
        {
            if (LocalZoom.IsInOfflineModeAndNotSandbox || !LocalZoom.enableCameraSetting)
                return;
            
            if (player.data.view.IsMine)
            {
                LocalZoom.instance.enableResetCamera = true;
            }
            if(__instance.players.Where(p=>!p.data.dead).ToList().Count == 1)
            {
                LocalZoom.instance.enableResetCamera = true;
            }
        }
    }
}