using UnityEngine;

namespace Mythrail.MainMenu.Tabs.Settings
{
    public class SettingsTab : Tab
    {
        private static SettingsTab instance;

        [SerializeField] private AudioSource audioSource;

        private void Awake()
        {
            instance = this;
        }

        public void Fullscreen(bool value)
        {
            Screen.fullScreen = value;
            PlayerPrefs.SetInt("Fullscreen", value ? 1 : 0);
        }

        public void Volume(float value)
        {
            //TODO: volume slider stuff
        }

        public void AskToInvite(bool value)
        {
            PlayerPrefs.SetInt("AskToInvite", value ? 1 : 0);
        }

        public void ToggleAlwaysInvite(bool value)
        {
            PlayerPrefs.SetInt("AlwaysInvite", value ? 1 : 0);
        }
    }
}