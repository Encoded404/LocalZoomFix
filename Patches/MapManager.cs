using HarmonyLib;
using UnboundLib;
using UnityEngine;

namespace LocalZoom.Patches
{
    [HarmonyPatch(typeof(MapManager), "OnLevelFinishedLoading")]
    public class MapManagerPatch
    {
        public static void Postfix(MapManager __instance)
        {
            if (LocalZoom.IsInOfflineModeAndNotSandbox || !MenuControllerHandler.instance.transform.Find("Visual/Rendering /FrontParticles").gameObject.activeSelf)
                return;
            foreach (var mask in MapManager.instance.currentMap.Map.GetComponentsInChildren<SpriteMask>(true))
            {
                if(mask.transform.parent.name.Contains("_Saw")) continue;
                LocalZoom.instance.MakeParticleRendererPortal(mask.gameObject);
            }
            
            // Do it again after 1 frame for mapExtended maps
            LocalZoom.instance.ExecuteAfterFrames(1, () =>
            {
                foreach (var mask in MapManager.instance.currentMap.Map.GetComponentsInChildren<SpriteMask>(true))
                {
                    if(mask.transform.parent.name.Contains("_Saw")) continue;
                    LocalZoom.instance.MakeParticleRendererPortal(mask.gameObject);
                }   
            });
        }
    }
}