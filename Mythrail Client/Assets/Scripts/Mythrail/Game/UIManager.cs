using System.Collections;
using Mythrail.Multiplayer;
using Mythrail.Players;
using Riptide;
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
        public TextMeshProUGUI PingText;
        public TextMeshProUGUI StartingText;

        private int countdown;

        private void Awake()
        {
            Singleton = this;
            
            SceneManager.sceneLoaded += LoadedGame;
        }

        private void LoadedGame(Scene scene, LoadSceneMode loadSceneMode)
        {
            SetPingText();
            if(scene.name == "BattleFeild")
            {
                UIHealthBar = GameObject.Find("Health").GetComponentsInChildren<Image>()[1].transform;
                loadingScreen = GameObject.Find("Loading");
                KillsText = GameObject.Find("KillCounter").GetComponent<TextMeshProUGUI>();
                DeathsText = GameObject.Find("DeathCounter").GetComponent<TextMeshProUGUI>();
                HUDUsernameDisplay = GameObject.Find("UsernameText").GetComponent<TextMeshProUGUI>();
                LoadingStatusDisplay = GameObject.Find("LoadingText").GetComponent<TextMeshProUGUI>();
                GunName = GameObject.Find("GunName").GetComponent<TextMeshProUGUI>();
                RespawningScreen = GameObject.Find("RespawnScreen");
                PlayScreen = GameObject.Find("PlayScreen");
                CountdownText = GameObject.Find("CountdownText").GetComponent<TextMeshProUGUI>();
                RespawnButton = GameObject.Find("RespawnButton").GetComponent<Button>();
                RespawnButton.onClick.AddListener(Respawn);
                RespawningScreen.SetActive(false);
                SetCode();
            }
        }

        public void SetCode()
        {
            if (SceneManager.GetActiveScene().name != "MainMenu")
            {
                CodeDisplay = GameObject.Find("CodeDisplay").GetComponent<Button>();
                CodeDisplay.GetComponentInChildren<TextMeshProUGUI>().text = NetworkManager.Singleton.code;
                CodeDisplay.onClick.AddListener(CopyCode);   
            }
        }

        public void SetStartingText(string text)
        {
            StartingText.text = text;
        }

        private void FixedUpdate()
        {
            SetPingText();
        }

        private void SetPingText()
        {
            if (PingText)
            {
                if(PingText.isActiveAndEnabled)
                {
                    PingText.text = "Ping: " + NetworkManager.Singleton.Client.Connection.RTT;
                    return;
                }
            }

            if (SceneManager.GetActiveScene().name == "Lobby")
            {
                if(LobbyPlayer.LocalPlayer)
                {
                    if(Cursor.lockState != CursorLockMode.Locked)
                    {
                        PingText = GameObject.Find("PingText").GetComponent<TextMeshProUGUI>();
                    }
                }
            }
            else
            {
                if(Player.LocalPlayer)
                {
                    if(Cursor.lockState != CursorLockMode.Locked)
                    {
                        PingText = GameObject.Find("PingText").GetComponent<TextMeshProUGUI>();
                    }
                }   
            }
        }

        private bool serverSaidWeCanRespawn;

        public void CanRespawn()
        {
            RespawnButton.interactable = true;
            CountdownText.text = "CAN RESPAWN";
            serverSaidWeCanRespawn = true;
            Debug.Log("Can respawn, server said so");
        }

        public void Respawned()
        {
            Player.LocalPlayer._cameraController.canPause = true;
            Player.LocalPlayer._cameraController.ToggleCursorMode();
            RespawningScreen.SetActive(false);
            PlayScreen.SetActive(true);
        }

        public void OpenRespawnScreen(int countdownTime)
        {
            Debug.Log("Respawn Screen opening");
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Player.LocalPlayer._cameraController.canPause = false;
            
            PlayScreen.SetActive(false);
            RespawningScreen.SetActive(true);
            RespawnButton.interactable = false;

            serverSaidWeCanRespawn = false;
            
            CountdownText.text = $"RESPAWNING IN {countdownTime}";
            countdown = countdownTime;
            StartCoroutine(CountDown());
        }

        private IEnumerator CountDown()
        {
            int initialCountdown = countdown;
            
            Debug.Log(initialCountdown);
            Debug.Log(countdown);
            
            for (int i = initialCountdown-1; i > 0; i--)
            {
                if(!serverSaidWeCanRespawn){
                    yield return new WaitForSeconds(1);
                    Debug.Log(i);
                    CountdownText.text = $"RESPAWNING IN {i}";
                }
                else
                {
                    break;
                }
            }
            Debug.Log("Can respawn, client said so");
        }

        public void Respawn()
        {
            Message message = Message.Create(MessageSendMode.Reliable, ClientToServerId.playerWantsToRespawn);
            NetworkManager.Singleton.Client.Send(message);
            Debug.Log("Pressed Respawn");
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