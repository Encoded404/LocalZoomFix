using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace LocalZoom.Patches
{
    [HarmonyPatch(typeof(HealthHandler), "Revive")]
    public class HealthHandlerRevivePatch
    {
        public static void Postfix(HealthHandler __instance)
        {
            if (LocalZoom.IsInOfflineModeAndNotSandbox || !LocalZoom.enableCameraSetting)
                return;
            
            if (__instance.GetComponent<PhotonView>().IsMine && !CardChoice.instance.IsPicking)
            {
                LocalZoom.instance.enableResetCamera = false;
                MapManager.instance.currentMap.Map.size =Mathf.Clamp(
                    LocalZoom.defaultMapSize / 1.25f * __instance.transform.localScale.x, 0,
                    LocalZoom.defaultMapSize + LocalZoom.defaultMapSize / 4);
                if (LocalZoom.instance.phoenixCircle != null)
                {
                    LocalZoom.instance.phoenixCircle.SetActive(false);
                    LocalZoom.instance.phoenixBlackBox.SetActive(false);
                }
                foreach (var player in PlayerManager.instance.players)
                {
                    LocalZoom.instance.MakeGunHidden(player);
                }
            }
        }
    }
    [HarmonyPatch(typeof(HealthHandler), "RPCA_Die_Phoenix")]
    public class HealthHandlerPhoenixPatch
    {
        public static void Postfix(HealthHandler __instance)
        {
            if (LocalZoom.IsInOfflineModeAndNotSandbox || !LocalZoom.enableCameraSetting)
                return;
            
            if (__instance.GetComponent<PhotonView>().IsMine && !CardChoice.instance.IsPicking)
            {
                LocalZoom.instance.enableResetCamera = false;
                if (LocalZoom.instance.phoenixCircle != null)
                {
                    LocalZoom.instance.phoenixCircle.transform.position = __instance.transform.position;
                    LocalZoom.instance.phoenixCircle.transform.localScale =
                        Vector3.one * 7.5f * __instance.transform.localScale.x;
                    LocalZoom.instance.phoenixCircle.SetActive(true);
                    LocalZoom.instance.phoenixBlackBox.transform.position = __instance.transform.position;
                    LocalZoom.instance.phoenixBlackBox.SetActive(true);
                }
            }
        }
    }
}