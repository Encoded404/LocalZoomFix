using HarmonyLib;
using LocalZoom.Extensions;
using TMPro;
using UnboundLib;

namespace LocalZoom.Patches
{
    [HarmonyPatch(typeof(CharacterData), "Start")]
    public class CharacterDataPatch
    {
        public static void Postfix(CharacterData __instance)
        {
            __instance.SetPlayerNamePlate(__instance.transform.Find("WobbleObjects/Healthbar/Canvas/PlayerName").GetComponent<TextMeshProUGUI>());
        }
    }
}