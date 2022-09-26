using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

namespace MythrailEngine
{
    public class NotificationManager : MonoBehaviour
    {
        private static NotificationManager _singleton;
        public static NotificationManager Singleton
        {
            get => _singleton;
            private set
            {
                if (_singleton == null)
                    _singleton = value;
                else if (_singleton != value)
                {
                    Debug.Log($"{nameof(NotificationManager)} instance already exists, destroying duplicate!");
                    Destroy(value);
                }
            }
        }

        private void Awake()
        {
            Singleton = this;
            SceneManager.sceneLoaded += (arg0, mode) =>
            {
                Singleton = this;
            };
        }

        private List<NotificationData> que;

        public float NotificationStayTime = 5f;
        public float NotificationAnimationTime = .8f;
        public GameObject startPosObj;
        public GameObject endPosObj;
        [Space]
        [SerializeField] private GameObject notificationSRC;

        private void Start()
        {
            que = new List<NotificationData>();
            DontDestroyOnLoad(gameObject);
        }

        public void AddNotificationToQue(Sprite logo, string title, string content)
        {
            NotificationData data = new NotificationData(logo, title, content);
            que.Add(data);

            if (que.Count == 1)
            {
                Next();
            }
        }

        public void Next()
        {
            if (que.Count != 0)
            {
                CreateNotification(que[0]);
                que.RemoveAt(0);
            }
        }

        private void CreateNotification(NotificationData data)
        {
            Notification newNotification = Instantiate(notificationSRC, startPosObj.transform.position,
                Quaternion.identity, transform).GetComponent<Notification>();
            
            newNotification.transform.SetParent(transform);

            newNotification.logo.sprite = data.logo;
            newNotification.title.text = data.title;
            newNotification.content.text = data.content;
        }
    }

    [Serializable]
    public class NotificationData
    {
        public Sprite logo;
        public string title;
        public string content;

        public NotificationData(Sprite logo, string title, string content)
        {
            this.logo = logo;
            this.title = title;
            this.content = content;
        }
    }
}