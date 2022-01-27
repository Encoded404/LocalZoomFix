using HarmonyLib;
using Photon.Pun;

namespace LocalZoom.Patches
{
    [HarmonyPatch(typeof(PlayerName), "Start")]
    public class PlayerNamePatch
    {
        public static void Postfix(PlayerName __instance)
        {
            if (!__instance.transform.root.GetComponent<PhotonView>().IsMine)
            {
                LocalZoom.MakeObjectHidden(__instance);
            }
        }
    }
}