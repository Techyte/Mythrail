using System;
using Mythrail.General;
using Mythrail.Multiplayer;
using UnityEngine;
using Riptide;
using Riptide.Utils;
using UnityEngine.SceneManagement;

namespace Mythrail.MainMenu
{
    public enum ClientToGameServerId : ushort
    {
        id = 100,
        updateUsername,
        requestMatches,
        createMatch,
        joinMatch,
        getPlayers,
        invites,
    }
    
    public enum GameServerToClientId : ushort
    {
        matches = 100,
        createMatchSuccess,
        joinedMatch,
        matchNotFound,
        invalidName,
        playersResult,
        invite,
    }

    public class MenuNetworkManager : MonoBehaviour
    {
        private static MenuNetworkManager _singleton;
        public static MenuNetworkManager Singleton
        {
            get => _singleton;
            private set
            {
                if (_singleton == null)
                    _singleton = value;
                else if (_singleton != value)
                {
                    Debug.Log($"{nameof(MenuNetworkManager)} instance already exists, destroying duplicate!");
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
        [SerializeField] private bool local;
        [SerializeField] private ushort port;
        public static string username = "Guest";

        [Space]
        
        [SerializeField] private MenuUIManager uiManager;

        public MenuUIManager UiManager => uiManager;

        private void Awake()
        {
            Singleton = this;
        }

        private void Start()
        {
            DestroyGameNetworkManager();
            
            LoadUsername();
            
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
            RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);

            Client = new Client();
            Client.Connected += ClientConnected;
            Client.ConnectionFailed += uiManager.ConnectionFailed;
            Client.Disconnected += uiManager.Disconnected;
            Connect();
            
            RichPresenseManager.UpdateStatus("In Main Menu", "Idling", false);
        }

        private void DestroyGameNetworkManager()
        {
            if (NetworkManager.Singleton)
            {
                Destroy(NetworkManager.Singleton.gameObject);
            }
        }
        
        public void Connect()
        {
            string newIp = local ? "127.0.0.1" : ip;
            
            Singleton.Client.Connect($"{newIp}:{port}");
            uiManager.Connecting();
        }

        private void SendInitialServerInfo()
        {
            Message message = Message.Create(MessageSendMode.Reliable, ClientToGameServerId.id);
            message.AddString(username);
            Singleton.Client.Send(message);
        }

        public void OnUsernameFieldChanged(string newUsername)
        {
            uiManager.SendUpdatedUsername(newUsername);
        }

        public void Quit()
        {
            Application.Quit();
        }

        private void LoadUsername()
        {
            string username = PlayerPrefs.GetString("Username");
            MenuNetworkManager.username = username;
            uiManager.LoadUsername(username);
        }

        private void ClientConnected(object sender, EventArgs e)
        {
            string[] args = Environment.GetCommandLineArgs();

            if (args.Length == 2)
            {
                string[] urlPeices = args[1].Split("//");
                
                Message message = Message.Create(MessageSendMode.Reliable, ClientToGameServerId.joinMatch);
                message.AddString(urlPeices[1].Remove(urlPeices[1].Length - 1, 1));
                Client.Send(message);
            }
            
            SendInitialServerInfo();
            RequestMatches();
            uiManager.Connected();
        }

        private void RequestMatches()
        {
            Message message = Message.Create(MessageSendMode.Reliable, ClientToGameServerId.requestMatches);
            Client.Send(message);
        }

        private void FixedUpdate()
        {
            if (Client != null)
            {
                Client.Update();
                ServerTick++;   
            }
        }

        private void OnApplicationQuit()
        {
            Client.Disconnect();
        }

        [MessageHandler((ushort)GameServerToClientId.invalidName)]
        private static void InvalidName(Message message)
        {
            Singleton.uiManager.InvalidUsername();
        }

        public void JoinMatch(ushort port)
        {
            JoinMatchInfo.port = port;
            JoinMatchInfo.username = username;
            
            SceneManager.LoadScene("Lobby");
        }
    }

    public static class JoinMatchInfo
    {
        public static ushort port;
        public static string username;
    }
}