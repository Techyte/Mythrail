using System.Collections;
using UnityEngine;
using RiptideNetworking;
using System.Collections.Generic;

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

    [SerializeField] private float respawnDelay = 5f;

    private void OnDestroy()
    {
        list.Remove(Id);
    }

    public static void Spawn(ushort id, string username)
    {
        Player player = Instantiate(GameLogic.Singleton.PlayerPrefab, new Vector3(0f, 10f, 0f), Quaternion.identity).GetComponent<Player>();
        player.name = $"Player {id} ({(string.IsNullOrEmpty(username) ? "Guest" : username)})";
        player.Id = id;
        player.Username = string.IsNullOrEmpty(username) ? $"Guest {id}" : username;

        player.currentHealth = player.maxHealth;

        player.SendSpawned();

        foreach (Player otherPlayer in list.Values)
            otherPlayer.SendSpawned(id);

        list.Add(id, player);
    }

    private void SendSpawned()
    {
        NetworkManager.Singleton.Server.SendToAll(AddSpawnData(Message.Create(MessageSendMode.reliable, ServerToClientId.playerSpawned)));
    }

    private void SendSpawned(ushort toClientId)
    {
        NetworkManager.Singleton.Server.Send(AddSpawnData(Message.Create(MessageSendMode.reliable, ServerToClientId.playerSpawned)), toClientId);
    }

    private Message AddSpawnData(Message message)
    {
        message.AddUShort(Id);
        message.AddString(Username);
        message.AddVector3(transform.position);
        return message;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log(currentHealth);

        Message message = Message.Create(MessageSendMode.reliable, ServerToClientId.playerTookDamage);
        message.AddInt(Id);
        message.AddInt(currentHealth);
        message.AddInt(maxHealth);
        NetworkManager.Singleton.Server.SendToAll(message);

        if (currentHealth <= 0)
        {
            PlayerDied();
        }
    }

    private IEnumerator Respawn()
    {
        movement.camMove = false;
        yield return new WaitForSeconds(respawnDelay);
        Debug.Log("Respawned");
    }

    private void PlayerDied()
    {
        Message message = Message.Create(MessageSendMode.reliable, ServerToClientId.playerDied);
        message.AddUShort(Id);
        
        NetworkManager.Singleton.Server.SendToAll(message);
        StartCoroutine(Respawn());
    }

    [MessageHandler((ushort)ClientToServerId.name)]
    private static void Name(ushort fromClientId, Message message)
    {
        Spawn(fromClientId, message.GetString());
    }

    [MessageHandler((ushort)ClientToServerId.movementInput)]
    private static void MovementInput(ushort fromClientId, Message message)
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
            player.GunManager.SetInputs(message.GetBools(2), message.GetFloat());
        }
    }
}
