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
            if (LocalZoom.IsInOfflineModeAndNotSandbox)
                yield break;
            LocalZoom.instance.ExecuteAfterFrames(1, () =>
            {
                LocalZoom.instance.enableResetCamera = false;
            });
            yield break;
        }

        public static IEnumerator RoundStart(IGameModeHandler gm)
        {
            // If not in sandbox and in offlinemode return
            if (LocalZoom.IsInOfflineModeAndNotSandbox)
                yield break;
            // SetCameraPosition(Vector3.zero, true);

            yield break;
        }

        public static IEnumerator PointStart(IGameModeHandler gm)
        {
            if (LocalZoom.IsInOfflineModeAndNotSandbox)
                yield break;
            LocalZoom.instance.ExecuteAfterSeconds(1f, () =>
            {
                MapManager.instance.currentMap.Map.size = LocalZoom.defaultMapSize/2f;
            });
            yield break;
        }
        
        public static IEnumerator PointEnd(IGameModeHandler gm)
        {
            if (LocalZoom.IsInOfflineModeAndNotSandbox)
                yield break;
            LocalZoom.instance.enableResetCamera = true;
            yield break;
        }

        public static IEnumerator StartPickPhase(IGameModeHandler gm)
        {
            if (LocalZoom.IsInOfflineModeAndNotSandbox)
                yield break;
            LocalZoom.instance.enableCamera = false;
            LocalZoom.instance.enableResetCamera = false;
            yield break;
        }
        public static IEnumerator EndPickPhase(IGameModeHandler gm)
        {
            if (LocalZoom.IsInOfflineModeAndNotSandbox)
                yield break;
            LocalZoom.instance.StartCoroutine(DisableCameraTemp());
            LocalZoom.instance.enableResetCamera = true;
            yield break;
        }

        public static IEnumerator DisableCameraTemp()
        {
            if (LocalZoom.IsInOfflineModeAndNotSandbox)
                yield break;
            LocalZoom.instance.enableCamera = false;
            yield return new WaitForSeconds(1);
            LocalZoom.instance.enableCamera = true;
        }

        public static IEnumerator GameStarted(IGameModeHandler gm)
        {
            if (LocalZoom.IsInOfflineModeAndNotSandbox)
                yield break;
            UnityEngine.Debug.Log("gamestarted");
            LocalZoom.instance.ExecuteAfterSeconds(5, () =>
            {
                UnityEngine.Debug.Log("players hidden");
                LocalZoom.instance.MakeAllPlayersHidden();
                LocalZoom.instance.GiveLocalPlayerViewCone();
                LocalZoom.instance.MakeAllParticlesHidden();
            });
            yield break;
        }
    }
}