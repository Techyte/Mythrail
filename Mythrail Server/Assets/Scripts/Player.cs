using UnityEngine;
using Riptide;
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

    public int currentHealth;
    public int maxHealth;

    public bool respawning;

    public float RespawnDelay => respawnDelay;

    [SerializeField] private int respawnDelay = 5;

    public bool isGameReady;
    
    private Vector3 telePos = Vector3.zero;

    private void OnDestroy()
    {
        list.Remove(Id);
    }

    public static void Spawn(ushort id, string username)
    {
        Player player = Instantiate(GameLogic.Singleton.PlayerPrefab, new Vector3(0f, 10f, 0f), Quaternion.identity).GetComponent<Player>();
        
        // making sure that the players username is not empty
        string finalUsername = string.IsNullOrEmpty(username) ? $"Guest {id}" : username;
        
        player.name = $"Player {id}: {finalUsername}";
        player.Id = id;
        player.Username = finalUsername;

        player.currentHealth = player.maxHealth;

        Vector3 spawnPoint = NetworkManager.Singleton.GetRandomSpawnPoint();
        player.transform.position = spawnPoint;

        bool lobby = SceneManager.GetActiveScene().name == "Lobby";
        
        player.SendSpawned(lobby); // tell all clients that a new player spawned
            
        // loop through every other player (this one has not been added to the list yet)
        foreach (Player otherPlayer in list.Values)
        {
            // each other player sends a message to the spawning player with the other players info so they can be displayed on the spawning player
            otherPlayer.SendProxyPlayerSpawnInfo(id, lobby);
        }
        
        list.Add(id, player);
        
        NetworkManager.Singleton.SyncAllClientTicks();

        PlayerMovementState state = new PlayerMovementState();
        state.position = spawnPoint;
        state.inputUsed = new PlayerInput();
        state.didTeleport = false;
        state.tick = NetworkManager.Singleton.CurrentTick;
        state.inputUsed.forward = player.movement.camProxy.forward;
        state.inputUsed.tick = NetworkManager.Singleton.CurrentTick;

        player.movement.SetStateAtTick(NetworkManager.Singleton.CurrentTick, state);
    }

    private void SendSpawned(bool lobby)
    {
        ushort id = lobby ? (ushort)LobbyServerToClientId.playerSpawned : (ushort)ServerToClientId.playerSpawned;

        Message message = AddSpawnData(Message.Create(MessageSendMode.Reliable, id));
        
        NetworkManager.Singleton.Server.SendToAll(message);
    }

    private void SendProxyPlayerSpawnInfo(ushort toClientId, bool lobby)
    {
        ushort id = lobby ? (ushort)LobbyServerToClientId.playerSpawned : (ushort)ServerToClientId.playerSpawned;

        Message message = AddSpawnData(Message.Create(MessageSendMode.Reliable, id));
        
        NetworkManager.Singleton.Server.Send(message, toClientId);
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

        Message message = Message.Create(MessageSendMode.Reliable, ServerToClientId.playerTookDamage);
        message.AddInt(Id);
        NetworkManager.Singleton.Server.SendToAll(message);
        
        SendHealth();

        if (currentHealth <= 0)
        {
            PlayerDied(playerThatShotId, Id);
        }
    }

    public void TakeEditorDamage(int damage)
    {
        currentHealth -= damage;

        Message message = Message.Create(MessageSendMode.Reliable, ServerToClientId.playerTookDamage);
        message.AddInt(Id);
        NetworkManager.Singleton.Server.SendToAll(message);
        
        SendHealth();

        if (currentHealth <= 0)
        {
            Died();
        }
    }

    private void SendHealth()
    {
        Message message = Message.Create(MessageSendMode.Reliable, ServerToClientId.playerHealth);
        message.AddUShort(Id);
        message.AddUShort((ushort)currentHealth);
        message.AddUShort((ushort)maxHealth);
        NetworkManager.Singleton.Server.SendToAll(message);
    }

    private void PlayerDied(ushort playerThatShotId, ushort killedPlayerId)
    {
        SendKilled(playerThatShotId, killedPlayerId);
        Died();
    }

    public void Died()
    {
        StartRespawn();
        
        Message message = Message.Create(MessageSendMode.Reliable, ServerToClientId.playerDied);
        message.AddUShort(Id);
        message.AddInt(respawnDelay);
        NetworkManager.Singleton.Server.SendToAll(message);
        SendHealth();
    }

    private void StartRespawn()
    {
        movement.StartRespawnDelay();
    }

    private void Respawn()
    {
        Vector3 spawnPoint = NetworkManager.Singleton.GetRandomSpawnPoint();
        SetTeleport(spawnPoint);
        currentHealth = maxHealth;
        movement.canMove = true;
        respawning = false;
        SendRegularCam();
        SendHealth();
    }

    public void SetTeleport(Vector3 teleportPos)
    {
        telePos = teleportPos;
        movement.didTeleport = true;
    }

    private void LateUpdate()
    {
        if (telePos != Vector3.zero)
        {
            transform.position = telePos;
            telePos = Vector3.zero;
        }
    }

    private void SendRegularCam()
    {
        Message message = Message.Create(MessageSendMode.Reliable, ServerToClientId.regularCam);
        NetworkManager.Singleton.Server.Send(message, Id);
    }

    private void SendKilled(ushort playerShotId, ushort killedPlayerId)
    {
        Message message = Message.Create(MessageSendMode.Reliable, ServerToClientId.playerKilled);
        message.AddUShort(playerShotId);
        message.AddUShort(killedPlayerId);
        
        NetworkManager.Singleton.Server.SendToAll(message);
    }

    [MessageHandler((ushort)ClientToServerId.register)]
    private static void Name(ushort fromClientId, Message message)
    {
        Spawn(fromClientId, message.GetString());
    }

    [MessageHandler((ushort)ClientToServerId.movementInput)]
    private static void MovementInput(ushort fromClientId, Message message)
    {
        if (list.TryGetValue(fromClientId, out Player player))
        {
            player.Movement.SetInputs(message.GetPlayerInput());
        }
    }

    [MessageHandler((ushort)ClientToLobbyServer.movementInput)]
    private static void LobbyMovementInput(ushort fromClientId, Message message)
    {
        if (list.TryGetValue(fromClientId, out Player player))
        {
            player.Movement.SetInputs(message.GetPlayerInput());
        }
    }

    [MessageHandler((ushort)ClientToServerId.weaponInput)]
    private static void WeaponInput(ushort fromClientId, Message message)
    {
        if (list.TryGetValue(fromClientId, out Player player))
        {
            player.GunManager.SetInputs(message.GetBools(4));
        }
    }

    [MessageHandler((ushort)ClientToServerId.playerWantsToRespawn)]
    private static void PlayerWantsToRespawn(ushort fromClientId, Message message)
    {
        if (list.TryGetValue(fromClientId, out Player player))
        {
            player.Respawn();
        }
    }
}
