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
        [SerializeField] private Toggle alwaysInvite;
        [SerializeField] private Toggle showDeveloperConsole;
        [SerializeField] private Toggle compressDeveloperConsole;

        [Space]
        
        [SerializeField] private bool defaultFullscreen;
        [SerializeField] [Range(0, 10)] private float defaultSensitivity;
        [SerializeField] private float defaultVolume = 2;
        [SerializeField] private bool defaultAskToInvite;
        [SerializeField] private bool defaultAlwaysInvite;
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
            
            alwaysInvite.onValueChanged.AddListener(delegate(bool arg0)
            {
                ToggleAlwaysInvite(arg0);
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
            if (!PlayerPrefs.HasKey("Fullscreen"))
            {
                PlayerPrefs.SetString("Fullscreen", defaultFullscreen.ToString());
            }
            if (!PlayerPrefs.HasKey("Sensitivity"))
            {
                PlayerPrefs.SetFloat("Sensitivity", defaultSensitivity);
            }
            if (!PlayerPrefs.HasKey("Volume"))
            {
                PlayerPrefs.SetFloat("Volume", defaultVolume);
            }
            if (!PlayerPrefs.HasKey("AskToInvite"))
            {
                PlayerPrefs.SetString("AskToInvite", defaultAskToInvite.ToString());
            }
            if (!PlayerPrefs.HasKey("AlwaysInvite"))
            {
                PlayerPrefs.SetString("AlwaysInvite", defaultAlwaysInvite.ToString());
            }
            if (!PlayerPrefs.HasKey("ShowDeveloperConsole"))
            {
                PlayerPrefs.SetString("ShowDeveloperConsole", defaultShowDeveloperConsole.ToString());
            }
            if (!PlayerPrefs.HasKey("CompressDeveloperConsole"))
            {
                PlayerPrefs.SetString("CompressDeveloperConsole", defaultCompressDeveloperConsole.ToString());
            }
            
            bool fullscreen = bool.Parse(PlayerPrefs.GetString("Fullscreen"));
            float sensitivity = PlayerPrefs.GetFloat("Sensitivity");
            bool askToInvite = bool.Parse(PlayerPrefs.GetString("AskToInvite"));
            bool alwaysInvite = bool.Parse(PlayerPrefs.GetString("AlwaysInvite"));
            bool showDeveloperConsole = bool.Parse(PlayerPrefs.GetString("ShowDeveloperConsole"));
            bool compressDeveloperConsole = bool.Parse(PlayerPrefs.GetString("CompressDeveloperConsole"));

            fullscreenToggle.isOn = fullscreen;
            sensitivitySlider.value = sensitivity;
            this.askToInvite.isOn = askToInvite;
            this.alwaysInvite.isOn = alwaysInvite;
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