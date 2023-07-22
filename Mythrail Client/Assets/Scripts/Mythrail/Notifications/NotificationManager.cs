using System.Collections.Generic;
using UnityEngine;

namespace Mythrail.Notifications
{
    public class NotificationManager : MonoBehaviour
    {
        public static NotificationManager Singleton;

        private void Awake()
        {
            Singleton = this;
        }


        private Queue<Notification> _queue;

        [Space]
        [SerializeField] private GameObject notificationSRC;

        [SerializeField] private Transform notificationHolder;

        public Notification CreateNotification(Sprite logo, string title, string content, float stayTime)
        {
            Notification notification = Instantiate(notificationSRC, notificationHolder).GetComponent<Notification>();
            notification.Logo.sprite = logo;
            notification.Title.text = title;
            notification.Content.text = content;
            notification.stayTime = stayTime;
            
            return notification;
        }
    }
}