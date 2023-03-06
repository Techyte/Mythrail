using System;
using System.Collections;
using System.Collections.Generic;
using Mythrail.Multiplayer;
using Mythrail.Players;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Mythrail.Game
{
    public class UIManager : MonoBehaviour
    {
        private static UIManager _singleton;
        public static UIManager Singleton
        {
            get => _singleton;
            private set
            {
                if (_singleton == null)
                    _singleton = value;
                else if (_singleton != value)
                {
                    //Debug.Log($"{nameof(UIManager)} instance already exists, destroying duplicate!");
                    Destroy(value);
                }
            }
        }

        [Header("Connect")]
        [SerializeField] private Transform UIHealthBar;

        public GameObject loadingScreen;
        
        public TextMeshProUGUI KillsText;
        public TextMeshProUGUI DeathsText;
        public TextMeshProUGUI HUDUsernameDisplay;
        public TextMeshProUGUI LoadingStatusDisplay;
        public TextMeshProUGUI GunName;
        public Button CodeDisplay;
        public GameObject RespawningScreen;
        public GameObject PlayScreen;
        public TextMeshProUGUI CountdownText;
        public Button RespawnButton;

        private int countdown;

        private void Awake()
        {
            Singleton = this;
            
            SceneManager.sceneLoaded += LoadedGame;
        }

        private void LoadedGame(Scene scene, LoadSceneMode loadSceneMode)
        {
            if(scene.name == "BattleFeild")
            {
                UIHealthBar = GameObject.Find("Health").GetComponentsInChildren<Image>()[1].transform;
                loadingScreen = GameObject.Find("Loading");
                KillsText = GameObject.Find("KillCounter").GetComponent<TextMeshProUGUI>();
                DeathsText = GameObject.Find("DeathCounter").GetComponent<TextMeshProUGUI>();
                HUDUsernameDisplay = GameObject.Find("UsernameText").GetComponent<TextMeshProUGUI>();
                LoadingStatusDisplay = GameObject.Find("Loading...").GetComponent<TextMeshProUGUI>();
                GunName = GameObject.Find("GunName").GetComponent<TextMeshProUGUI>();
                CodeDisplay = GameObject.Find("CodeDisplay").GetComponent<Button>();
                CodeDisplay.GetComponentInChildren<TextMeshProUGUI>().text = NetworkManager.Singleton.code;
                CodeDisplay.onClick.AddListener(CopyCode);
                RespawningScreen = GameObject.Find("RespawnScreen");
                PlayScreen = GameObject.Find("PlayScreen");
                CountdownText = GameObject.Find("CountdownText").GetComponent<TextMeshProUGUI>();
                RespawnButton = GameObject.Find("RespawnButton").GetComponent<Button>();
                RespawnButton.onClick.AddListener(Respawn);
            }
        }

        public void CanRespawn()
        {
            RespawnButton.interactable = true;
            CountdownText.text = "CAN RESPAWN";
        }

        public void OpenRespawnScreen(int countdownTime)
        {
            PlayScreen.SetActive(false);
            RespawningScreen.SetActive(true);
            CountdownText.text = $"RESPAWNING IN {countdownTime}";
            countdown = countdownTime;
            StartCoroutine(CountDown());
        }

        private IEnumerator CountDown()
        {
            int initialCountdown = countdown;
            
            for (int i = initialCountdown; i > 0; i--)
            {
                yield return new WaitForSeconds(1);
                countdown--;
                CountdownText.text = $"RESPAWNING IN {i}";
            }
        }

        public void Respawn()
        {
            
        }

        private void Update()
        {
            if (Player.LocalPlayer==null) return;
            RefreshHealthBar();
        }

        void RefreshHealthBar()
        {
            float healthRatio = (float)Player.LocalPlayer.currentHealth / Player.LocalPlayer.maxHealth;
            UIHealthBar.localScale = Vector3.Lerp(UIHealthBar.localScale, new Vector3(healthRatio, 1, 1), Time.deltaTime * 8f);

            UIHealthBar.GetComponent<Image>().color = Player.LocalPlayer.currentHealth <= 10 ? Color.red : Color.green;
        }

        public void CopyCode()
        {
            GUIUtility.systemCopyBuffer = NetworkManager.Singleton.code;
        }
    }

}