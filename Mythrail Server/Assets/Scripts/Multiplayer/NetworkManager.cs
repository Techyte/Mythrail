using System;
using System.Collections.Generic;
using UnityEngine;
using Riptide;
using System.Net;
using System.Net.Sockets;
using Riptide.Utils;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

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
    playerCanRespawn,
    regularCam,
    LobbyCountdown,
}

public enum ClientToServerId : ushort
{
    register = 1,
    movementInput,
    weaponInput,
    ready,
    isInGameRequest,
    playerWantsToRespawn,
    clientDevMessage,
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

    [Space]
    [SerializeField] private float readyCountdown;
    [SerializeField] private float currentReadCountdown;

    [SerializeField] private bool isCountingDown;

    private bool hasLoadedLobby;

    private List<Transform> spawnPoints;

    [SerializeField] private float emptyLobbyTimer = 30;
    private float emptyLobbyTimerCurrent;

    private bool isPrivate;

    public string code;
    
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
            }else if (args[i].StartsWith("isPrivate"))
            {
                string[] splitArg = args[i].Split(":");
                isPrivate = bool.Parse(splitArg[1]);
            }else if (args[i].StartsWith("code"))
            {
                string[] splitArg = args[i].Split(":");
                code = splitArg[1];
            }
        }

        Application.targetFrameRate = 60;

        RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);

        if (port == 0) port = (ushort)FreeTcpPort();

        Server = new Server();
        Server.Start(port, maxClientCount);
        Server.ClientDisconnected += PlayerLeft;

        SceneManager.sceneLoaded += UpdateReferences;
    }

    public Vector3 GetRandomSpawnPoint()
    {
        Vector3 spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)].position;
        
        return spawnPoint;
    }

    private void UpdateReferences(Scene scene, LoadSceneMode loadSceneMode)
    {
        spawnPoints = new List<Transform>();
        foreach (Transform spawnPoint in GameObject.Find("SpawnPoints").transform)
        {
            spawnPoints.Add(spawnPoint);
        }
    }

    private void Update()
    {
        if (Server.ClientCount == 0)
        {
            emptyLobbyTimerCurrent -= Time.deltaTime;
            if (emptyLobbyTimerCurrent <= 0)
            {
                Application.Quit();
            }
        }  

        if (SceneManager.GetActiveScene().buildIndex == 0)
        {
            if (Server.ClientCount >= minPlayerCount && !isCountingDown)
            {
                isCountingDown = true;
                currentReadCountdown = readyCountdown;
            }
            else if(Server.ClientCount < minPlayerCount)
            {
                isCountingDown = false;
                if(Server.ClientCount > 0)
                    SendLobbyStillWaiting();
            }
            
            if (isCountingDown)
            {
                currentReadCountdown -= Time.deltaTime;
                SendLobbyCountDown(((int)currentReadCountdown).ToString());
                if (currentReadCountdown <= 0)
                {
                    SendLobbyReady();
                }
            }   
        }
    }

    private void SendLobbyCountDown(string text)
    {
        Message message = Message.Create(MessageSendMode.Unreliable, ServerToClientId.LobbyCountdown);
        message.AddString(text);
        Server.SendToAll(message);
    }

    private void SendLobbyStillWaiting()
    {
        Debug.Log("lobby still waiting");
        Message message = Message.Create(MessageSendMode.Unreliable, ServerToClientId.LobbyCountdown);
        message.AddString("NEED MORE PLAYERS");
        Server.SendToAll(message);
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
        Server.Update();

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
    }

    private void SendLobbyReady()
    {
        Message message = Message.Create(MessageSendMode.Reliable, LobbyServerToClient.ready);
        message.AddBool(Singleton.isPrivate);
        message.AddUShort((ushort)Server.ClientCount);
        message.AddUShort(Server.MaxClientCount);
        Singleton.Server.SendToAll(message);
        SceneManager.LoadScene(1);
    }

    private void OnApplicationQuit()
    {
        Server.Stop();
    }

    private void PlayerLeft(object sender, ServerDisconnectedEventArgs e)
    {
        if (Player.list.TryGetValue(e.Client.Id, out Player player))
        {
            if (GameLogic.Singleton.gameHasStarted)
            {
                player.isGameReady = false;
            }
            
            Destroy(player.gameObject);
            
            if (Server.ClientCount == 0)
            {
                emptyLobbyTimerCurrent = emptyLobbyTimer;
            }
            
            if(SceneManager.GetActiveScene().buildIndex == 0)
            {
                if (Server.ClientCount >= minPlayerCount && !isCountingDown)
                {
                    isCountingDown = true;
                    currentReadCountdown = readyCountdown;
                }else if(Server.ClientCount < minPlayerCount)
                {
                    isCountingDown = false;
                }
            }
        }
    }

    private void SendSync()
    {
        Message message = Message.Create(MessageSendMode.Unreliable, ServerToClientId.sync);
        message.AddUInt(CurrentTick);

        Server.SendToAll(message);
    }

    private void HandClientDevMessage(int id, object[] otherInfo)
    {
        switch (id)
        {
            case 0:
                if (Player.list.TryGetValue((ushort)otherInfo[0], out Player player))
                {
                    player.TakeEditorDamage(int.MaxValue);
                }
                break;
        }
    }

    private void SendLobbySync()
    {
        Message message = Message.Create(MessageSendMode.Unreliable, LobbyServerToClient.sync);
        message.AddUInt(CurrentTick);

        Server.SendToAll(message);
    }

    [MessageHandler((ushort)ClientToServerId.isInGameRequest)]
    private static void IsInGameRequest(ushort fromClientId, Message message)
    {
        Message resaultMessage = Message.Create(MessageSendMode.Reliable, ServerToClientId.isInGameResult);
        resaultMessage.AddBool(SceneManager.GetActiveScene().buildIndex == 1);
        resaultMessage.AddBool(Singleton.isPrivate);
        resaultMessage.AddUShort((ushort)Singleton.Server.ClientCount);
        resaultMessage.AddUShort(Singleton.Server.MaxClientCount);
        resaultMessage.AddString(Singleton.code);
        Singleton.Server.Send(resaultMessage, fromClientId);
    }

    [MessageHandler((ushort)ClientToServerId.clientDevMessage)]
    private static void ClientDevMessage(ushort fromClientId, Message message)
    {
        int id = message.GetInt();
        Singleton.HandClientDevMessage(id, new object[] { fromClientId });
    }
}
