using RiptideNetworking;
using UnityEngine;
using TMPro;
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
        [SerializeField] private GameObject connectUI;
        [SerializeField] private GameObject gameUI;
        [SerializeField] private TMP_InputField usernameField;
        public TMP_InputField port;
        [SerializeField] private Transform UIHealthBar;

        private void Awake()
        {
            Singleton = this;
        }

        private void Update()
        {
            if (Player.LocalPlayer==null) return;
            RefreshHealthBar();
        }

        public void ConnectClicked()
        {
            usernameField.interactable = false;
            connectUI.SetActive(false);
            gameUI.SetActive(true);

            NetworkManager.Singleton.Connect();
        }

        public void BackToMain()
        {
            usernameField.interactable = true;
            connectUI.SetActive(true);
            gameUI.SetActive(false);
        }

        public void SendName()
        {
            Message message = Message.Create(MessageSendMode.unreliable, ClientToServerId.name);
            message.AddString(usernameField.text);
            NetworkManager.Singleton.Client.Send(message);
        }

        void RefreshHealthBar()
        {
            float healthRatio = (float)Player.LocalPlayer.currentHealth / Player.LocalPlayer.maxHealth;
            UIHealthBar.localScale = Vector3.Lerp(UIHealthBar.localScale, new Vector3(healthRatio, 1, 1), Time.deltaTime * 8f);

            UIHealthBar.GetComponent<Image>().color = Player.LocalPlayer.currentHealth <= 10 ? Color.red : Color.green;
        }
    }

}