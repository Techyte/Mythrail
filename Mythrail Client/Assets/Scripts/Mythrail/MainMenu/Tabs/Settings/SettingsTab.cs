using Mythrail.Audio;
using Mythrail.General;
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

        public void ChangeSensitivity(int value)
        {
            PlayerPrefs.SetFloat("Sensitivity", value);
            MythrailSettings.MouseSensitivity = value;
        }

        public void Volume(float value)
        {
            PlayerPrefs.SetFloat("Volume", value);
            AudioManager.instance.ChangeVolume(value);
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

        public void ToggleShowDeveloperConsole(bool value)
        {
            PlayerPrefs.SetString("ShowDeveloperConsole", value.ToString());
            MythrailSettings.ShowDeveloperConsole = value;
            InGameConsoleManager.Instance.SetShowConsole(value);
        }

        public void ToggleCompressDeveloperConsole(bool value)
        {
            PlayerPrefs.SetString("CompressDeveloperConsole", value.ToString());
            MythrailSettings.CompressDeveloperConsole = value;
            InGameConsoleManager.Instance.RecalculateCurrentCompression();
        }
    }
}