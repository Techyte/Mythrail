using UnityEngine;
using RiptideNetworking;
using RiptideNetworking.Utils;
using System;
using TMPro;
using UnityEngine.SceneManagement;

namespace MythrailEngine
{
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
    }

    public enum ClientToServerId : ushort
    {
        name = 1,
        movementInput,
        weaponInput,
        ready,
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
                    Debug.Log($"{nameof(NetworkManager)} instance already exists, destroying duplicate!");
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
                InterpolationTick = (uint)(ServerTick - value);
            }
        }

        [SerializeField] private string ip;
        [SerializeField] private ushort port;
        [SerializeField] private string username;
        [Space(10)]
        [SerializeField] private uint TickDivergenceTolerance = 1;
        [Space(10)]
        [SerializeField] private GameObject LoadingScreen;

        [SerializeField] private TextMeshProUGUI deathsText;
        [SerializeField] private TextMeshProUGUI killsText;

        public static TextMeshProUGUI KillsText;
        public static TextMeshProUGUI DeathsText;

        public UIManager uiManager;

        [SerializeField] private bool PlayerReady;

        private void Awake()
        {
            Singleton = this;
        }

        private void Start()
        {
            LoadingScreen.SetActive(true);
            KillsText = killsText;
            DeathsText = deathsText;
            
            RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);

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
            port = JoinMatchInfo.port;
            Client.Connect($"{ip}:{port}");
        }

        private void DidConnect(object sender, EventArgs e)
        {
            SendName();
        }

        private void SendName()
        {
            Message message = Message.Create(MessageSendMode.reliable, ClientToServerId.name);
            if (JoinMatchInfo.username != "")
            {
                message.Add(JoinMatchInfo.username);
            }
            else
            {
                message.Add(username);
            }
            JoinMatchInfo.username = "";
            message.Add(username);
            
            Singleton.Client.Send(message);
        }

        private void PlayerLeft(object sender, ClientDisconnectedEventArgs e)
        {
            if (Player.list.TryGetValue(e.Id, out Player player))
                Destroy(player.gameObject);
        }

        private void DidDisconnect(object sender, EventArgs e)
        {
            foreach (Player player in Player.list.Values)
                Destroy(player.gameObject);
            
            SceneManager.LoadScene(0);
        }

        private void SetTick(ushort serverTick)
        {
            if (Mathf.Abs(ServerTick - serverTick) > TickDivergenceTolerance)
            {
                Debug.Log($"Client tick: {ServerTick} -> {serverTick}");
                ServerTick = serverTick;
                if (!PlayerReady)
                {
                    Ready();
                }
            }
        }

        private void Ready()
        {
            PlayerReady = true;
            Message message = Message.Create(MessageSendMode.reliable, ClientToServerId.ready);
            Client.Send(message);
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
            Singleton.LoadingScreen.SetActive(false);
        }
    }

}