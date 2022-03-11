using System;
using System.Runtime.CompilerServices;
using InControl;

namespace LocalZoom.Extensions
{
    // this extension stores ONLY the data for additional player actions
    // additional actions are assigned in the PlayerActions patches
    public class PlayerActionsAdditionalData
    {
        public PlayerAction zoomIn;
        public PlayerAction zoomOut;
        public PlayerAction modifier;
    }
    public static class PlayerActionsExtension
    {
        public static readonly ConditionalWeakTable<PlayerActions, PlayerActionsAdditionalData> data =
            new ConditionalWeakTable<PlayerActions, PlayerActionsAdditionalData>();

        public static PlayerActionsAdditionalData GetAdditionalData(this PlayerActions playerActions)
        {
            return data.GetOrCreateValue(playerActions);
        }

        public static void AddData(this PlayerActions playerActions, PlayerActionsAdditionalData value)
        {
            try
            {
                data.Add(playerActions, value);
            }
            catch (Exception) { }
        }

        public static float PlayerZoom(this PlayerActions playerActions)
        {
            if (playerActions.Device != null && playerActions.Device.DeviceClass == InputDeviceClass.Controller)
            {
                if (playerActions.GetAdditionalData().modifier.IsPressed && playerActions.GetAdditionalData().zoomIn.IsPressed) { return -0.2f; }
                else if (playerActions.GetAdditionalData().modifier.IsPressed && playerActions.GetAdditionalData().zoomOut.IsPressed) { return +0.2f; }
                else { return 0; }

            }
            else
            {
                if (playerActions.GetAdditionalData().zoomIn.IsPressed) { return -1f; }
                else if (playerActions.GetAdditionalData().zoomOut.IsPressed) { return +1f; }
                else { return 0; }
            }
        }
    }
}
