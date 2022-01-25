using System.Collections;
using Photon.Pun;
using UnboundLib;
using UnboundLib.GameModes;
using UnityEngine;

namespace LocalZoom
{
    public static class Numerators
    {
        public static IEnumerator RoundEnd(IGameModeHandler gm)
        {
            if (!(GM_Test.instance && GM_Test.instance.gameObject.activeSelf) && PhotonNetwork.OfflineMode)
                yield break;
            LocalZoom.instance.enableResetCamera = false;
            yield break;
        }

        public static IEnumerator RoundStart(IGameModeHandler gm)
        {
            // If not in sandbox and in offlinemode return
            if (!(GM_Test.instance && GM_Test.instance.gameObject.activeSelf) && PhotonNetwork.OfflineMode)
                yield break;
            // SetCameraPosition(Vector3.zero, true);

            yield break;
        }

        public static IEnumerator PointStart(IGameModeHandler gm)
        {
            if (!(GM_Test.instance && GM_Test.instance.gameObject.activeSelf) && PhotonNetwork.OfflineMode)
                yield break;
            LocalZoom.instance.ExecuteAfterSeconds(1f, () =>
            {
                MapManager.instance.currentMap.Map.size = LocalZoom.defaultMapSize/2f;
            });
            yield break;
        }

        public static IEnumerator StartPickPhase(IGameModeHandler gm)
        {
            if (!(GM_Test.instance && GM_Test.instance.gameObject.activeSelf) && PhotonNetwork.OfflineMode)
                yield break;
            LocalZoom.instance.enableCamera = false;
            LocalZoom.instance.enableResetCamera = false;
            yield break;
        }
        public static IEnumerator EndPickPhase(IGameModeHandler gm)
        {
            if (!(GM_Test.instance && GM_Test.instance.gameObject.activeSelf) && PhotonNetwork.OfflineMode)
                yield break;
            LocalZoom.instance.StartCoroutine(DisableCameraTemp());
            LocalZoom.instance.enableResetCamera = true;
            yield break;
        }

        public static IEnumerator DisableCameraTemp()
        {
            if (!(GM_Test.instance && GM_Test.instance.gameObject.activeSelf) && PhotonNetwork.OfflineMode)
                yield break;
            LocalZoom.instance.enableCamera = false;
            yield return new WaitForSeconds(1);
            LocalZoom.instance.enableCamera = true;
        }

        public static IEnumerator GameStarted(IGameModeHandler gm)
        {

            yield break;
        }
    }
}