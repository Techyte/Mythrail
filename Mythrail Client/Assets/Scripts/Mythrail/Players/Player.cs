using Riptide;
using System.Collections.Generic;
using Mythrail.Game;
using Mythrail.Multiplayer;
using Mythrail.Weapons;
using UnityEngine;
using TMPro;

namespace Mythrail.Players
{
    public class Player : MonoBehaviour
    {
        public static Dictionary<ushort, Player> list = new Dictionary<ushort, Player>();
        public static Player LocalPlayer;

        public ushort Id { get; private set; }
        public bool IsLocal { get; private set; }

        [SerializeField] private Transform camTransform;
        [SerializeField] private Interpolator interpolator;
        [SerializeField] private Transform serverDisplay;
        public GunManager gunManager;

        [SerializeField] private TextMeshPro usernameText;
        [SerializeField] private Camera playerCam;

        [SerializeField] private float runningFOV;
        [SerializeField] private float regularFOV;
        [SerializeField] private float zoomedFOV;

        [SerializeField] private GameObject gunModelHolder;
        private float movementCounter;
        private float idleCounter;

        public int currentHealth;
        public int maxHealth;

        private string username;
        public string Username => username;

        private int kills;
        private int deaths;

        public bool respawning;

        private Vector3 NewPosition;

        public PlayerController playerController;

        private static List<Player> usernameBufferPlayers = new List<Player>();

        [SerializeField] private GameObject crouchingModel;
        [SerializeField] private GameObject defaultModel;

        public CameraController _cameraController => GetComponentInChildren<CameraController>();

        private void Start()
        {
            if (!IsLocal) return;
            gunManager = GetComponent<GunManager>();
            regularFOV = playerCam.fieldOfView;
        }

        private void Update()
        {
            if (!IsLocal) return;

            bool canRun = Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.W);

            if (!Input.GetMouseButton(1))
            {
                playerCam.fieldOfView = canRun
                    ? Mathf.Lerp(playerCam.fieldOfView, runningFOV, .03f)
                    : Mathf.Lerp(playerCam.fieldOfView, regularFOV, .03f);   
            }
            else
            {
                playerCam.fieldOfView = Mathf.Lerp(playerCam.fieldOfView, zoomedFOV, 0.05f);
            }
            
            if (NewPosition == Vector3.zero)
            {
                HeadBob(idleCounter, 0.025f, 0.025f);
                idleCounter += Time.deltaTime;
            }
            else
            {
                HeadBob(movementCounter, 0.05f, 0.05f);
                movementCounter += Time.deltaTime;
            }
        }

        private void OnDestroy()
        {
            list.Remove(Id);
        }

        public void Move(PlayerMovementState state)
        {
            if (!IsLocal)
            {
                transform.position = state.position;
                
                serverDisplay.forward = state.inputUsed.forward;
                serverDisplay.rotation = FlattenQuaternion(serverDisplay.rotation);
            }
            else
            {
                serverDisplay.position = state.position;
            
                playerController.ReceivedServerMovementState(state);
            }
            
            if (state.inputUsed.inputs[6])
            {
                crouchingModel.SetActive(true);
                defaultModel.SetActive(false);
            }
            else
            {
                crouchingModel.SetActive(false);
                defaultModel.SetActive(true);
            }
        }
        
        private Quaternion FlattenQuaternion(Quaternion quaternion)
        {
            quaternion.x = 0;
            quaternion.z = 0;
            return quaternion;
        }

        private void NewHealth(int newHealth, int newMaxHealth)
        {
            currentHealth = newHealth;
            maxHealth = newMaxHealth;
            Debug.Log("Health changed");
        }

