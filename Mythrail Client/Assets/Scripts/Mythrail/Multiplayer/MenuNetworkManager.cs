using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Mythrail.Notifications;
using UnityEngine;
using Riptide;
using Riptide.Utils;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Mythrail.Multiplayer
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
        private static string username = "Guest";
        
        [Space]

        [SerializeField] private GameObject MatchObject;
        [SerializeField] private Transform MatchHolders;
        [SerializeField] private GameObject PlayerObject;
        [SerializeField] private Transform PlayerHolders;

        private static List<GameObject> matchButtons = new List<GameObject>();

        [SerializeField] private TextMeshProUGUI connectionStatusText;
        [SerializeField] private TMP_InputField usernameFeild;

        [SerializeField] private GameObject CreateScreen;
        [SerializeField] private GameObject JoinPrivateMatchScreen;
        [SerializeField] private GameObject MainScreen;

        [SerializeField] private Slider maxPlayerCountSlider;
        [SerializeField] private TextMeshProUGUI maxPlayerDisplay;
        [SerializeField] private Slider minPlayerCountSlider;
        [SerializeField] private TextMeshProUGUI minPlayerDisplay;
        [SerializeField] private TMP_InputField matchName;
        [SerializeField] private Button matchMap;
        [SerializeField] private Toggle privateMatch;

        [SerializeField] private Button CreateBackButton;
        [SerializeField] private Button CreateComfirmButton;

        [SerializeField] private Button SendInvitesButton;

        [SerializeField] private TMP_InputField privateMatchJoinCodeText;

        [SerializeField] private RectTransform privateMatchPopup;
        [SerializeField] private TextMeshProUGUI matchCreatePopupTitle;
        [SerializeField] private TextMeshProUGUI privateCodeText;
        [SerializeField] private TextMeshProUGUI privateCodeURLText;

        [SerializeField] private Sprite PrivateMatchNotFoundImage;

        [SerializeField] private Animator screenShakeAnimator;

        [SerializeField] private GameObject InviteScreen;
        [SerializeField] private GameObject InviteQuestionPopup;

        public void OpenCreateScreen()
        {
            if (string.IsNullOrEmpty(username))
            {
                NotificationManager.Singleton.AddNotificationToQue(PrivateMatchNotFoundImage, "Username empty", "The username you enter cannot be empty, please try again.", 2);
                ShakeScreen();
                return;
            }
            CreateScreen.SetActive(true);
            MainScreen.SetActive(false);
            JoinPrivateMatchScreen.SetActive(false);
        }

        public void OpenMainScreen()
        {
            CreateScreen.SetActive(false);
            MainScreen.SetActive(true);
            JoinPrivateMatchScreen.SetActive(false);
        }

        public void OpenJoinPrivateMatchScreen()
        {
            if (string.IsNullOrEmpty(username))
            {
                NotificationManager.Singleton.AddNotificationToQue(PrivateMatchNotFoundImage, "Username empty", "The username you enter cannot be empty, please try again.", 2);
                ShakeScreen();
                return;
            }
            CreateScreen.SetActive(false);
            MainScreen.SetActive(false);
            JoinPrivateMatchScreen.SetActive(true);
        }

        public void GetCurrentPlayers()
        {
            Message message = Message.Create(MessageSendMode.Reliable, ClientToGameServerId.getPlayers);
            Client.Send(message);
        }

        [MessageHandler((ushort)GameServerToClientId.playersResult)]
        private static void PlayersResult(Message message)
        {
            Singleton.CreateScreen.SetActive(false);
            Singleton.InviteQuestionPopup.SetActive(false);
            Singleton.InviteScreen.SetActive(true);
            Singleton.OpenInviteScreen(message.GetClientInfos().ToList());
        }

        private void OpenInviteScreen(List<ClientInfo> clientInfos)
        {
            for (int i = 0; i < clientInfos.Count; i++)
            {
                GameObject PlayerListObject = Instantiate(PlayerObject, PlayerHolders);
                PlayerListObject.GetComponentInChildren<TextMeshProUGUI>().text = clientInfos[i].username;
                PlayerListObject.GetComponentInChildren<Toggle>().onValueChanged.AddListener(result =>
                {
                    clientInfos[i-1].wantsToInvite = result;
                });
            }
            
            SendInvitesButton.onClick.AddListener(() =>
            {
                List<ClientInfo> invitedClients = new List<ClientInfo>();
                foreach (ClientInfo player in clientInfos)
                {
                    if (player.wantsToInvite)
                    {
                        invitedClients.Add(player);
                    }
                }
                
                Message message = Message.Create(MessageSendMode.Reliable, ClientToGameServerId.invites);
                message.AddClientInfos(invitedClients.ToArray());
                message.AddUShort(Singleton.quickPort);
                Client.Send(message);
            });
        }

        public void OpenInviteQuestionScreen()
        {
            InviteScreen.SetActive(false);
            InviteQuestionPopup.SetActive(true);
        }

        public void ShakeScreen()
        {
            screenShakeAnimator.SetBool("CanShake", true);
            StartCoroutine(ShakeScreenOff());
        }

        private IEnumerator ShakeScreenOff()
        {
            yield return new WaitForSeconds(.1f);
            screenShakeAnimator.SetBool("CanShake", false);
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

            maxPlayerCountSlider.onValueChanged.AddListener(delegate { UpdateMinMax(); });
            minPlayerCountSlider.onValueChanged.AddListener(delegate { UpdateMinMax(); });

            Client = new Client();
            Client.Connected += ClientConnected;
            Client.ConnectionFailed += ConnectionFailed;
            Singleton.Client.Connect($"{ip}:{port}");
            connectionStatusText.text = "Connecting...";
        }

        private void UpdateMinMax()
        {
            maxPlayerDisplay.text = maxPlayerCountSlider.value.ToString();
            minPlayerDisplay.text = minPlayerCountSlider.value.ToString();
        }

        private void ConnectionFailed(object o, EventArgs args)
        {
            connectionStatusText.text = "Connection Failed!";
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
            username = newUsername;
            SaveUsername(newUsername);
            SendUpdatedUsername(newUsername);
        }

        public void Quit()
        {
            Application.Quit();
        }

        private void LoadUsername()
        {
            string username = PlayerPrefs.GetString("Username");
            MenuNetworkManager.username = username;
            usernameFeild.text = username;
        }

        private void SaveUsername(string newUsername)
        {
            PlayerPrefs.SetString("Username", newUsername);
        }

        private void SendUpdatedUsername(string newUsername)
        {
            if(newUsername == username) return;
            
            if (string.IsNullOrEmpty(newUsername))
            {
                NotificationManager.Singleton.AddNotificationToQue(PrivateMatchNotFoundImage, "Username empty", "The username you enter cannot be empty, please try again.", 2);
                ShakeScreen();
                return;
            }

            username = newUsername;
            
            Message message = Message.Create(MessageSendMode.Reliable, ClientToGameServerId.updateUsername);
            message.AddString(username);
            Singleton.Client.Send(message);
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
            connectionStatusText.text = "Connected";
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

        private void ShowPrivateMatchMessage(string code, bool isPrivate)
        {
            privateMatchPopup.gameObject.SetActive(true);
            privateCodeText.text = code;
            matchName.interactable = false;
            matchMap.interactable = false;
            maxPlayerCountSlider.interactable = false;
            minPlayerCountSlider.interactable = false;
            privateMatch.interactable = false;
            CreateBackButton.interactable = false;
            CreateComfirmButton.interactable = false;

            Singleton.matchCreatePopupTitle.text = isPrivate ? "PRIVATE MATCH CREATED" : "PUBLIC MATCH CREATED";

            privateCodeURLText.text = "mythrail://" + code;
        }

        [MessageHandler((ushort)GameServerToClientId.matches)]
        private static void Matches(Message message)
        {
            foreach (var button in matchButtons)
            {
                Destroy(button);
            }
            
            MatchInfo[] matchInfos = message.GetMatchInfos();

            for (int i = 0; i < matchInfos.Length; i++)
            {
                Singleton.CreateMatchButton(matchInfos[i].name, matchInfos[i].creatorName, matchInfos[i].code, matchInfos[i].port);
            }
        }
        
        private ushort quickPort;
        [MessageHandler((ushort)GameServerToClientId.createMatchSuccess)]
        private static void CreateMatchSuccess(Message message)
        {
            bool isPrivate = message.GetBool();
            string code = message.GetString();
            ushort port = message.GetUShort();
            
            Singleton.ShowPrivateMatchMessage(code, isPrivate);
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
            Singleton.OpenMainScreen();
            NotificationManager.Singleton.AddNotificationToQue(Singleton.PrivateMatchNotFoundImage, "Incorrect Code", "This is not the game you are looking for...", 2);
        }

        [MessageHandler((ushort)GameServerToClientId.invalidName)]
        private static void InvalidName(Message message)
        {
            Singleton.usernameFeild.text = "";
            NotificationManager.Singleton.AddNotificationToQue(Singleton.PrivateMatchNotFoundImage, "Can't use that name", "A user on this server is already using that name, please try again with a different name", 2);
        }

        [SerializeField] private Sprite multiplayerImage;
        
        [MessageHandler((ushort)GameServerToClientId.invite)]
        private static void Invited(Message message)
        {
            int index = NotificationManager.Singleton.AddNotificationToQue(Singleton.multiplayerImage,
                $"Invited by {message.GetString()}", "Click here to join", 5);

            ushort port = message.GetUShort();
            NotificationManager.Singleton.NewNotification += (o, e) =>
            {
                if (e.notificationIndex == index)
                {
                    e.notification.Clicked += (o, e) =>
                    {
                        Singleton.JoinMatch(port);
                    };
                }
            };
        }

        private void CreateMatchButton(string name, string creator, string code, ushort port)
        {
            GameObject newMatchObj = Instantiate(MatchObject, MatchHolders);

            newMatchObj.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = name;
            newMatchObj.transform.Find("Creator").GetComponent<TextMeshProUGUI>().text = creator;
            newMatchObj.transform.Find("Port").GetComponent<TextMeshProUGUI>().text = code;
            
            newMatchObj.GetComponent<Button>().onClick.AddListener(delegate
            {
                JoinMatch(port);
            });
            
            matchButtons.Add(newMatchObj);
        }

        public void JoinMatch(ushort port)
        {
            JoinMatchInfo.port = port;
            JoinMatchInfo.username = username;
            
            SceneManager.LoadScene("Lobby");
        }

        public void CreateMatch()
        {
            if (string.IsNullOrEmpty(matchName.text))
            {
                NotificationManager.Singleton.AddNotificationToQue(PrivateMatchNotFoundImage, "Name empty", "The match name you enter cannot be empty, please try again.", 2);
                ShakeScreen();
                return;
            }
            
            Message message = Message.Create(MessageSendMode.Reliable, ClientToGameServerId.createMatch);
            message.AddUShort((ushort)maxPlayerCountSlider.value);
            message.AddUShort((ushort)minPlayerCountSlider.value);
            message.AddString(matchName.text);
            message.AddBool(privateMatch.isOn);
            Singleton.Client.Send(message);
        }

        public void JoinPrivateMatchFromCreate()
        {
            JoinMatch(quickPort);
        }

        public void JoinPrivateMatch()
        {
            Message message = Message.Create(MessageSendMode.Reliable, ClientToGameServerId.joinPrivateMatch);
            message.AddString(privateMatchJoinCodeText.text);
            Client.Send(message);
        }

        public void CopyPrivateMatchCode()
        {
            GUIUtility.systemCopyBuffer = privateCodeText.text;
        }

        public void CopyPrivateMatchURL()
        {
            GUIUtility.systemCopyBuffer = privateCodeURLText.text;
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