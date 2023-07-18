using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        public MenuUIManager UiManager => uiManager;
        public static List<GameObject> _matchButtons = new List<GameObject>();

        public List<Invite> Invites => invites;
        private List<Invite> invites = new List<Invite>();

        public List<GameObject> currentInviteObjs = new List<GameObject>();

        public void UpdateInvites()
        {
            List<Invite> toBeRemoved = new List<Invite>();

            for (int i = 0; i < invites.Count; i++)
            {
                if (invites[i].expired)
                {
                    toBeRemoved.Add(invites[i]);
                }
            }

            for (int i = 0; i < toBeRemoved.Count; i++)
            {
                invites.Remove(toBeRemoved[i]);
            }
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
            
            RichPresenseManager.UpdateStatus("In Main Menu", "Idling", false);
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
            string username = message.GetString();
            ushort port = message.GetUShort();
            string code = message.GetString();
            string name = message.GetString();
            
            Singleton.uiManager.InvitedBy(username, port);

            Invite invite = new Invite(port, code, username, name);
            
            Singleton.invites.Add(invite);
            Singleton.StartCoroutine(Singleton.InviteTimer(invite));
        }

        private IEnumerator InviteTimer(Invite invite)
        {
            yield return new WaitForSeconds(uiManager.InviteExpireTime);
            invite.Expire();
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

    public class ClientInviteInfo
    {
        public ushort id;
        public string username;
        public bool wantsToInvite;

        public ClientInviteInfo(ushort id, string username)
        {
            this.id = id;
            this.username = username;
        }
    }

    public class Invite
    {
        public ushort port;
        public string username;
        public string matchName;
        public string code;
        public bool expired;

        public Invite(ushort port, string code, string username, string matchName)
        {
            this.port = port;
            this.code = code;
            this.username = username;
            this.matchName = matchName;
        }

        public void Expire()
        {
            Debug.Log("Expired");
            expired = true;
            MenuNetworkManager.Singleton.UpdateInvites();
            MenuNetworkManager.Singleton.UiManager.InviteExpired();
        }
    }
}