using System.Linq;
using HarmonyLib;
using TMPro;
using UnboundLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LocalZoom.Patches
{
    [HarmonyPatch(typeof(GeneralParticleSystem), "Play")]
    public class GeneralParticleSystemPatch
    {
        public static string[] names = new[] { "RoundStartText", "Join", "CONTINUE" ,"REMATCH", "EXIT"};
        
        public static void Postfix(GeneralParticleSystem __instance)
        {
            if (LocalZoom.IsInOfflineModeAndNotSandbox || !LocalZoom.enableCameraSetting)
                return;
            
            if (GeneralParticleSystemPatch.names.Any(n=>n == __instance.transform.parent.name))
            {
                for (int i = 0; i < __instance.transform.childCount; i++)
                {
                    var child = __instance.transform.GetChild(i);
                    child.gameObject.SetActive(false);
                }
                __instance.gameObject.GetComponentInParent<UnityEngine.UI.Mask>().enabled = false;
                __instance.gameObject.GetComponentInParent<TextMeshProUGUI>().color = new Color(0.9f, 0.9f, 0.9f);
                
                __instance.endEvent.AddListener(() =>
                {
                    foreach (var text in __instance.transform.parent.GetComponentsInChildren<TextMeshProUGUI>())
                    {
                        text.enabled = false;
                    }
                });
            }
        }
    }

    [HarmonyPatch(typeof(GeneralParticleSystem), "EndLooping")]
    public class GeneralParticleSystemEndLoopingPatch
    {
        public static void Postfix(GeneralParticleSystem __instance)
        {
            if (LocalZoom.IsInOfflineModeAndNotSandbox || !LocalZoom.enableCameraSetting)
                return;
            
            if (GeneralParticleSystemPatch.names.Any(n=>n == __instance.transform.parent.name))
            {
                foreach (var text in __instance.transform.parent.GetComponentsInChildren<TextMeshProUGUI>())
                {
                    text.enabled = false;
                }
            }
        }
    }
    [HarmonyPatch(typeof(GeneralParticleSystem), "Stop")]
    public class GeneralParticleSystemStopPatch
    {
        public static void Postfix(GeneralParticleSystem __instance)
        {
            if (LocalZoom.IsInOfflineModeAndNotSandbox || !LocalZoom.enableCameraSetting)
                return;
            
            if (GeneralParticleSystemPatch.names.Any(n=>n == __instance.transform.parent.name))
            {
                foreach (var text in __instance.transform.parent.GetComponentsInChildren<TextMeshProUGUI>())
                {
                    text.enabled = false;
                }
            }
        }
    }
}