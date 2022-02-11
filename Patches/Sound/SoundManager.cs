using System;
using HarmonyLib;
using Sonigon;
using Sonigon.Internal;
using UnboundLib;
using UnityEngine;

namespace LocalZoom.Patches
{
    [HarmonyPatch(typeof(SoundManager), "Play")]
    [HarmonyPatch(new Type[] {typeof(SoundEvent), typeof(Transform)})]
    public class SoundManagerPlayPatch
    {
        public static bool Prefix(SoundManager __instance, SoundEvent soundEvent, Transform owner)
        {
            if (LocalZoom.IsInOfflineModeAndNotSandbox || !LocalZoom.enableShaderSetting || owner == null || soundEvent == null)
                return true;
            if (owner == SoundManager.Instance.GetTransform())
            {
                return true;
            }

            if (!(__instance.GetFieldValue("data") is SoundManagerData data))
            {
                UnityEngine.Debug.LogError("SoundManagerData is null");
                return true;
            }
            data.PlaySoundEvent(soundEvent, PlayType.Play, owner, null, owner, null, null, null, null, null);

            return false;
        }
    }
}