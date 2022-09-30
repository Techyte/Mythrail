using System;
using System.Collections;
using UnityEngine;
using RiptideNetworking;
using RiptideNetworking.Utils;
using System.Net;
using System.Net.Sockets;
using TMPro;
using UnityEngine.SceneManagement;

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

public enum LobbyServerToClient : ushort
{
    sync = 200,
    ready,
    playerSpawned,
    playerMovement,
}

public enum ClientToLobbyServer : ushort
{
    movementInput = 100,
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
            else if(_singleton != value)
            {
                Debug.Log($"{nameof(NetworkManager)} instance already exists, destroying duplicate!");
                Destroy(value);
            }
        }
    }

    public Server Server { get; private set; }
    public uint CurrentTick { get; private set; } = 0;

    [SerializeField] private ushort port;
    [SerializeField] private ushort maxClientCount;
    [SerializeField] private ushort minPlayerCount;

    [SerializeField] private TextMeshProUGUI portText;
    [SerializeField] private TextMeshProUGUI readyPlayersText;
    [SerializeField] private TextMeshProUGUI totalPlayersText;
    [Space]
    [SerializeField] private float readyCountdown;
    [SerializeField] private float fullReadyCountdown;

    private bool lobbyHasStartedCounting = false;
    private bool lobbyHasStartedCountingQuickly = false;

    private bool hasLoadedLobby;

    [SerializeField] private float emptyLobbyTimer = 30;
    private float emptyLobbyTimerCurrent;
    
    private void Awake()
    {
        emptyLobbyTimerCurrent = emptyLobbyTimer;
        Singleton = this;
        
        DontDestroyOnLoad(gameObject);
        
        string[] args = Environment.GetCommandLineArgs();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("port"))
            {
                string[] splitArg = args[i].Split(":");
                port = ushort.Parse(splitArg[1]);
            }else if (args[i].StartsWith("maxPlayers"))
            {
                string[] splitArg = args[i].Split(":");
                maxClientCount = ushort.Parse(splitArg[1]);
            }else if (args[i].StartsWith("minPlayers"))
            {
                string[] splitArg = args[i].Split(":");
                minPlayerCount = ushort.Parse(splitArg[1]);
            }
        }

        Application.targetFrameRate = 60;

        RiptideLogger.Initialize(Debug.LogError, Debug.LogError, Debug.LogWarning, Debug.LogError, false);

        if (port == 0) port = (ushort)FreeTcpPort();

        Server = new Server();
        Server.Start(port, maxClientCount);
        Server.ClientDisconnected += PlayerLeft;
        if(portText) portText.text = port.ToString();

        SceneManager.sceneLoaded += UpdateReferences;
    }

    private void UpdateReferences(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (scene.buildIndex == 1)
        {
            portText = GameObject.Find("PortText").GetComponent<TextMeshProUGUI>();
            portText.text = port.ToString();
            readyPlayersText = GameObject.Find("ReadyPlayers").GetComponent<TextMeshProUGUI>();
            totalPlayersText = GameObject.Find("TotalPlayers").GetComponent<TextMeshProUGUI>();
        }
    }

    private void Update()
    {
        if (readyPlayersText)
        {
            readyPlayersText.text = GameLogic.Singleton.readyPlayers.ToString();
            totalPlayersText.text = Server.ClientCount.ToString();
        }

        if (Server.ClientCount == 0)
        {
            emptyLobbyTimerCurrent -= Time.deltaTime;
            if (emptyLobbyTimerCurrent <= 0)
            {
                Debug.Log("Match Was Empty For To Long");
                Application.Quit();
            }
        }
    }

    static int FreeTcpPort()
    {
        TcpListener l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        int port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }

    private void FixedUpdate()
    {
        Server.Tick();

        if (CurrentTick % 200 == 0)
        {
            if(SceneManager.GetActiveScene().buildIndex != 0)
            {
                SendSync();
            }
            else
            {
                SendLobbySync();
            }   
        }

        CurrentTick++;
        
        if(SceneManager.GetActiveScene().buildIndex == 0) UpdateLobbyStatus();
    }

    private void UpdateLobbyStatus()
    {
        if (Server.ClientCount >= minPlayerCount && !lobbyHasStartedCounting)
        {
            StartCoroutine(StartGameCountdown());
        }

        if (minPlayerCount == maxClientCount && !lobbyHasStartedCountingQuickly)
        {
            StartCoroutine(StartQuickGameCountdown());
        }
    }

    private IEnumerator StartGameCountdown()
    {
        lobbyHasStartedCounting = true;
        yield return new WaitForSeconds(readyCountdown);
        SendLobbyReady();
    }

    private IEnumerator StartQuickGameCountdown()
    {
        lobbyHasStartedCountingQuickly = true;
        yield return new WaitForSeconds(fullReadyCountdown);
        SendLobbyReady();
    }

    private void SendLobbyReady()
    {
        Message message = Message.Create(MessageSendMode.reliable, LobbyServerToClient.ready);
        Singleton.Server.SendToAll(message);
        SceneManager.LoadScene(1);
    }

    private void OnApplicationQuit()
    {
        Server.Stop();
    }

    private void PlayerLeft(object sender, ClientDisconnectedEventArgs e)
    {
        Debug.Log("Player left id: " + e.Id);
        if (Player.list.TryGetValue(e.Id, out Player player))
        {
            if (SceneManager.GetActiveScene().buildIndex == 1 && !player.isGameReady)
            {
                GameLogic.Singleton.PlayerLeftWhileLoading();
            }
            Debug.LogError("Player destroyed");
            Destroy(player.gameObject);
            emptyLobbyTimerCurrent = emptyLobbyTimer;
        }
    }

    private void SendSync()
    {
        Message message = Message.Create(MessageSendMode.unreliable, ServerToClientId.sync);
        message.AddUInt(CurrentTick);

        Server.SendToAll(message);
    }

    private void SendLobbySync()
    {
        Message message = Message.Create(MessageSendMode.unreliable, LobbyServerToClient.sync);
        message.AddUInt(CurrentTick);

        Server.SendToAll(message);
    }

    [MessageHandler((ushort)ClientToServerId.isInGameRequest)]
    private static void IsInGameRequest(ushort fromClientId, Message message)
    {
        Message resaultMessage = Message.Create(MessageSendMode.reliable, ServerToClientId.isInGameResult);
        resaultMessage.AddBool(SceneManager.GetActiveScene().buildIndex == 1);
        Singleton.Server.Send(resaultMessage, fromClientId);
    }
}
