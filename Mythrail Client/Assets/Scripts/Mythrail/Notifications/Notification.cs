using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Mythrail.Notifications
{
    public class Notification : MonoBehaviour
    {
        public Image logo;
        public TextMeshProUGUI title;
        public TextMeshProUGUI content;
        [SerializeField] private bool hasReachedResting;
        [SerializeField] private bool timeToGoBack;
        [SerializeField] private bool hasStartedCountdown;

        public event EventHandler Clicked;

        public float stayTime = 2;

        private bool _enabled;

        private void Start()
        {
            GetComponent<Button>().onClick.AddListener(delegate
            {
                Clicked?.Invoke(gameObject, EventArgs.Empty);   
            });
        }

        public void Enable()
        {
            _enabled = true;
        }

        private void Update()
        {
            if(_enabled)
            {
                if (!hasReachedResting)
                {
                    transform.position = Vector3.Lerp(transform.position,
                        NotificationManager.Singleton.endPosObj.transform.position,
                        NotificationManager.Singleton.NotificationAnimationTime * Time.deltaTime);
                }

                if (transform.position.x <= NotificationManager.Singleton.endPosObj.transform.position.x + 1 &&
                    !hasStartedCountdown)
                {
                    hasStartedCountdown = true;
                    hasReachedResting = true;
                    StartCoroutine(WaitTimer());
                }


                if (timeToGoBack)
                {
                    transform.position = Vector3.Lerp(transform.position,
                        NotificationManager.Singleton.startPosObj.transform.position,
                        NotificationManager.Singleton.NotificationAnimationTime * Time.deltaTime);
                }

                if (transform.position.x >= NotificationManager.Singleton.startPosObj.transform.position.x - 1)
                {
                    Debug.Log("Destroying");
                    Destroy(gameObject);
                }
            }
        }
        
        private IEnumerator WaitTimer()
        {
            yield return new WaitForSeconds(stayTime);
            timeToGoBack = true;
        }
    }
   
}