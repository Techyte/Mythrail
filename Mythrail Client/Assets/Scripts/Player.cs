using System;
using RiptideNetworking;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Mythrail
{
    public class Player : MonoBehaviour
    {
        public static Dictionary<ushort, Player> list = new Dictionary<ushort, Player>();
        public static Player LocalPlayer;

        public ushort Id { get; private set; }
        public bool IsLocal { get; private set; }

        [SerializeField] private Transform camTransform;
        [SerializeField] private Interpolator interpolator;
        public GunManager gunManager;

        [SerializeField] private TextMeshPro usernameText;
        [SerializeField] private Camera playerCam;

        [SerializeField] private float runningFOV;
        [SerializeField] private float regularFOV;

        public int currentHealth;
        public int maxHealth;
        public bool isRunning;

        private string username;

        private void Start()
        {
            if (!IsLocal) return;
            runningFOV = playerCam.fieldOfView;
        }

        private void Update()
        {
            if (!IsLocal) return;
            playerCam.fieldOfView = isRunning
                ? Mathf.Lerp(playerCam.fieldOfView, runningFOV, Time.deltaTime * 8f)
                : Mathf.Lerp(playerCam.fieldOfView, regularFOV, Time.deltaTime * 8f);
        }

        private void OnDestroy()
        {
            list.Remove(Id);
        }

        private void Move(uint tick, bool didTeleport, Vector3 newPosition, Vector3 forward)
        {
            interpolator.NewUpdate(tick, didTeleport, newPosition);

            if (!IsLocal)
            {
                camTransform.forward = forward;
            }
        }

        private void NewHealth(int newHealth, int newMaxHealth)
        {
            currentHealth = newHealth;
            this.maxHealth = newMaxHealth;
            Debug.Log("Health changed");
        }

        private static void Spawn(ushort id, string username, Vector3 position)
        {
            Player player;
            if (id == NetworkManager.Singleton.Client.Id)
            {
                player = Instantiate(GameLogic.Singleton.LocalPlayerPrefab, position, Quaternion.identity).GetComponent<Player>();
                player.IsLocal = true;
                LocalPlayer = player;
            }
            else
            {
                player = Instantiate(GameLogic.Singleton.PlayerPrefab, position, Quaternion.identity).GetComponent<Player>();
                player.IsLocal = false;
            }

            player.name = $"Player {id} ({(string.IsNullOrEmpty(username) ? "Guest" : username)})";
            player.Id = id;
            player.username = username;

            list.Add(id, player);

            player.usernameText.GetComponent<ObjectLookAt>().target = player.camTransform;

            foreach (Player gotPlayer in list.Values)
            {
                gotPlayer.usernameText.text = gotPlayer.username;
            }

            if (!player.IsLocal)
            {
                player.usernameText.GetComponent<ObjectLookAt>().target = LocalPlayer.transform;
            }
        }

        private void FixedUpdate()
        {
            if (!IsLocal)
                return;
        }

        [MessageHandler((ushort)ServerToClientId.playerSpawned)]
        private static void SpawnPlayer(Message message)
        {
            Spawn(message.GetUShort(), message.GetString(), message.GetVector3());
        }

        [MessageHandler((ushort)ServerToClientId.playerMovement)]
        private static void PlayerMovement(Message message)
        {
            if (list.TryGetValue(message.GetUShort(), out Player player))
                player.Move(message.GetUInt(), message.GetBool(), message.GetVector3(), message.GetVector3());
        }

        [MessageHandler((ushort)ServerToClientId.playerTookDamage)]
        private static void PlayerTookDamage(Message message)
        {
            if (list.TryGetValue(message.GetUShort(), out Player player))
            {
                player.NewHealth(message.GetInt(), message.GetInt());
            }
        }

        [MessageHandler((ushort)ServerToClientId.bulletHole)]
        private static void HandleHitInfo(Message message)
        {
            Vector3 point = message.GetVector3();
            Vector3 normal = message.GetVector3();
            
            GameObject newHole = Instantiate(GameLogic.Singleton.BulletHolePrefab, point + normal * 0.001f, Quaternion.identity);
            newHole.transform.LookAt(point + normal);
            Destroy(newHole, 5);
        }
    }
}