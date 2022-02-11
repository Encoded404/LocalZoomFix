using System;
using HarmonyLib;
using UnityEngine;

namespace LocalZoom.Patches
{
    [HarmonyPatch(typeof(ObjectsToSpawn),"SpawnObject")]
    [HarmonyPatch(new Type[] { typeof(Transform), typeof(HitInfo), typeof(ObjectsToSpawn), typeof(HealthHandler), typeof(PlayerSkin), typeof(float), typeof(SpawnedAttack), typeof(bool) })]
    class ObjectsToSpawnPatchSpawnObject
    {
        private static void Postfix(ref GameObject[] __result)
        {
            if (LocalZoom.IsInOfflineModeAndNotSandbox || !LocalZoom.enableShaderSetting)
                return;
            
            foreach (GameObject obj in __result)
            {
                if (obj != null) { 
                    LocalZoom.MakeObjectHidden(obj.transform);
                    GameObject.Destroy(obj.GetComponentInChildren<ChangeColor>());
                }
            }
        }
    }
    
    [HarmonyPatch(typeof(ObjectsToSpawn),"SpawnObject")]
    [HarmonyPatch(new Type[] { typeof(ObjectsToSpawn), typeof(Vector3), typeof(Quaternion) })]
    class ObjectsToSpawnPatchSpawnObject2
    {
        private static void Postfix(ObjectsToSpawn objectToSpawn)
        {
            if (LocalZoom.IsInOfflineModeAndNotSandbox || !LocalZoom.enableShaderSetting)
                return;
            
            var obj = objectToSpawn.effect;
            if (obj != null) { 
                LocalZoom.MakeObjectHidden(obj.transform);
                GameObject.Destroy(obj.GetComponentInChildren<ChangeColor>());
            }
            
        }
    }
}