using System;
using HarmonyLib;
using LocalZoom.Extensions;
using System.Reflection;
using InControl;

namespace LocalZoom.Patches
{
    // postfix PlayerActions constructor to add controls
    [HarmonyPatch(typeof(PlayerActions))]
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPatch(new Type[] { })]
    class PlayerActionsPatchPlayerActions
    {
        private static void Postfix(PlayerActions __instance)
        {
            __instance.GetAdditionalData().zoomIn = (PlayerAction)typeof(PlayerActions).InvokeMember("CreatePlayerAction",
                                    BindingFlags.Instance | BindingFlags.InvokeMethod |
                                    BindingFlags.NonPublic, null, __instance, new object[] { "Zoom In" });
            __instance.GetAdditionalData().zoomOut = (PlayerAction)typeof(PlayerActions).InvokeMember("CreatePlayerAction",
                                    BindingFlags.Instance | BindingFlags.InvokeMethod |
                                    BindingFlags.NonPublic, null, __instance, new object[] { "Zoom Out" });
            __instance.GetAdditionalData().modifier = (PlayerAction)typeof(PlayerActions).InvokeMember("CreatePlayerAction",
                                    BindingFlags.Instance | BindingFlags.InvokeMethod |
                                    BindingFlags.NonPublic, null, __instance, new object[] { "Enable Zoom" });


        }
    }
    // postfix PlayerActions to add controller controls
    [HarmonyPatch(typeof(PlayerActions), "CreateWithControllerBindings")]
    class PlayerActionsPatchCreateWithControllerBindings
    {
        private static void Postfix(ref PlayerActions __result)
        {
            // there's not enough buttons on controllers... so we add a modifier to create a combination

            // zoomIn is RightBumper + d-pad up
            // zoomOut is RightBumper + d-pad down

            __result.Jump.RemoveBinding(new DeviceBindingSource(InputControlType.RightBumper));
            __result.GetAdditionalData().modifier.AddDefaultBinding(InputControlType.RightBumper);

            __result.GetAdditionalData().zoomIn.AddDefaultBinding(InputControlType.DPadUp);

            __result.GetAdditionalData().zoomOut.AddDefaultBinding(InputControlType.DPadDown);
        }
    }
    // postfix PlayerActions to add keyboard controls
    [HarmonyPatch(typeof(PlayerActions), "CreateWithKeyboardBindings")]
    class PlayerActionsPatchCreateWithKeyboardBindings
    {
        private static void Postfix(ref PlayerActions __result)
        {
            __result.GetAdditionalData().zoomIn.AddDefaultBinding(Mouse.PositiveScrollWheel);
            __result.GetAdditionalData().zoomOut.AddDefaultBinding(Mouse.NegativeScrollWheel);
        }
    }
}
