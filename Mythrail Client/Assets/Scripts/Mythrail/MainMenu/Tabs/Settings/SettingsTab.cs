using UnityEngine;
using Mythrail.Settings;

namespace Mythrail.MainMenu.Tabs.Settings
{
    public class SettingsTab : Tab
    {
        public void Fullscreen(bool value)
        {
            Screen.fullScreen = value;
            Debug.Log(value);
            PlayerPrefs.SetString("Fullscreen", value.ToString());
            MythrailSettings.Fullscreen = value;
        }

        public void Volume(float value)
        {
            //TODO: volume slider stuff
        }

        public void AskToInvite(bool value)
        {
            PlayerPrefs.SetString("AskToInvite", value.ToString());
            MythrailSettings.AskToInvite = value;
        }

        public void ToggleAlwaysInvite(bool value)
        {
            PlayerPrefs.SetString("AlwaysInvite", value.ToString());
            MythrailSettings.AlwaysInvite = value;
        }
    }
}