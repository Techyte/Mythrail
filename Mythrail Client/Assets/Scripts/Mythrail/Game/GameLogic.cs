using UnityEngine;

namespace Mythrail.Game
{
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
                    Destroy(value);
                }
            }
        }

        public GameObject PlayerPrefab => playerPrefab;
        public GameObject LocalPlayerPrefab => localPlayerPrefab;
        public GameObject BulletHolePrefab => bulletHolePrefab;
        public GameObject LobbyPlayerPrefab => lobbyPlayerPrefab;
        public GameObject LobbyLocalPlayerPrefab => lobbyLocalPlayerPrefab;

        [Header("Prefabs")]
        [SerializeField] private GameObject localPlayerPrefab;
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject bulletHolePrefab;

        [SerializeField] private GameObject lobbyPlayerPrefab;
        [SerializeField] private GameObject lobbyLocalPlayerPrefab;

        private void Awake()
        {
            Singleton = this;
        }
    }

}