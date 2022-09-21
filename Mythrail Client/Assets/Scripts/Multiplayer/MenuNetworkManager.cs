using System;
using System.Collections.Generic;
using UnityEngine;
using RiptideNetworking;
using RiptideNetworking.Utils;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MythrailEngine
{
    public enum ClientToGameServerId : ushort
    {
        id = 100,
        updateUsername,
        requestMatches,
        createMatch,
    }
    
    public enum GameServerToClientId : ushort
    {
        matches = 100,
        createMatchSuccess,
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
        private static string username = "TestTechyte";

        [SerializeField] private GameObject MatchObject;
        [SerializeField] private Transform MatchHolders;

        private static List<GameObject> matchButtons = new List<GameObject>();

        [SerializeField] private TextMeshProUGUI connectionStatusText;

        [SerializeField] private GameObject CreateScreen;
        [SerializeField] private GameObject MainScreen;

        [SerializeField] private Slider maxPlayerCountSlider;
        [SerializeField] private TextMeshProUGUI maxPlayerDisplay;
        [SerializeField] private Slider minPlayerCountSlider;
        [SerializeField] private TextMeshProUGUI minPlayerDisplay;
        [SerializeField] private TMP_InputField matchName;

        public void OpenCreateScreen()
        {
            CreateScreen.SetActive(true);
            MainScreen.SetActive(false);
        }

        public void OpenMainScreen()
        {
            CreateScreen.SetActive(false);
            MainScreen.SetActive(true);
        }

        private void Awake()
        {
            Singleton = this;
        }

        private void Start()
        {
            //RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);

            maxPlayerCountSlider.onValueChanged.AddListener(delegate {UpdateMinMax(); });
            minPlayerCountSlider.onValueChanged.AddListener(delegate {UpdateMinMax(); });

            Client = new Client();
            Client.Connected += ClientConnected;
            Client.ClientDisconnected += ClientDisconnected;
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

        private void ClientDisconnected(object o, EventArgs args)
        {
            connectionStatusText.text = "Connection Failed";
        }

        private void SendInitialServerInfo()
        {
            Message message = Message.Create(MessageSendMode.reliable, ClientToGameServerId.id);
            message.AddString(username);
            Singleton.Client.Send(message);
        }

        public void RequestMatches()
        {
            Message message = Message.Create(MessageSendMode.reliable, ClientToGameServerId.requestMatches);
            Singleton.Client.Send(message);
        }

        public void OnUsernameFieldChanged(string newUsername)
        {
            username = newUsername;
            SendUpdatedUsername();
        }

        private void SendUpdatedUsername()
        {
            Message message = Message.Create(MessageSendMode.reliable, ClientToGameServerId.updateUsername);
            message.AddString(username);
            Singleton.Client.Send(message);
        }

        private void ClientConnected(object sender, EventArgs e)
        {
            SendInitialServerInfo();
            RequestMatches();
            connectionStatusText.text = "Connected";
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
                Singleton.CreateMatchButton(matchInfos[i].name, matchInfos[i].creatorName, matchInfos[i].port);
            }
        }

        [MessageHandler((ushort)GameServerToClientId.createMatchSuccess)]
        private static void CreateMatchSuccess(Message message)
        {
            JoinMatchInfo.port = message.GetUShort();
            JoinMatchInfo.username = username;
            
            SceneManager.LoadScene(1);
        }

        private void CreateMatchButton(string name, string creator, ushort port)
        {
            GameObject newMatchObj = Instantiate(MatchObject, MatchHolders);

            newMatchObj.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = name;
            newMatchObj.transform.Find("Creator").GetComponent<TextMeshProUGUI>().text = creator;
            newMatchObj.transform.Find("Port").GetComponent<TextMeshProUGUI>().text = port.ToString();
            
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
            SceneManager.LoadScene(1);
        }

        public void CreateMatch()
        {
            Message message = Message.Create(MessageSendMode.reliable, ClientToGameServerId.createMatch);
            message.AddUShort((ushort)maxPlayerCountSlider.value);
            message.AddUShort((ushort)minPlayerCountSlider.value);
            message.AddString(matchName.text);
            Singleton.Client.Send(message);
        }
    }

    public static class JoinMatchInfo
    {
        public static ushort port;
        public static string username;
    }
}