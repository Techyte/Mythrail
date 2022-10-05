using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace MythrailEngine
{
    public class NotificationCalledArgs : EventArgs
    {
        public int notificationIndex;
        public GameObject notificationObject;

        public NotificationCalledArgs(int notificationIndex, GameObject notificationObject)
        {
            this.notificationIndex = notificationIndex;
            this.notificationObject = notificationObject;
        }
    }
    
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

        public Dictionary<int, NotificationData> que;

        public float NotificationStayTime = 5f;
        public float NotificationAnimationTime = .8f;
        [SerializeField] private float NotificaionCooldown;
        [Space]
        public GameObject startPosObj;
        public GameObject endPosObj;
        [Space]
        [SerializeField] private GameObject notificationSRC;

        private bool canTakeNewNotifications = true;

        public event EventHandler<NotificationCalledArgs> NewNotification;

        private void Start()
        {
            que = new Dictionary<int, NotificationData>();
            DontDestroyOnLoad(gameObject);
        }

        private int currentNotificationIndex;
        public int AddNotificationToQue(Sprite logo, string title, string content)
        {
            if(canTakeNewNotifications)
            {
                currentNotificationIndex++;
                NotificationData data = new NotificationData(logo, title, content, currentNotificationIndex);
                que.Add(currentNotificationIndex, data);

                if (que.Count == 1)
                {
                    Next();
                }

                StartCoroutine(Cooldown());
                return currentNotificationIndex;
            }

            return 0;
        }

        public void Next()
        {
            if (que.Count != 0)
            {
                CreateNotification(que[0]);
            }
        }

        private IEnumerator Cooldown()
        {
            canTakeNewNotifications = false;
            yield return new WaitForSeconds(NotificaionCooldown);
            canTakeNewNotifications = true;
        }

        private void CreateNotification(NotificationData data)
        {
            Notification newNotification = Instantiate(notificationSRC, startPosObj.transform.position,
                Quaternion.identity, transform).GetComponent<Notification>();
            
            newNotification.transform.SetParent(transform);

            newNotification.logo.sprite = data.logo;
            newNotification.title.text = data.title;
            newNotification.content.text = data.content;
            newNotification.index = data.index;
        }
    }

    public class NotificationData
    {
        public Sprite logo;
        public string title;
        public string content;
        public int index;

        public NotificationData(Sprite logo, string title, string content, int index)
        {
            this.logo = logo;
            this.title = title;
            this.content = content;
            this.index = index;
        }
    }
}