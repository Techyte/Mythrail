using UnityEngine;
using Riptide;
using System;
using Mythrail.Game;
using Mythrail.General;
using Mythrail.MainMenu;
using Mythrail.Notifications;
using Mythrail.Players;
using Riptide.Utils;
using UnityEngine.SceneManagement;

namespace Mythrail.Multiplayer
{
    public enum LobbyServerToClient : ushort
    {
        sync = 200,
        ready,
        playerSpawned,
        playerMovement,
    }
    
    public enum ServerToClientId : ushort
    {
        sync = 1,
        playerSpawned,
        playerMovement,
        playerShot,
        swapWeapon,
        playerTookDamage,
        playerDied,
        bulletHole,
        loadoutInfo,
        playerKilled,
        playerHealth,
        gameStarted,
        isInGameResult,
        playerCanRespawn,
        regularCam,
        LobbyCountdown,
    }

    public enum ClientToServerId : ushort
    {
        register = 1,
        movementInput,
        weaponInput,
        ready,
        isInGameRequest,
        playerWantsToRespawn,
        clientDevMessage,
    }
    
    public class NetworkManager : MonoBehaviour
    {
        private static NetworkManager _singleton;
        public static NetworkManager Singleton
        {
            get => _singleton;
            private set
            {
                if (_singleton == null)
                    _singleton = value;
                else if (_singleton != value)
                {
                    Debug.Log("networkmanager already existed");
                    Destroy(value.gameObject);
                }
            }
        }

        public Client Client { get; private set; }

        private uint _serverTick;
        public uint ServerTick
        {
            get => _serverTick;
            private set
            {
                _serverTick = value;
                InterpolationTick = (uint)(value - TicksBetweenPositionUpdates);
            }
        }
        public uint InterpolationTick { get; private set; }
        private uint _ticksBetweenPositionUpdates = 2;
        public uint TicksBetweenPositionUpdates
        {
            get => _ticksBetweenPositionUpdates;
            set
            {
                _ticksBetweenPositionUpdates = value;
                InterpolationTick = (ServerTick - value);
            }
        }

        [SerializeField] private string ip;
        [SerializeField] private bool local;
        [SerializeField] private ushort port;
        [SerializeField] private string username;
        [Space(10)]
        [SerializeField] private uint TickDivergenceTolerance = 1;
        [Space(10)]
        [SerializeField] private GameObject LobbyLoadingScreen;
        [Space(10)]
        [SerializeField] private GameObject LoadingScreen;

        [SerializeField] private GameObject BufferCamera;

        [SerializeField] private bool PlayerReady;

        [SerializeField] private Sprite xImage;

        private bool isPrivate;
        private ushort maxClientCount;
        private ushort clientCount;

        public string code;

        private void Awake()
        {
            Debug.Log(Singleton);
            Singleton = this;
            
            SceneManager.sceneLoaded += NewScene;
        }

        public void Disconnect()
        {
            Client.Disconnect();
        }

        private void NewScene(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (scene.name == "BattleFeild")
            {
                Singleton.StartSettingUpPlayer(); // called when we load into the lobby or game
                BufferCamera = GameObject.Find("Buffer Camera");
            }
        }

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
            
            LoadingScreen.SetActive(true);

            RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);

            Client = new Client();
            Client.Connected += DidConnect;
            Client.ConnectionFailed += FailedToConnect;
            Client.ClientDisconnected += PlayerLeft;
            Client.Disconnected += DidDisconnect;
            Client.ClientConnected += ClientConnected;

            ServerTick = 2;
            
