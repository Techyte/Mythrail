using System;
using System.Collections.Generic;
using System.Linq;
using Mythrail.General;
using Mythrail.Multiplayer;
using UnityEngine;
using Riptide;
using Riptide.Utils;
using UnityEngine.SceneManagement;

namespace Mythrail.Menu
{
    public enum ClientToGameServerId : ushort
    {
        id = 100,
        updateUsername,
        requestMatches,
        createMatch,
        joinPrivateMatch,
        getPlayers,
        invites,
    }
    
    public enum GameServerToClientId : ushort
    {
        matches = 100,
        createMatchSuccess,
        joinedPrivateMatch,
        privateMatchNotFound,
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
        [SerializeField] private ushort port;
        public static string username = "Guest";

        [Space] 
        
        [SerializeField] private MenuUIManager uiManager;

        public static List<GameObject> _matchButtons = new List<GameObject>();

        public void GetCurrentPlayers()
        {
            Message message = Message.Create(MessageSendMode.Reliable, ClientToGameServerId.getPlayers);
            Client.Send(message);
        }

        [MessageHandler((ushort)GameServerToClientId.playersResult)]
        private static void PlayersResult(Message message)
        {
            Singleton.uiManager.EnableInviteScreen(message.GetClientInfos().ToList());
        }

        private void Awake()
        {
            Singleton = this;
        }

        private void Start()
        {
            LoadUsername();
            
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
            RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);

            Client = new Client();
            Client.Connected += ClientConnected;
            Client.ConnectionFailed += uiManager.ConnectionFailed;
            Client.Disconnected += uiManager.Disconnected;
            Connect();
            
            RichPresenseManager.Singleton.UpdateStatus("In Main Menu", "Idling", false);
        }

        public void Connect()
        {
            Singleton.Client.Connect($"{ip}:{port}");
            uiManager.Connecting();
        }

        private void SendInitialServerInfo()
        {
            Message message = Message.Create(MessageSendMode.Reliable, ClientToGameServerId.id);
            message.AddString(username);
            Singleton.Client.Send(message);
        }

        public void RequestMatches()
        {
            Message message = Message.Create(MessageSendMode.Reliable, ClientToGameServerId.requestMatches);
            Singleton.Client.Send(message);
        }

        public void OnUsernameFieldChanged(string newUsername)
        {
            SaveUsername(newUsername);
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

        private void SaveUsername(string newUsername)
        {
            PlayerPrefs.SetString("Username", newUsername);
        }

        private void ClientConnected(object sender, EventArgs e)
        {
            string[] args = Environment.GetCommandLineArgs();

            if (args.Length == 2)
            {
                string[] urlPeices = args[1].Split("//");
                
                Message message = Message.Create(MessageSendMode.Reliable, ClientToGameServerId.joinPrivateMatch);
                message.AddString(urlPeices[1].Remove(urlPeices[1].Length - 1, 1));
                Client.Send(message);
            }
            
            SendInitialServerInfo();
            RequestMatches();
            uiManager.Connected();
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

        [MessageHandler((ushort)GameServerToClientId.matches)]
        private static void Matches(Message message)
        {
            foreach (var button in _matchButtons)
            {
                Destroy(button);
            }
            
            MatchInfo[] matchInfos = message.GetMatchInfos();

            for (int i = 0; i < matchInfos.Length; i++)
            {
                Singleton.uiManager.CreateMatchButton(matchInfos[i].name, matchInfos[i].creatorName, matchInfos[i].code, matchInfos[i].port);
            }
        }
        
        public ushort quickPort;
        [MessageHandler((ushort)GameServerToClientId.createMatchSuccess)]
        private static void CreateMatchSuccess(Message message)
        {
            bool isPrivate = message.GetBool();
            string code = message.GetString();
            ushort port = message.GetUShort();
            
            Singleton.uiManager.ShowMatchCreationMessage(code, isPrivate);
            Singleton.quickPort = port;
        }

        public void JoinMatchWithoutInviting()
        {
            JoinMatch(quickPort);
        }

        [MessageHandler((ushort)GameServerToClientId.joinedPrivateMatch)]
        private static void PrivateMatchJoinSuccess(Message message)
        {
            Singleton.JoinMatch(message.GetUShort());
        }

        [MessageHandler((ushort)GameServerToClientId.privateMatchNotFound)]
        private static void PrivateMatchNotFound(Message message)
        {
            Singleton.uiManager.MatchNotFound();
        }

        [MessageHandler((ushort)GameServerToClientId.invalidName)]
        private static void InvalidName(Message message)
        {
            Singleton.uiManager.InvalidUsername();
        }

        
        [MessageHandler((ushort)GameServerToClientId.invite)]
        private static void Invited(Message message)
        {
            Singleton.uiManager.InvitedBy(message.GetString(), message.GetUShort());
        }

        public void JoinMatch(ushort port)
        {
            JoinMatchInfo.port = port;
            JoinMatchInfo.username = username;
            
            SceneManager.LoadScene("Lobby");
        }

        public void JoinPrivateMatchFromCreate()
        {
            JoinMatch(quickPort);
        }
    }

    public static class JoinMatchInfo
    {
        public static ushort port;
        public static string username;
    }

    public class ClientInfo
    {
        public ushort id;
        public string username;
        public bool wantsToInvite;

        public ClientInfo(ushort id, string username)
        {
            this.id = id;
            this.username = username;
        }
    }
}