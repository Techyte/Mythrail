using System;
using RiptideNetworking;
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

    private int readyPlayers;

    public bool gameHasStarted;

    private void Awake()
    {
        Singleton = this;
    }

    private void FixedUpdate()
    {
        if (readyPlayers == NetworkManager.Singleton.Server.ClientCount && SceneManager.GetActiveScene().buildIndex != 0 && NetworkManager.Singleton.Server.ClientCount > 0 && !gameHasStarted)
        {
            Debug.LogError("Everyone is ready");
            SendReady();
            gameHasStarted = true;
        }
    }

    private void SendReady()
    {
        Message message = Message.Create(MessageSendMode.reliable, ServerToClientId.gameStarted);
        NetworkManager.Singleton.Server.SendToAll(message);
    }

    [MessageHandler((ushort)ClientToServerId.ready)]
    private static void Ready(ushort fromClientId, Message message)
    {
        if (Player.list.TryGetValue(fromClientId, out Player player))
        {
            Debug.LogError($"{player} is ready");
            Singleton.readyPlayers++;
        }
    }
}
