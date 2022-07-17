using System;
using UnityEngine;
using RiptideNetworking;
using RiptideNetworking.Utils;
using System.Net;
using System.Net.Sockets;
using TMPro;

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
}

public enum ClientToServerId : ushort
{
    name = 1,
    movementInput,
    weaponInput,
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

    [SerializeField] private TextMeshProUGUI portText;

    private void Awake()
    {
        Singleton = this;
        
        string[] args = Environment.GetCommandLineArgs();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("port"))
            {
                string[] splitArg = args[i].Split(":");
                foreach (var VARIABLE in splitArg)
                {
                    Debug.Log(VARIABLE);
                }
                port = ushort.Parse(splitArg[1]);
            }else if (args[i].StartsWith("maxPlayers"))
            {
                string[] splitArg = args[i].Split(":");
                foreach (var VARIABLE in splitArg)
                {
                    Debug.Log(VARIABLE);
                }
                maxClientCount = ushort.Parse(splitArg[1]);
            }
        }

        Application.targetFrameRate = 60;

        RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);

        if (port == 0) port = (ushort)FreeTcpPort();

        Server = new Server();
        Server.Start(port, maxClientCount);
        Server.ClientDisconnected += PlayerLeft;
        portText.text = port.ToString();
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
            SendSync();

        CurrentTick++;
    }

    private void OnApplicationQuit()
    {
        Server.Stop();
    }

    private void PlayerLeft(object sender, ClientDisconnectedEventArgs e)
    {
        if (Player.list.TryGetValue(e.Id, out Player player))
            Destroy(player.gameObject);
    }

    private void SendSync()
    {
        Message message = Message.Create(MessageSendMode.unreliable, ServerToClientId.sync);
        message.AddUInt(CurrentTick);

        Server.SendToAll(message);
    }
}
