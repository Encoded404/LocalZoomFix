using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace LocalZoom.Patches
{
    [HarmonyPatch(typeof(CardBar))]
    public class CardBarPatch
    {
        [HarmonyPatch("OnHover")]
        public static bool Prefix(CardBar __instance, ref GameObject ___currentCard, CardInfo card)
        {
            if (LocalZoom.IsInOfflineModeAndNotSandbox || !LocalZoom.enableCameraSetting)
                return true;
            
            if (___currentCard)
            {
                Object.Destroy(___currentCard);
            }
            ___currentCard = CardChoice.instance.AddCardVisual(card, __instance.cardPos.transform.position);
            Collider2D[] colliders = ___currentCard.transform.root.GetComponentsInChildren<Collider2D>();
            foreach (var col in colliders)
            {
                col.enabled = false;
            }
            ___currentCard.GetComponentInChildren<Canvas>().sortingLayerName = "MostFront";
            ___currentCard.GetComponentInChildren<GraphicRaycaster>().enabled = false;
            ___currentCard.GetComponentInChildren<SetScaleToZero>().enabled = false;
            ___currentCard.transform.localScale = Vector3.one * MapManager.instance.currentMap.Map.size/20f;
            return false;
        }
        
        [HarmonyPatch("Update")]
        public static void Prefix(CardBar __instance, ref GameObject ___currentCard)
        {
            if (LocalZoom.IsInOfflineModeAndNotSandbox || !LocalZoom.enableCameraSetting)
                return;
            
            if (___currentCard)
            {
                ___currentCard.transform.localScale = Vector3.Lerp(___currentCard.transform.localScale, Vector3.one * MapManager.instance.currentMap.Map.size/20f, Time.deltaTime*5f);
                ___currentCard.transform.position = __instance.cardPos.transform.position;
            }
        }
    }
}