using RiptideNetworking;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace MythrailEngine
{
    public class LobbyPlayer : MonoBehaviour
    {
        public static Dictionary<ushort, LobbyPlayer> list = new Dictionary<ushort, LobbyPlayer>();
        public static LobbyPlayer LocalPlayer;

        public ushort Id { get; private set; }
        public bool IsLocal { get; private set; }

        [SerializeField] private Transform camTransform;
        [SerializeField] private Interpolator interpolator;

        [SerializeField] private TextMeshPro usernameText;
        [SerializeField] private Camera playerCam;

        [SerializeField] private float runningFOV;
        [SerializeField] private float regularFOV;

        private float movementCounter;
        private float idleCounter;

        private string username;
        
        public static List<LobbyPlayer> usernameBufferPlayers = new List<LobbyPlayer>();
        private void Start()
        {
            if (!IsLocal) return;
            regularFOV = playerCam.fieldOfView;
        }

        private void Update()
        {
            if (!IsLocal) return;

            bool canRun = Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.W);

            playerCam.fieldOfView = canRun
                ? Mathf.Lerp(playerCam.fieldOfView, runningFOV, .03f)
                : Mathf.Lerp(playerCam.fieldOfView, regularFOV, .03f);
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
                camTransform.rotation = FlattenQuaternion(camTransform.rotation);
            }
        }
        
        private Quaternion FlattenQuaternion(Quaternion quaternion)
        {
            quaternion.x = 0;
            quaternion.z = 0;
            return quaternion;
        }

        private static void Spawn(ushort id, string username, Vector3 position, bool isLocal)
        {
            LobbyPlayer lobbyPlayer;
            if (NetworkManager.Singleton.Client.Id == id)
            {
                lobbyPlayer = Instantiate(GameLogic.Singleton.LobbyLocalPlayerPrefab, position, Quaternion.identity).GetComponent<LobbyPlayer>();
                lobbyPlayer.IsLocal = true;
                LocalPlayer = lobbyPlayer;
            }
            else
            {
                lobbyPlayer = Instantiate(GameLogic.Singleton.LobbyPlayerPrefab, position, Quaternion.identity).GetComponent<LobbyPlayer>();
                lobbyPlayer.IsLocal = false;
            }

            lobbyPlayer.name = $"Player {id} ({(string.IsNullOrEmpty(username) ? "Guest" : username)})";
            lobbyPlayer.Id = id;
            lobbyPlayer.username = username;

            list.Add(id, lobbyPlayer);

            if(!lobbyPlayer.IsLocal)
            {
                lobbyPlayer.usernameText.GetComponent<ObjectLookAt>().target = lobbyPlayer.camTransform;
                lobbyPlayer.usernameText.text = lobbyPlayer.username;
            }

            if (!lobbyPlayer.IsLocal && !LocalPlayer)
            {
                usernameBufferPlayers.Add(lobbyPlayer);
            }

            if (!lobbyPlayer.IsLocal && LocalPlayer)
            {
                lobbyPlayer.usernameText.GetComponent<ObjectLookAt>().target = LocalPlayer.transform;
            }
            
            if(lobbyPlayer.IsLocal && LocalPlayer)
            {
                foreach (LobbyPlayer bufferPlayer in usernameBufferPlayers)
                {
                    bufferPlayer.usernameText.GetComponent<ObjectLookAt>().target = LocalPlayer.transform;
                }
                usernameBufferPlayers.Clear();
            }
        }

        [MessageHandler((ushort)LobbyServerToClient.playerSpawned)]
        private static void SpawnPlayer(Message message)
        {
            Spawn(message.GetUShort(), message.GetString(), message.GetVector3(), true);
        }

        [MessageHandler((ushort)LobbyServerToClient.playerMovement)]
        private static void PlayerMovement(Message message)
        {
            if (list.TryGetValue(message.GetUShort(), out LobbyPlayer player))
                player.Move(message.GetUInt(), message.GetBool(), message.GetVector3(), message.GetVector3());
        }
    }
}