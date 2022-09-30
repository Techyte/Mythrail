using UnityEngine;
using RiptideNetworking;
using System;
using UnityEngine.SceneManagement;

namespace MythrailEngine
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

        [SerializeField] private bool PlayerReady;
        
        private void Awake()
        {
            Singleton = this;

            SceneManager.sceneLoaded += CheckForMainMenu;
            DontDestroyOnLoad(gameObject);
        }

        private void CheckForMainMenu(Scene scene, LoadSceneMode loadSceneMode)
        {
            Singleton = this;
            if (scene.buildIndex == 0)
            {
                Destroy(gameObject);
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

            ServerTick = 2;
            
            Connect();
        }

        private void FixedUpdate()
        {
            Client.Tick();
            ServerTick++;
        }

        private void OnApplicationQuit()
        {
            Client.Disconnect();
        }

        private void FailedToConnect(object sender, EventArgs e)
        {
            SceneManager.LoadScene(0);
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
            Message message = Message.Create(MessageSendMode.reliable, ClientToServerId.isInGameRequest);
            Client.Send(message);
        }

        private void SendName()
        {
            Message message = Message.Create(MessageSendMode.reliable, ClientToServerId.name);

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
            
            SceneManager.LoadScene(0);
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
            Message message = Message.Create(MessageSendMode.reliable, ClientToServerId.ready);
            Client.Send(message);
            Debug.LogError("Ready");
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
        }

        [MessageHandler((ushort)ServerToClientId.isInGameResult)]
        private static void IsInGameResault(Message message)
        {
            if (message.GetBool())
            {
                Singleton.LoadGame();
            }
            else
            {
                Singleton.SendName();
            }
        }

        private void LoadGame()
        {
            SceneManager.LoadScene(2);
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
            Player.LocalPlayer.playerController.canMove = true;
            UIManager.Singleton.loadingScreen.SetActive(false);
        }
    }
}