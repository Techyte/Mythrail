using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace Mythrail.Notifications
{
    public class NotificationCalledArgs : EventArgs
    {
        public int notificationIndex;
        public Notification notification;

        public NotificationCalledArgs(int notificationIndex, Notification notification)
        {
            this.notificationIndex = notificationIndex;
            this.notification = notification;
        }
    }
    
    public class NotificationManager : MonoBehaviour
    {
        public static NotificationManager Singleton;

        private void Awake()
        {
            Singleton = this;
        }


        private Queue<Notification> _queue;

        public float NotificationAnimationTime = .8f;
        [Space]
        public GameObject startPosObj;
        public GameObject endPosObj;
        [Space]
        [SerializeField] private GameObject notificationSRC;

        private Notification _currentNotification;

        private void Start()
        {
            _queue = new Queue<Notification>();
        }

        public Notification QueNotification(Sprite logo, string title, string content, float stayTime)
        {
            Notification notification = Instantiate(notificationSRC, startPosObj.transform.position, Quaternion.identity, transform).GetComponent<Notification>();
            notification.logo.sprite = logo;
            notification.title.text = title;
            notification.content.text = content;
            notification.stayTime = stayTime;
            
            _queue.Enqueue(notification);
            
            return notification;
        }

        private void Update()
        {
            if (!_currentNotification && _queue.Count > 0)
            {
                Debug.Log("displaying new");
                Notification next = _queue.Dequeue();
                _currentNotification = next;
                next.Enable();
            }
        }
    }
}