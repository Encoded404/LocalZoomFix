using HarmonyLib;
using UnboundLib;
using UnityEngine;

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
                });
            }
        }
    }
}