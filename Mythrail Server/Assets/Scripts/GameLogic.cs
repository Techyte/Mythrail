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

    public int readyPlayers;

    public bool gameHasStarted;

    private void Awake()
    {
        Singleton = this;
    }

    public void PlayerLeftWhileLoading()
    {
        readyPlayers--;
    }

    private void FixedUpdate()
    {
        if (readyPlayers >= NetworkManager.Singleton.Server.ClientCount && SceneManager.GetActiveScene().buildIndex != 0 && NetworkManager.Singleton.Server.ClientCount > 0 && !gameHasStarted)
        {
            SendReady();
            gameHasStarted = true;
        }
    }

    private void SendReady()
    {
        Message message = Message.Create(MessageSendMode.Reliable, ServerToClientId.gameStarted);
        NetworkManager.Singleton.Server.SendToAll(message);
    }

    [MessageHandler((ushort)ClientToServerId.ready)]
    private static void Ready(ushort fromClientId, Message message)
    {
        if (Player.list.TryGetValue(fromClientId, out Player player))
        {
            Singleton.readyPlayers++;
            player.isGameReady = true;

            if (Singleton.gameHasStarted)
            {
                Message readyMessage = Message.Create(MessageSendMode.Reliable, ServerToClientId.gameStarted);
                NetworkManager.Singleton.Server.Send(readyMessage, fromClientId);
            }
        }
    }
}
