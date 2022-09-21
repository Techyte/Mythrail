using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MythrailEngine
{
    public class UIManager : MonoBehaviour
    {
        private static UIManager _singleton;
        public static UIManager Singleton
        {
            get => _singleton;
            private set
            {
                if (_singleton == null)
                    _singleton = value;
                else if (_singleton != value)
                {
                    Debug.Log($"{nameof(UIManager)} instance already exists, destroying duplicate!");
                    Destroy(value);
                }
            }
        }

        [Header("Connect")]
        [SerializeField] private Transform UIHealthBar;

        [SerializeField] private TextMeshProUGUI username;

        public GameObject loadingScreen;

        private void Awake()
        {
            Singleton = this;
            
            SceneManager.sceneLoaded += LoadedGame;
        }

        private void LoadedGame(Scene scene, LoadSceneMode loadSceneMode)
        {
            if(scene.buildIndex == 2)
            {
                UIHealthBar = GameObject.Find("Health").GetComponentInChildren<Image>().transform;
                username = GameObject.Find("Username").GetComponentInChildren<TextMeshProUGUI>();
                loadingScreen = GameObject.Find("Loading");
            }
        }

        public void UpdateUsername()
        {
            username.text = Player.LocalPlayer.Username;
        }

        private void Update()
        {
            if (Player.LocalPlayer==null) return;
            RefreshHealthBar();
        }

        void RefreshHealthBar()
        {
            float healthRatio = (float)Player.LocalPlayer.currentHealth / Player.LocalPlayer.maxHealth;
            UIHealthBar.localScale = Vector3.Lerp(UIHealthBar.localScale, new Vector3(healthRatio, 1, 1), Time.deltaTime * 8f);

            UIHealthBar.GetComponent<Image>().color = Player.LocalPlayer.currentHealth <= 10 ? Color.red : Color.green;
        }
    }

}