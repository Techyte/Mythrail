using Mythrail.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Mythrail.MainMenu.Tabs.Settings
{
    public class SettingsTabUiManager : TabUiManager
    {
        private static SettingsTabUiManager instance;

        private SettingsTab _SettingsTab;

        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Toggle askToInvite;
        [SerializeField] private Toggle showDeveloperConsole;
        [SerializeField] private Toggle compressDeveloperConsole;

        [Space]
        
        [SerializeField] private bool defaultFullscreen;
        [SerializeField] [Range(0, 10)] private float defaultSensitivity;
        [SerializeField] private float defaultVolume = 2;
        [SerializeField] private bool defaultAskToInvite;
        [SerializeField] private bool defaultShowDeveloperConsole;
        [SerializeField] private bool defaultCompressDeveloperConsole;

        private void Awake()
        {
            instance = this;

            _SettingsTab = (SettingsTab)tab;
        }

        private void Start()
        {
            fullscreenToggle.onValueChanged.AddListener(delegate(bool arg0)
            {
                ToggleFullscreen(arg0);
            });
            
            sensitivitySlider.onValueChanged.AddListener(delegate(float arg0)
            {
                ChangeSensitivity((int)arg0);
            });
            
            volumeSlider.onValueChanged.AddListener(delegate(float arg0)
            {
                VolumeChanged(arg0);
            });
            
            askToInvite.onValueChanged.AddListener(delegate(bool arg0)
            {
                ToggleAskToInvite(arg0);
            });
            
            showDeveloperConsole.onValueChanged.AddListener(delegate(bool arg0)
            {
                ToggleShowDeveloperConsole(arg0);
            });
            
            compressDeveloperConsole.onValueChanged.AddListener(delegate(bool arg0)
            {
                ToggleCompressDeveloperConsole(arg0);
            });
            
            LoadSettings();
        }

        private void LoadSettings()
        {
            bool fullscreen = bool.Parse(PlayerPrefs.GetString("Fullscreen", defaultFullscreen.ToString()));
            float volume = PlayerPrefs.GetFloat("Volume", defaultVolume);
            float sensitivity = PlayerPrefs.GetFloat("Sensitivity", defaultSensitivity);
            bool askToInvite = bool.Parse(PlayerPrefs.GetString("AskToInvite", defaultAskToInvite.ToString()));
            bool showDeveloperConsole = bool.Parse(PlayerPrefs.GetString("ShowDeveloperConsole", defaultShowDeveloperConsole.ToString()));
            bool compressDeveloperConsole = bool.Parse(PlayerPrefs.GetString("CompressDeveloperConsole", defaultCompressDeveloperConsole.ToString()));

            volumeSlider.value = volume;
            fullscreenToggle.isOn = fullscreen;
            sensitivitySlider.value = sensitivity;
            this.askToInvite.isOn = askToInvite;
            this.showDeveloperConsole.isOn = showDeveloperConsole;
            this.compressDeveloperConsole.isOn = compressDeveloperConsole;
        }

        public void ToggleFullscreen(bool value)
        {
            _SettingsTab.Fullscreen(value);
        }

        public void ChangeSensitivity(int value)
        {
            _SettingsTab.ChangeSensitivity(value);
        }

        public void VolumeChanged(float value)
        {
            _SettingsTab.Volume(value);
        }

        public void ToggleAskToInvite(bool value)
        {
            _SettingsTab.AskToInvite(value);

            MythrailSettings.AlwaysInvite = false;
        }

        public void ToggleShowDeveloperConsole(bool value)
        {
            _SettingsTab.ToggleShowDeveloperConsole(value);
        }

        public void ToggleCompressDeveloperConsole(bool value)
        {
            _SettingsTab.ToggleCompressDeveloperConsole(value);
        }
    }   
}