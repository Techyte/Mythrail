using UnityEngine;

namespace MythrailEngine
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
                    Debug.Log($"{nameof(GameLogic)} instance already exists, destroying duplicate!");
                    Destroy(value);
                }
            }
        }

        public GameObject PlayerPrefab => playerPrefab;
        public GameObject LocalPlayerPrefab => localPlayerPrefab;
        public GameObject BulletHolePrefab => bulletHolePrefab;

        [Header("Prefabs")]
        [SerializeField] private GameObject localPlayerPrefab;
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject bulletHolePrefab;

        private void Awake()
        {
            Singleton = this;
        }
    }

}