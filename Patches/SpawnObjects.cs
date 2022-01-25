using System;
using HarmonyLib;
using UnityEngine;

namespace LocalZoom.Patches
{
    [HarmonyPatch(typeof(SpawnObjects),"ConfigureObject")]
    class SpawnObjectsPatchSpawn
    {
        private static void Postfix(SpawnObjects __instance, GameObject go)
        {
            if (go != null)
            {
                LocalZoom.MakeObjectHidden(go.transform);
            }
        }
    }
}