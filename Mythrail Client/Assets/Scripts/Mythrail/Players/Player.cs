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

        private Vector3 NewPosition;

        public PlayerController playerController;

        private static List<Player> usernameBufferPlayers = new List<Player>();

        [SerializeField] private GameObject crouchingModel;
        [SerializeField] private GameObject defaultModel;

        [SerializeField] private Transform crouchingCameraPos;
        [SerializeField] private Transform defaultCameraPos;

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

        private void Move(uint tick, bool didTeleport, Vector3 newPosition, Vector3 forward, bool isCrouching)
        {
            interpolator.NewUpdate(tick, didTeleport, newPosition);

            if (!IsLocal)
            { 
                camTransform.forward = forward;
                camTransform.rotation = FlattenQuaternion(camTransform.rotation);
            }
            
            if (isCrouching)
            {
                crouchingModel.SetActive(true);
                defaultModel.SetActive(false);
                if(IsLocal)
                {
                    camTransform.position = crouchingCameraPos.position;
                }
            }
            else
            {
                crouchingModel.SetActive(false);
                defaultModel.SetActive(true);
                if(IsLocal)
                {
                    camTransform.position = defaultCameraPos.position;
                }
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
                if (NetworkManager.hasBeenReadyOnce)
                {
                    NetworkManager.Singleton.Ready();
                }
                UIManager.Singleton.HUDUsernameDisplay.text = LocalPlayer.username;
                foreach (Player bufferPlayer in usernameBufferPlayers)
                {
                    Debug.Log(bufferPlayer);
                    Debug.Log(bufferPlayer.usernameText);
                    Debug.Log(bufferPlayer.usernameText.GetComponent<ObjectLookAt>());
                    Debug.Log(LocalPlayer);
                    Debug.Log(LocalPlayer.transform);
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

        private void Died()
        {
            deaths++;
            if (this != LocalPlayer) return;
            UpdateKillsAndDeaths();
        }

        private void UpdateKillsAndDeaths()
        {
            UIManager.Singleton.DeathsText.text = deaths.ToString();
            UIManager.Singleton.KillsText.text = kills.ToString();
        }
        
        private void HeadBob(float z, float xIntensity, float yIntensity)
        {
            gunModelHolder.transform.localPosition = gunManager.weaponModels[gunManager.currentWeaponIndex].transform.localPosition + new Vector3 (Mathf.Cos(z) * xIntensity, Mathf.Sin(z * 2) * yIntensity, 0);
        }

        private void TookDamage()
        {
            Debug.Log($"{username} took damage");
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
                player.Move(message.GetUInt(), message.GetBool(), message.GetVector3(), message.GetVector3(), message.GetBool());
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
                player.Died();
        }

        [MessageHandler((ushort)ServerToClientId.playerHealth)]
        private static void PlayerHealth(Message message)
        {
            if (list.TryGetValue(message.GetUShort(), out Player player))
                player.NewHealth(message.GetUShort(), message.GetUShort());
        }
    }
}