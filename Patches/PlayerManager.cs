using System.Linq;
using HarmonyLib;

namespace LocalZoom.Patches
{
    [HarmonyPatch(typeof(PlayerManager), "PlayerDied")]
    public class PlayerManagerPatch
    {
        public static void Postfix(PlayerManager __instance, Player player)
        {
            if (LocalZoom.IsInOfflineModeAndNotSandbox || (!LocalZoom.enableCameraSetting && !LocalZoom.enableShaderSetting))
                return;
            
            if (player.data.view.IsMine)
            {
                LocalZoom.instance.enableResetCamera = true;
            }
            // the below code causes problems for gamemodes or cards with custom revives and is redundant with
            // MyCameraController::OnUpdate
            /*
            if(__instance.players.Where(p=>!p.data.dead).ToList().Count == 1)
            {
                LocalZoom.instance.enableResetCamera = true;
            }*/
        }
    }
}