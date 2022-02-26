using System.Linq;
using HarmonyLib;
using LocalZoom.Extensions;
using UnboundLib;
using UnityEngine;
using UnityEngine.UI;

namespace LocalZoom.Patches
{
    [HarmonyPatch(typeof(HealthBar), "Start")]
    public class HealthBarPatch
    {
        public static void Postfix(HealthBar __instance)
        {
            if (LocalZoom.enableLoSNamePlates)
            {
                LocalZoom.instance.ExecuteAfterSeconds(7, () =>
                {
                    __instance.transform.GetChild(0).GetComponent<Canvas>().sortingLayerName = "MostFront";
                    __instance.transform.root.GetComponent<CharacterData>()
                        .GetData().allWobbleImages.AddRange(__instance.transform.parent.GetComponentsInChildren<Image>(true));
                });
            }
        }
    }
}