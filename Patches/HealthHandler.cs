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
            if (__instance.GetComponent<PhotonView>().IsMine && !LocalZoom.IsInOfflineModeAndNotSandbox && !CardChoice.instance.IsPicking)
            {
                LocalZoom.instance.enableResetCamera = false;
                MapManager.instance.currentMap.Map.size = LocalZoom.defaultMapSize/1.10f;
                LocalZoom.instance.phoenixCircle.SetActive(false);
                LocalZoom.instance.phoenixBlackBox.SetActive(false);
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
            if (__instance.GetComponent<PhotonView>().IsMine && !LocalZoom.IsInOfflineModeAndNotSandbox && !CardChoice.instance.IsPicking)
            {
                LocalZoom.instance.enableResetCamera = false;
                LocalZoom.instance.phoenixCircle.transform.position = __instance.transform.position;
                LocalZoom.instance.phoenixCircle.transform.localScale = Vector3.one * 7.5f * __instance.transform.localScale.x;
                LocalZoom.instance.phoenixCircle.SetActive(true);
                LocalZoom.instance.phoenixBlackBox.transform.position = __instance.transform.position;
                LocalZoom.instance.phoenixBlackBox.SetActive(true);
            }
        }
    }
}