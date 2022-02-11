using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine.Events;
using UnityEngine.UI.ProceduralImage;

namespace LocalZoom.Patches
{
    [HarmonyPatch(typeof(ProceduralImage), "Init")]
    public class ProceduralImagePatch
    {
        public static bool Prefix(ProceduralImage __instance, ref UnityAction ___m_OnDirtyVertsCallback)
        {
            if (LocalZoom.IsInOfflineModeAndNotSandbox || !LocalZoom.enableShaderSetting)
                return true;
            
            var method = typeof(ProceduralImage).GetMethod("OnVerticesDirty",BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null)
            {
                UnityEngine.Debug.LogWarning("method is null?");
                return false;
            }
            ___m_OnDirtyVertsCallback += () => { method.Invoke(__instance, null); };
            __instance.preserveAspect = false;
            if (__instance.sprite == null)
            {
                __instance.sprite = EmptySprite.Get();
            }

            return false;
        }
    }
}