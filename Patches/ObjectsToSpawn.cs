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
            foreach (GameObject obj in __result)
            {
                if (obj != null) { 
                    LocalZoom.MakeObjectHidden(obj.transform);
                    obj.AddComponent<TestMono>();
                    GameObject.Destroy(obj.GetComponentInChildren<ChangeColor>());
                }
            }
        }
    }
    
    internal class TestMono : MonoBehaviour
    {
    }
}