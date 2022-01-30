using HarmonyLib;

namespace LocalZoom.Patches
{
    [HarmonyPatch(typeof(PlayerManager), "PlayerDied")]
    public class PlayerManagerPatch
    {
        public static void Postfix(PlayerManager __instance, Player player)
        {
            if (player.data.view.IsMine)
            {
                LocalZoom.instance.enableResetCamera = true;
            }
            if(__instance.players.Count == 1)
            {
                LocalZoom.instance.enableResetCamera = true;
            }
        }
    }
}