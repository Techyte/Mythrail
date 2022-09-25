using System.Collections;
using UnityEngine;
using RiptideNetworking;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    public static Dictionary<ushort, Player> list = new Dictionary<ushort, Player>();

    public ushort Id { get; private set; }
    public string Username { get; private set; }

    public PlayerMovement Movement => movement;
    public GunManager GunManager => gunManager;

    [SerializeField] private PlayerMovement movement;
    [SerializeField] private GunManager gunManager;
    [SerializeField] private Rigidbody rb;

    public int currentHealth;
    public int maxHealth;

    [SerializeField] private float respawnDelay = 5f;

    public bool isGameReady;

    private void OnDestroy()
    {
        list.Remove(Id);
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    public static void Spawn(ushort id, string username)
    {
        Player player = Instantiate(GameLogic.Singleton.PlayerPrefab, new Vector3(0f, 10f, 0f), Quaternion.identity).GetComponent<Player>();
        player.name = $"Player {id} ({(string.IsNullOrEmpty(username) ? "Guest" : username)})";
        player.Id = id;
        player.Username = string.IsNullOrEmpty(username) ? $"Guest {id}" : username;

        player.currentHealth = player.maxHealth;

        if (SceneManager.GetActiveScene().buildIndex == 0)
        {
            player.SendLobbySpawned();
            foreach (Player otherPlayer in list.Values)
            {
                otherPlayer.SendLobbyProxyPlayerSpawnInfo(id);
            }
        }
        else
        {
            player.SendSpawned();
            foreach (Player otherPlayer in list.Values)
            {
                otherPlayer.SendProxyPlayerSpawnInfo(id);
            }
        }
        list.Add(id, player);
    }

    private void SendSpawned()
    {
        Debug.LogError("Spawning local player");
        NetworkManager.Singleton.Server.SendToAll(AddSpawnData(Message.Create(MessageSendMode.reliable, ServerToClientId.playerSpawned)));
    }

    private void SendProxyPlayerSpawnInfo(ushort toClientId)
    {
        Debug.LogError("Spawning proxy player");
        NetworkManager.Singleton.Server.Send(AddSpawnData(Message.Create(MessageSendMode.reliable, ServerToClientId.playerSpawned)), toClientId);
    }

    private void SendLobbySpawned()
    {
        NetworkManager.Singleton.Server.SendToAll(AddSpawnData(Message.Create(MessageSendMode.reliable, LobbyServerToClient.playerSpawned)));
    }

    private void SendLobbyProxyPlayerSpawnInfo(ushort toClientId)
    {
        NetworkManager.Singleton.Server.Send(AddSpawnData(Message.Create(MessageSendMode.reliable, LobbyServerToClient.playerSpawned)), toClientId);
    }

    private Message AddSpawnData(Message message)
    {
        message.AddUShort(Id);
        message.AddString(Username);
        message.AddVector3(transform.position);
        return message;
    }

    public void TakeDamage(int damage, ushort playerThatShotId)
    {
        currentHealth -= damage;
        Debug.LogError(Username);

        Message message = Message.Create(MessageSendMode.reliable, ServerToClientId.playerTookDamage);
        message.AddInt(Id);
        NetworkManager.Singleton.Server.SendToAll(message);
        
        SendHealth();

        if (currentHealth <= 0)
        {
            PlayerDied(playerThatShotId, Id);
        }
    }

    private void SendHealth()
    {
        Message message = Message.Create(MessageSendMode.reliable, ServerToClientId.playerHealth);
        message.AddUShort(Id);
        message.AddUShort((ushort)currentHealth);
        message.AddUShort((ushort)maxHealth);
        NetworkManager.Singleton.Server.SendToAll(message);
    }

    private IEnumerator RespawnTimer()
    {
        movement.camMove = false;
        yield return new WaitForSeconds(respawnDelay);
        Debug.Log("Respawned");
    }

    private void PlayerDied(ushort playerThatShotId, ushort killedPlayerId)
    {
        SendKilled(playerThatShotId, killedPlayerId);
        StartCoroutine(RespawnTimer());
        Died();
    }

    public void Died()
    {   
        rb.velocity = Vector3.zero;
        transform.position = new Vector3(0, 10, 0);
        currentHealth = maxHealth;
        
        Message message = Message.Create(MessageSendMode.reliable, ServerToClientId.playerDied);
        message.AddUShort(Id);
        NetworkManager.Singleton.Server.SendToAll(message);
        SendHealth();
    }

    private void SendKilled(ushort playerShotId, ushort killedPlayerId)
    {
        Message message = Message.Create(MessageSendMode.reliable, ServerToClientId.playerKilled);
        message.AddUShort(playerShotId);
        message.AddUShort(killedPlayerId);
        
        NetworkManager.Singleton.Server.SendToAll(message);
    }

    [MessageHandler((ushort)ClientToServerId.name)]
    private static void Name(ushort fromClientId, Message message)
    {
        Spawn(fromClientId, message.GetString());
    }

    [MessageHandler((ushort)ClientToServerId.movementInput)]
    private static void MovementInput(ushort fromClientId, Message message)
    {
        Debug.Log("Received movement input");
        if (list.TryGetValue(fromClientId, out Player player))
        {
            Debug.Log("Found the player");
            player.Movement.SetInputs(message.GetBools(6), message.GetVector3());
        }
    }

    [MessageHandler((ushort)ClientToLobbyServer.movementInput)]
    private static void LobbyMovementInput(ushort fromClientId, Message message)
    {
        if (list.TryGetValue(fromClientId, out Player player))
        {
            player.Movement.SetInputs(message.GetBools(6), message.GetVector3());
        }
    }

    [MessageHandler((ushort)ClientToServerId.weaponInput)]
    private static void WeaponInput(ushort fromClientId, Message message)
    {
        if (list.TryGetValue(fromClientId, out Player player))
        {
            player.GunManager.SetInputs(message.GetBools(3));
        }
    }
}
