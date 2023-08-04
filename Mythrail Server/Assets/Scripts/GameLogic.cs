using Riptide;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLogic : MonoBehaviour
{
    private static GameLogic _singleton;
    public static GameLogic Singleton
    {
        get => _singleton;
        private set
        {
            if (_singleton == null)
                _singleton = value;
            else if (_singleton != value)
            {
                Debug.Log($"{nameof(GameLogic)} instance already exists, destroying duplicate!");
                Destroy(value);
            }
        }
    }

    public GameObject PlayerPrefab => playerPrefab;

    [Header("Prefabs")]
    [SerializeField] private GameObject playerPrefab;

    public int ReadyPlayers => GetReadyPlayers();

    public bool gameHasStarted;
    private bool AllPlayersReady => ReadyPlayers == Player.list.Count && Player.list.Count > 0;
    private bool GameCanStart => AllPlayersReady && !gameHasStarted && SceneManager.GetActiveScene().name != "Lobby";

    private void Awake()
    {
        Singleton = this;
    }

    public int GetReadyPlayers()
    {
        int readyPlayers = 0;

        foreach (Player player in Player.list.Values)
        {
            if (player.isGameReady)
            {
                readyPlayers++;
            }
        }
        
        return readyPlayers;
    }

    private void FixedUpdate()
    {
        if (GameCanStart)
        {
            Debug.Log("starting");
            SendReady();
            gameHasStarted = true;
        }
    }

    private void SendReady()
    {
        Debug.Log(NetworkManager.Singleton.Server.ClientCount);
        Message message = Message.Create(MessageSendMode.Reliable, ServerToClientId.gameStarted);
        NetworkManager.Singleton.Server.SendToAll(message);
    }
    
    private void SendReady(ushort id)
    {
        Debug.Log(NetworkManager.Singleton.Server.ClientCount);
        Message message = Message.Create(MessageSendMode.Reliable, ServerToClientId.gameStarted);
        NetworkManager.Singleton.Server.Send(message, id);
    }

    [MessageHandler((ushort)ClientToServerId.ready)]
    private static void Ready(ushort fromClientId, Message message)
    {
        if (Player.list.TryGetValue(fromClientId, out Player player))
        {
            if (Singleton.gameHasStarted)
            {
                Singleton.SendReady(fromClientId);
            }
            player.isGameReady = true;
        }
    }
}
