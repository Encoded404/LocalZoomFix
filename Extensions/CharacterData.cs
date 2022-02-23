
using System.Runtime.CompilerServices;
using TMPro;

public class CharacterDataAdditionalData
{
    public TextMeshProUGUI PlayerNamePlate;
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
}
