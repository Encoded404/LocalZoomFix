using HarmonyLib;
using UnityEngine;

namespace LocalZoom.Patches
{
    [HarmonyPatch(typeof(DamageBox), "Start")]
    public class DamageBoxPatch
    {
        public static void Postfix(DamageBox __instance)
        {
            if (LocalZoom.IsInOfflineModeAndNotSandbox || !LocalZoom.enableCameraSetting)
                return;
            
            if (__instance.name.Contains("_Saw"))
            {
                __instance.GetComponentInChildren<ParticleSystemRenderer>().enabled = false;
                __instance.GetComponentInChildren<ParticleSystem>().Stop();
                __instance.GetComponentInChildren<SpriteRenderer>().enabled = true;
                var particles = __instance.GetComponentsInChildren<ParticleSystemRenderer>();
                LocalZoom.MakeObjectHidden(particles[particles.Length - 1]);
            }
        }
    }
}