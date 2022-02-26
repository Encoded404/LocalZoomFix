
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine.UI;

namespace LocalZoom.Extensions
{
    public class CharacterDataAdditionalData
    {
        public TextMeshProUGUI PlayerNamePlate;
        public List<Image> allWobbleImages = new List<Image>();
    }

    public static class CharacterDataExtension
    {
        private static readonly ConditionalWeakTable<CharacterData, CharacterDataAdditionalData> additionalData = new ConditionalWeakTable<CharacterData, CharacterDataAdditionalData>();
        
        public static CharacterDataAdditionalData GetData(this CharacterData instance)
        {
            return additionalData.GetOrCreateValue(instance);
        }
        
        public static void SetPlayerNamePlate(this CharacterData instance, TextMeshProUGUI playerNamePlate)
        {
            instance.GetData().PlayerNamePlate = playerNamePlate;
        }
        
        public static TextMeshProUGUI GetPlayerNamePlate(this CharacterData instance)
        {
            return instance.GetData().PlayerNamePlate;
        }

        public static List<Image> GetAllWobbleImages(this CharacterData instance)
        {
            return instance.GetData().allWobbleImages;
        }
    }
}

