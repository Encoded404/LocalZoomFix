using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine.UI;
using System.Linq;

namespace LocalZoom.Extensions
{
    public class CharacterDataAdditionalData
    {
        public TextMeshProUGUI PlayerNamePlate;
        public List<Image> allWobbleImages = new List<Image>();
        public Transform[] leftLegJointsSorted = null;
        public Transform[] rightLegJointsSorted = null;
        public Transform[] leftArmJointsSorted = null;
        public Transform[] rightArmJointsSorted = null;
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

        public static Transform[] GetSortedLegJoints(this CharacterData instance, bool left)
        {
            if (left)
            {
                if (instance.GetData().leftLegJointsSorted is null)
                {
                    Transform leftLeg = instance.transform.Find("Limbs/LegStuff/LegRendererLeft");
                    instance.GetData().leftLegJointsSorted = leftLeg.GetComponentsInChildren<Transform>(true).Where(j => j.name == "Joint(Clone)").OrderBy(j => (j.position - instance.transform.position).sqrMagnitude).ToArray();
                }
                return instance.GetData().leftLegJointsSorted;
            }
            else
            {
                if (instance.GetData().rightLegJointsSorted is null)
                {
                    Transform rightLeg = instance.transform.Find("Limbs/LegStuff/LegRendererRight");
                    instance.GetData().rightLegJointsSorted = rightLeg.GetComponentsInChildren<Transform>(true).Where(j => j.name == "Joint(Clone)").OrderBy(j => (j.position - instance.transform.position).sqrMagnitude).ToArray();
                }
                return instance.GetData().rightLegJointsSorted;
            }
        }
        public static Transform[] GetSortedArmJoints(this CharacterData instance, bool left)
        {
            if (left)
            {
                if (instance.GetData().leftArmJointsSorted is null)
                {
                    Transform leftArm = instance.transform.Find("Limbs/ArmStuff/ArmRendererLeft");
                    instance.GetData().leftArmJointsSorted = leftArm.GetComponentsInChildren<Transform>(true).Where(j => j.name == "Joint(Clone)").OrderBy(j => (j.position - instance.transform.position).sqrMagnitude).ToArray();
                }
                return instance.GetData().leftArmJointsSorted;
            }
            else
            {
                if (instance.GetData().rightArmJointsSorted is null)
                {
                    Transform rightArm = instance.transform.Find("Limbs/ArmStuff/ArmRendererRight");
                    instance.GetData().rightArmJointsSorted = rightArm.GetComponentsInChildren<Transform>(true).Where(j => j.name == "Joint(Clone)").OrderBy(j => (j.position - instance.transform.position).sqrMagnitude).ToArray();
                }
                return instance.GetData().rightArmJointsSorted;
            }
        }
    }
}

