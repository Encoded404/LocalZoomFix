using System;
using HarmonyLib;
using UnityEngine;

namespace LocalZoom.Patches
{
    [HarmonyPatch(typeof(ApplyCardStats),"ApplyStats")]
    class ApplyCardStatsPatchSpawn
    {
        private static void Postfix(ApplyCardStats __instance, Player ___playerToUpgrade)
        {
            if (LocalZoom.IsInOfflineModeAndNotSandbox || !LocalZoom.enableShaderSetting)
                return;
            if (___playerToUpgrade != null)
            {
                var stats = ___playerToUpgrade.GetComponent<CharacterStatModifiers>();
                if (stats.objectsAddedToPlayer.Count > 0)
                {
                    var obj = stats.objectsAddedToPlayer[stats.objectsAddedToPlayer.Count - 1];
                    LocalZoom.MakeObjectHidden(obj.transform);
                }
            }
        }
    }
}