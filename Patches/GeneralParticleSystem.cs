using HarmonyLib;
using TMPro;
using UnboundLib;
using UnityEngine;
using UnityEngine.UI;

namespace LocalZoom.Patches
{
    [HarmonyPatch(typeof(GeneralParticleSystem), "Play")]
    public class GeneralParticleSystemPatch
    {
        public static void Postfix(GeneralParticleSystem __instance)
        {
            if (__instance.transform.parent.name == "RoundStartText")
            {
                __instance.transform.GetChild(0).gameObject.SetActive(false);
                __instance.gameObject.GetComponentInParent<UnityEngine.UI.Mask>().enabled = false;
                __instance.gameObject.GetComponentInParent<TextMeshProUGUI>().color = new Color(0.9f, 0.9f, 0.9f);
            }
        }
    }
}