        private static void Spawn(ushort id, string username, Vector3 position)
        {
            if (list.ContainsKey(id)) return;
            
            Player player;
            if (NetworkManager.Singleton.Client.Id == id)
            {
                player = Instantiate(GameLogic.Singleton.LocalPlayerPrefab, position, Quaternion.identity).GetComponent<Player>();
                player.IsLocal = true;
                LocalPlayer = player;
                player.playerController.canMove = false;
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

            if(!player.IsLocal)
            {
                player.usernameText.GetComponent<ObjectLookAt>().target = player.camTransform;
                player.usernameText.text = player.username;
            }

            if (!player.IsLocal && !LocalPlayer)
            {
                usernameBufferPlayers.Add(player);
            }

            if (!player.IsLocal && LocalPlayer)
            {
                player.usernameText.GetComponent<ObjectLookAt>().target = LocalPlayer.transform;
            }
            
            if(player.IsLocal && LocalPlayer)
            {
                NetworkManager.Singleton.Ready();
                UIManager.Singleton.hudUsernameDisplay.text = LocalPlayer.username;
                foreach (Player bufferPlayer in usernameBufferPlayers)
                {
                    bufferPlayer.usernameText.GetComponent<ObjectLookAt>().target = LocalPlayer.transform;
                }
                usernameBufferPlayers.Clear();
            }
        }

        private void Killed(ushort killedPlayerId)
        {
            kills++;
            Debug.Log($"{name} killed {list[killedPlayerId].name}");
            UpdateKillsAndDeaths();
        }

        private void Died(int respawnDelay)
        {
            Debug.Log("Died");
            respawning = true;
            deaths++;
            SetPlayerDeadModel();
            if (this == LocalPlayer)
            {
                UpdateKillsAndDeaths();
                ShowRespawnScreen(respawnDelay);
            }
        }

        private void ShowRespawnScreen(int respawnDelay)
        {
            UIManager.Singleton.OpenRespawnScreen(respawnDelay);
        }

        private void SetPlayerDeadModel()
        {
            // TODO: set player dead model
        }

        private void UpdateKillsAndDeaths()
        {
            UIManager.Singleton.deathsText.text = deaths.ToString();
            UIManager.Singleton.killsText.text = kills.ToString();
        }
        
        private void HeadBob(float z, float xIntensity, float yIntensity)
        {
            gunModelHolder.transform.localPosition = gunManager.weaponModels[gunManager.currentWeaponIndex].transform.localPosition + new Vector3 (Mathf.Cos(z) * xIntensity, Mathf.Sin(z * 2) * yIntensity, 0);
        }

        private void TookDamage()
        {
            Debug.Log($"{username} took damage");
        }

        private void SendDevMessage(int id)
        {
            Message message = Message.Create(MessageSendMode.Reliable, ClientToServerId.clientDevMessage);
            message.AddInt(id); // kill
            NetworkManager.Singleton.Client.Send(message);
        }

        public void SendKillDevMessage()
        {
            SendDevMessage(0); // id for kil
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
            {
                player.Move(message.GetPlayerState());
            }
        }

        [MessageHandler((ushort)ServerToClientId.playerTookDamage)]
        private static void PlayerTookDamageHandler(Message message)
        {
            if (list.TryGetValue(message.GetUShort(), out Player player))
            {
                player.TookDamage();
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

        [MessageHandler((ushort)ServerToClientId.playerKilled)]
        private static void PlayerKilled(Message message)
        {
            if (list.TryGetValue(message.GetUShort(), out Player player))
                player.Killed(message.GetUShort());
        }

        [MessageHandler((ushort)ServerToClientId.playerDied)]
        private static void PlayerDied(Message message)
        {
            if(list.TryGetValue(message.GetUShort(), out Player player))
                player.Died(message.GetInt());
        }

        [MessageHandler((ushort)ServerToClientId.playerHealth)]
        private static void PlayerHealth(Message message)
        {
            if (list.TryGetValue(message.GetUShort(), out Player player))
                player.NewHealth(message.GetUShort(), message.GetUShort());
        }

        [MessageHandler((ushort)ServerToClientId.playerCanRespawn)]
        private static void PlayerCanRespawn(Message message)
        {
            UIManager.Singleton.CanRespawn();
        }

        [MessageHandler((ushort)ServerToClientId.regularCam)]
        private static void RegularCam(Message message)
        {
            UIManager.Singleton.Respawned();
        }
    }
}