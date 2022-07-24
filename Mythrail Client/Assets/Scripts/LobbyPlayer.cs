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

        private int kills;
        private int deaths;
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
                camTransform.forward = forward;
        }

        private static void Spawn(ushort id, string username, Vector3 position)
        {
            LobbyPlayer player;
            if (id == LobbyNetworkManager.Singleton.Client.Id)
            {
                player = Instantiate(GameLogic.Singleton.LobbyLocalPlayerPrefab, position, Quaternion.identity).GetComponent<LobbyPlayer>();
                player.IsLocal = true;
                LocalPlayer = player;
            }
            else
            {
                player = Instantiate(GameLogic.Singleton.LobbyPlayerPrefab, position, Quaternion.identity).GetComponent<LobbyPlayer>();
                player.IsLocal = false;
            }

            player.name = $"Player {id} ({(string.IsNullOrEmpty(username) ? "Guest" : username)})";
            player.Id = id;
            player.username = username;

            list.Add(id, player);

            player.usernameText.GetComponent<ObjectLookAt>().target = player.camTransform;

            foreach (LobbyPlayer gotPlayer in list.Values)
                gotPlayer.usernameText.text = gotPlayer.username;

            if (!player.IsLocal)
                player.usernameText.GetComponent<ObjectLookAt>().target = LocalPlayer.transform;
        }

        [MessageHandler((ushort)LobbyServerToClient.playerSpawned)]
        private static void SpawnPlayer(Message message)
        {
            Spawn(message.GetUShort(), message.GetString(), message.GetVector3());
        }

        [MessageHandler((ushort)LobbyServerToClient.playerMovement)]
        private static void PlayerMovement(Message message)
        {
            Debug.Log("Received lobby movement");
            if (list.TryGetValue(message.GetUShort(), out LobbyPlayer player))
                player.Move(message.GetUInt(), message.GetBool(), message.GetVector3(), message.GetVector3());
        }
    }
}