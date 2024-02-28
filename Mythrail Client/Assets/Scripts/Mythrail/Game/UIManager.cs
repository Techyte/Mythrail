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

        public Transform uiHealthBar;

        public GameObject loadingScreen;
        
        public TextMeshProUGUI killsText;
        public TextMeshProUGUI deathsText;
        public TextMeshProUGUI hudUsernameDisplay;
        public TextMeshProUGUI loadingStatusDisplay;
        public TextMeshProUGUI gunName;
        public Button codeDisplay;
        public GameObject respawningScreen;
        public GameObject playScreen;
        public TextMeshProUGUI countdownText;
        public Button respawnButton;
        public TextMeshProUGUI latencyText;
        public TextMeshProUGUI startingText;

        private int _countdown;

        private void Awake()
        {
            Singleton = this;
        }

        public void SetCode()
        {
            if (SceneManager.GetActiveScene().name != "MainMenu")
            {
                codeDisplay = GameObject.Find("CodeDisplay").GetComponent<Button>();
                codeDisplay.GetComponentInChildren<TextMeshProUGUI>().text = NetworkManager.Singleton.code;
                codeDisplay.onClick.AddListener(CopyCode);   
            }
        }

        public void SetStartingText(string text)
        {
            startingText.text = text;
        }

        private void FixedUpdate()
        {
            SetPingText();
        }

        private void SetPingText()
        {
            if (latencyText)
            {
                if(latencyText.isActiveAndEnabled)
                {
                    latencyText.text = "Latency: " + NetworkManager.Singleton.Client.Connection.RTT;
                    return;
                }
            }

            if (SceneManager.GetActiveScene().name == "Lobby")
            {
                if(LobbyPlayer.LocalPlayer)
                {
                    if(Cursor.lockState != CursorLockMode.Locked)
                    {
                        latencyText = GameObject.Find("LatencyText").GetComponent<TextMeshProUGUI>();
                    }
                }
            }
            else
            {
                if(Player.LocalPlayer)
                {
                    if(Cursor.lockState != CursorLockMode.Locked && !Player.LocalPlayer.respawning)
                    {
                        latencyText = GameObject.Find("LatencyText").GetComponent<TextMeshProUGUI>();
                    }
                }   
            }
        }

        private bool serverSaidWeCanRespawn;

        public void CanRespawn()
        {
            respawnButton.interactable = true;
            countdownText.text = "CAN RESPAWN";
            serverSaidWeCanRespawn = true;
            Debug.Log("Can respawn, server said so");
        }

        public void Respawned()
        {
            Player.LocalPlayer._cameraController.canPause = true;
            Player.LocalPlayer._cameraController.ToggleCursorMode();
            respawningScreen.SetActive(false);
            playScreen.SetActive(true);
        }

        public void OpenRespawnScreen(int countdownTime)
        {
            Debug.Log("Respawn Screen opening");
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Player.LocalPlayer._cameraController.canPause = false;
            
            playScreen.SetActive(false);
            respawningScreen.SetActive(true);
            respawnButton.interactable = false;

            serverSaidWeCanRespawn = false;
            
            countdownText.text = $"RESPAWNING IN {countdownTime}";
            _countdown = countdownTime;
            StartCoroutine(CountDown());
        }

        private IEnumerator CountDown()
        {
            int initialCountdown = _countdown;
            
            for (int i = initialCountdown-1; i > 0; i--)
            {
                if(!serverSaidWeCanRespawn){
                    yield return new WaitForSeconds(1);
                    Debug.Log(i);
                    countdownText.text = $"RESPAWNING IN {i}";
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
            uiHealthBar.localScale = Vector3.Lerp(uiHealthBar.localScale, new Vector3(healthRatio, 1, 1), Time.deltaTime * 8f);

            uiHealthBar.GetComponent<Image>().color = Player.LocalPlayer.currentHealth <= 10 ? Color.red : Color.green;
        }

        public void CopyCode()
        {
            GUIUtility.systemCopyBuffer = NetworkManager.Singleton.code;
        }
    }

}