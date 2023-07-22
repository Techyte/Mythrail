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

        private void Awake()
        {
            instance = this;

            _SettingsTab = (SettingsTab)tab;
        }

        private void Start()
        {
            LoadSettings();
            
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
        }

        private void LoadSettings()
        {
            
        }

        private void ToggleFullscreen(bool value)
        {
            _SettingsTab.Fullscreen(value);
        }

        private void VolumeChanged(float value)
        {
            _SettingsTab.Volume(value);
        }

        private void ToggleAskToInvite(bool value)
        {
            _SettingsTab.AskToInvite(value);
            alwaysInvite.interactable = !value;
        }

        private void ToggleAlwaysInvite(bool value)
        {
            _SettingsTab.ToggleAlwaysInvite(value);
        }
    }   
}