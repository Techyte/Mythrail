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
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Toggle askToInvite;
        [SerializeField] private Toggle alwaysInvite;

        [SerializeField] private bool defaultFullscreen;
        [SerializeField] private bool defaultAskToInvite;
        [SerializeField] private bool defaultAlwaysInvite;

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
            
            volumeSlider.onValueChanged.AddListener(delegate(float arg0)
            {
                VolumeChanged(arg0);
            });
            
            askToInvite.onValueChanged.AddListener(delegate(bool arg0)
            {
                ToggleAskToInvite(arg0);
            });
            
            alwaysInvite.onValueChanged.AddListener(delegate(bool arg0)
            {
                ToggleAlwaysInvite(arg0);
            });
            
            LoadSettings();
        }

        private void LoadSettings()
        {
            if (!PlayerPrefs.HasKey("Fullscreen"))
            {
                PlayerPrefs.SetString("Fullscreen", defaultFullscreen.ToString());
            }
            if (!PlayerPrefs.HasKey("AskToInvite"))
            {
                PlayerPrefs.SetString("AskToInvite", defaultAskToInvite.ToString());
            }
            if (!PlayerPrefs.HasKey("AlwaysInvite"))
            {
                PlayerPrefs.SetString("AlwaysInvite", defaultAlwaysInvite.ToString());
            }
            
            bool fullscreen = bool.Parse(PlayerPrefs.GetString("Fullscreen"));
            bool askToInvite = bool.Parse(PlayerPrefs.GetString("AskToInvite"));
            bool alwaysInvite = bool.Parse(PlayerPrefs.GetString("AlwaysInvite"));

            fullscreenToggle.isOn = fullscreen;
            this.askToInvite.isOn = askToInvite;
            this.alwaysInvite.isOn = alwaysInvite;
        }

        public void ToggleFullscreen(bool value)
        {
            _SettingsTab.Fullscreen(value);
        }

        public void VolumeChanged(float value)
        {
            _SettingsTab.Volume(value);
        }

        public void ToggleAskToInvite(bool value)
        {
            _SettingsTab.AskToInvite(value);
            
            if(value)
                alwaysInvite.isOn = false;

            MythrailSettings.AlwaysInvite = false;
        }

        public void ToggleAlwaysInvite(bool value)
        {
            _SettingsTab.ToggleAlwaysInvite(value);

            if(value)
                askToInvite.isOn = false;
            
            Debug.Log("always invite toggled");
        }
    }   
}