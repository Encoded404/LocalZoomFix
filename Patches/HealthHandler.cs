using HarmonyLib;
using MapEmbiggener.Controllers;
using Photon.Pun;
using UnityEngine;

namespace LocalZoom.Patches
{
    [HarmonyPatch(typeof(HealthHandler), "Revive")]
    public class HealthHandlerRevivePatch
    {
        public static void Postfix(HealthHandler __instance)
        {
            if (LocalZoom.IsInOfflineModeAndNotSandbox || (!LocalZoom.enableCameraSetting && !LocalZoom.enableShaderSetting))
                return;
            
            if (__instance.GetComponent<PhotonView>().IsMine && !CardChoice.instance.IsPicking)
            {
                LocalZoom.instance.enableResetCamera = false;
                if (LocalZoom.enableCameraSetting)
                {
                    var camController = (MyCameraController)ControllerManager.CurrentCameraController;
                    camController.zoomLevel = Mathf.Clamp(
                        ControllerManager.DefaultZoom / 1.20f * (__instance.transform.localScale.x/1.15f), 0,
                        ControllerManager.DefaultZoom + ControllerManager.DefaultZoom / 4);
                }

                if (LocalZoom.enableShaderSetting)
                {
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
    }
    [HarmonyPatch(typeof(HealthHandler), "RPCA_Die_Phoenix")]
    public class HealthHandlerPhoenixPatch
    {
        public static void Postfix(HealthHandler __instance)
        {
            if (LocalZoom.IsInOfflineModeAndNotSandbox || (!LocalZoom.enableCameraSetting && !LocalZoom.enableShaderSetting))
                return;
            
            if (__instance.GetComponent<PhotonView>().IsMine && !CardChoice.instance.IsPicking)
            {
                if (LocalZoom.enableCameraSetting)
                {
                    LocalZoom.instance.enableResetCamera = false;
                }
                if (LocalZoom.enableShaderSetting && LocalZoom.instance.phoenixCircle != null)
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