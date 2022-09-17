using UnityEngine;
using RiptideNetworking;
using RiptideNetworking.Utils;
using System;
using UnityEngine.SceneManagement;

namespace MythrailEngine
{
    public enum ClientToLobbyServer : ushort
    {
        movementInput = 100,
    }

    public enum LobbyServerToClient : ushort
    {
        sync = 200,
        ready,
        playerSpawned,
        playerMovement,
    }
    
    public class LobbyNetworkManager : MonoBehaviour
    {
        private static LobbyNetworkManager _singleton;
        public static LobbyNetworkManager Singleton
        {
            get => _singleton;
            private set
            {
                if (_singleton == null)
                    _singleton = value;
                else if (_singleton != value)
                {
                    Debug.Log($"{nameof(LobbyNetworkManager)} instance already exists, destroying duplicate!");
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
        [SerializeField] private GameObject LoadingScreen;
        

        private void Awake()
        {
            Singleton = this;
        }

        private void Start()
        {
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
            if (JoinMatchInfo.port != 0)
            {
                port = JoinMatchInfo.port;
            }
            Client.Connect($"{ip}:{port}");
        }

        private void DidConnect(object sender, EventArgs e)
        {
            SendName();
            LoadingScreen.SetActive(false);
        }

        private void SendName()
        {
            Message message = Message.Create(MessageSendMode.reliable, ClientToServerId.name);

            string finalUsername = JoinMatchInfo.username != null ? JoinMatchInfo.username : username;
            message.AddString(finalUsername);
            JoinMatchInfo.username = "";
            
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
            }
        }

        [MessageHandler((ushort)LobbyServerToClient.sync)]
        public static void LobbySync(Message message)
        {
            Singleton.SetTick(message.GetUShort());
        }

        [MessageHandler((ushort)LobbyServerToClient.ready)]
        private static void LobbyReady(Message message)
        {
            JoinMatchInfo.port = Singleton.port;
            JoinMatchInfo.username = Singleton.username;
            Singleton.Client.Disconnect();
            SceneManager.LoadScene(2);
        }
    }

}