            Connect();
        }

        private void ClientConnected(object sender, ClientConnectedEventArgs e)
        {
            if(!Singleton.isPrivate)
            {
                clientCount++;
                RichPresenseManager.UpdateStatus("In Game",
                    $"Game ({clientCount} of {maxClientCount})", false);
            }
            else
            {
                RichPresenseManager.UpdateStatus("In Game",
                    $"Private Match", false);
            }
        }

        private void FixedUpdate()
        {
            Client.Update();
            ServerTick++;
        }

        private void OnApplicationQuit()
        {
            Client.Disconnect();
        }

        private void FailedToConnect(object sender, EventArgs e)
        {
            Cursor.lockState = CursorLockMode.None;
            Player.list.Clear();
            NotificationManager.Singleton.CreateNotification(xImage, "Could not connect", "The match does not exist or something went wrong.", 2);
            SceneManager.LoadScene("MainMenu");
        }

        private void Connect()
        {
            port = JoinMatchInfo.port != 0 ? JoinMatchInfo.port : port;
            JoinMatchInfo.port = 0;

            string trueIp = local ? "127.0.0.1" : ip;
            
            Client.Connect($"{trueIp}:{port}");
        }

        private void DidConnect(object sender, EventArgs e)
        {
            GetIsInGameStatus();
            LobbyLoadingScreen.SetActive(false);
        }

        private void GetIsInGameStatus()
        {
            Message message = Message.Create(MessageSendMode.Reliable, ClientToServerId.isInGameRequest);
            Client.Send(message);
        }

        private void StartSettingUpPlayer()
        {
            SendName();
        }

        private void SendName()
        {
            Message message = Message.Create(MessageSendMode.Reliable, ClientToServerId.register);

            username = !string.IsNullOrEmpty(JoinMatchInfo.username)? JoinMatchInfo.username : username;
            message.AddString(username);
            JoinMatchInfo.username = "";
            
            Singleton.Client.Send(message);
        }

        private void PlayerLeft(object sender, ClientDisconnectedEventArgs e)
        {
            if (Player.list.TryGetValue(e.Id, out Player player))
            {
                Destroy(player.gameObject);
            }
        }

        private void DidDisconnect(object sender, EventArgs e)
        {
            foreach (Player player in Player.list.Values)
            {
                Destroy(player.gameObject);
            }
            
            Cursor.lockState = CursorLockMode.None;
            Player.list.Clear();
            SceneManager.LoadScene("MainMenu");
        }
        
        public static bool hasBeenReadyOnce;
        private void SetTick(ushort serverTick)
        {
            Debug.Log("received a set tick");
            if (Mathf.Abs(ServerTick - serverTick) > TickDivergenceTolerance)
            {
                Debug.Log("Setting tick");
                ServerTick = serverTick;
                if (SceneManager.GetActiveScene().buildIndex == 2)
                {
                    if (!PlayerReady)
                    {
                        if(Player.LocalPlayer)
                        {
                            Debug.Log("Ready");
                        }
                        else
                        {
                            hasBeenReadyOnce = true;
                        }
                    }
                }
            }
        }

        public void Ready()
        {
            PlayerReady = true;
            Message message = Message.Create(MessageSendMode.Reliable, ClientToServerId.ready);
            Client.Send(message);
            Debug.Log("Ready");
            Debug.Log(UIManager.Singleton);
            Debug.Log(UIManager.Singleton.LoadingStatusDisplay);
            UIManager.Singleton.LoadingStatusDisplay.text = "WAITING";
        }

        [MessageHandler((ushort)LobbyServerToClient.sync)]
        public static void LobbySync(Message message)
        {
            Singleton.SetTick(message.GetUShort());
        }

        [MessageHandler((ushort)LobbyServerToClient.ready)]
        private static void LobbyReady(Message message)
        {
            Singleton.LoadGame();
            Singleton.isPrivate = message.GetBool(); // TODO: remove all of these values because they have already been set by this point
            ushort clientCount = message.GetUShort();
            Singleton.maxClientCount = message.GetUShort();
            
            if(!Singleton.isPrivate)
            {
                RichPresenseManager.UpdateStatus("In Game",
                    $"Battlefield ({clientCount} of {Singleton.maxClientCount})", false);
            }
            else
            {
                RichPresenseManager.UpdateStatus("In Game",
                    "Private Match", false);
            }
        }

        [MessageHandler((ushort)ServerToClientId.isInGameResult)]
        private static void IsInGameResault(Message message)
        {
            bool isInGame = message.GetBool();
            Singleton.isPrivate = message.GetBool();
            ushort clientCount = message.GetUShort();
            Singleton.maxClientCount = message.GetUShort();
            string code = message.GetString();
            Singleton.code = code;
            Debug.Log(Singleton);
            Debug.Log(Singleton.GetComponent<UIManager>());
            Singleton.GetComponent<UIManager>().SetCode();
            if (isInGame)
            {
                Debug.Log("Game has started, loading battlefeild");
                Singleton.LoadGame();
                if(!Singleton.isPrivate)
                {
                    RichPresenseManager.UpdateStatus("In Game",
                        $"Game ({clientCount} of {Singleton.maxClientCount})", false);
                }
                else
                {
                    RichPresenseManager.UpdateStatus("In Game",
                        "Private Match", false);
                }
            }
            else
            {
                Debug.Log("Game has not started, loading lobby");
                Singleton.StartSettingUpPlayer();
                if(!Singleton.isPrivate)
                {
                    RichPresenseManager.UpdateStatus("In Lobby",
                        $"Waiting ({clientCount} of {Singleton.maxClientCount})", false);
                }
                else
                {
                    RichPresenseManager.UpdateStatus("In Lobby",
                        "Private Match", false);
                }
            }
        }

        private void LoadGame()
        {
            SceneManager.LoadScene("BattleFeild");
        }

        [MessageHandler((ushort)ServerToClientId.sync)]
        public static void Sync(Message message)
        {
            Singleton.SetTick(message.GetUShort());
        }

        [MessageHandler((ushort)ServerToClientId.gameStarted)]
        public static void GameStarted(Message message)
        {
            Debug.Log("Everyone is ready");
            Debug.Log(Singleton);
            Debug.Log(Singleton.BufferCamera);
            Singleton.BufferCamera.SetActive(false);
            Player.LocalPlayer.playerController.canMove = true;
            UIManager.Singleton.loadingScreen.SetActive(false);
        }

        [MessageHandler((ushort)ServerToClientId.LobbyCountdown)]
        private static void LobbyCountdown(Message message)
        {
            Singleton.GetComponent<UIManager>().SetStartingText((int.Parse(message.GetString())+1).ToString());
        }
    }
}