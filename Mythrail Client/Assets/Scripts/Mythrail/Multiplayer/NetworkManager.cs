using UnityEngine;
using Riptide;
using System;
using Mythrail.Game;
using Mythrail.General;
using Mythrail.Menu;
using Mythrail.Notifications;
using Mythrail.Players;
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
    }

    public enum ClientToServerId : ushort
    {
        name = 1,
        movementInput,
        weaponInput,
        ready,
        isInGameRequest,
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
                    Destroy(value);
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
        [Space] 
        [SerializeField] private Sprite kickedImage;

        [SerializeField] private Sprite xImage;

        private bool isPrivate;
        private ushort maxClientCount;
        private ushort clientCount;

        public string code;
        
        private void Awake()
        {
            Singleton = this;

            SceneManager.sceneLoaded += CheckForMainMenu;
            DontDestroyOnLoad(gameObject);
        }

        private void CheckForMainMenu(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (SceneManager.GetActiveScene().name == "BattleFeild")
            {
                BufferCamera = GameObject.Find("Buffer Camera");
            }
        }

        private void Start()
        {
            LoadingScreen.SetActive(true);

            //RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);

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
                RichPresenseManager.Singleton.UpdateStatus("In Game",
                    $"Game ({clientCount} of {maxClientCount})", false);
            }
            else
            {
                RichPresenseManager.Singleton.UpdateStatus("In Game",
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
            Client.Disconnect();
            NotificationManager.Singleton.QueNotification(xImage, "Could not connect", "The match does not exist or something went wrong.", 2);
            SceneManager.LoadScene("MainMenu");
        }

        private void Connect()
        {
            port = JoinMatchInfo.port != 0 ? JoinMatchInfo.port : port;
            JoinMatchInfo.port = 0;
            
            Client.Connect($"{ip}:{port}");
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

        private void SendName()
        {
            Message message = Message.Create(MessageSendMode.Reliable, ClientToServerId.name);

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
            Client.Disconnect();
            Debug.Log("we think the server disconnected us");
            NotificationManager.Singleton.QueNotification(kickedImage, "Kicked from match", "The match server shut down and you were kicked.", 2);
            SceneManager.LoadScene("MainMenu");
        }

        public static bool hasBeenReadyOnce;
        private void SetTick(ushort serverTick)
        {
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
                            Ready();
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
            UIManager.Singleton.LoadingStatusDisplay.text = "Waiting for other players";
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
            Singleton.isPrivate = message.GetBool();
            ushort clientCount = message.GetUShort();
            Singleton.maxClientCount = message.GetUShort();
            
            if(!Singleton.isPrivate)
            {
                RichPresenseManager.Singleton.UpdateStatus("In Game",
                    $"Battlefield ({clientCount} of {Singleton.maxClientCount})", false);
            }
            else
            {
                RichPresenseManager.Singleton.UpdateStatus("In Game",
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
            Singleton.code = message.GetString();
            if (isInGame)
            {
                Singleton.LoadGame();
                if(!Singleton.isPrivate)
                {
                    RichPresenseManager.Singleton.UpdateStatus("In Game",
                        $"Game ({clientCount} of {Singleton.maxClientCount})", false);
                }
                else
                {
                    RichPresenseManager.Singleton.UpdateStatus("In Game",
                        "Private Match", false);
                }
            }
            else
            {
                Singleton.SendName();
                if(!Singleton.isPrivate)
                {
                    RichPresenseManager.Singleton.UpdateStatus("In Lobby",
                        $"Waiting ({clientCount} of {Singleton.maxClientCount})", false);
                }
                else
                {
                    RichPresenseManager.Singleton.UpdateStatus("In Lobby",
                        "Private Match", false);
                }
            }
        }

        private void LoadGame()
        {
            SceneManager.LoadScene("BattleFeild");
            Player.list.Clear();
            Singleton.SendName();
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
            Singleton.BufferCamera.SetActive(false);
            Player.LocalPlayer.playerController.canMove = true;
            UIManager.Singleton.loadingScreen.SetActive(false);
        }
    }
}