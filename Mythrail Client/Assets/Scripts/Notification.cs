using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MythrailEngine
{
    public class Notification : MonoBehaviour
    {
        public Image logo;
        public TextMeshProUGUI title;
        public TextMeshProUGUI content;
        [SerializeField] private bool hasReachedResting;
        [SerializeField] private bool timeToGoBack;
        [SerializeField] private bool hasStartedCountdown;
        private float localLerpAdd;

        private void Update()
        {
            if (!hasReachedResting)
            {
                Debug.Log("coming!");
                transform.position = Vector3.Lerp(transform.position, NotificationManager.Singleton.endPosObj.transform.position, NotificationManager.Singleton.NotificationAnimationTime * Time.deltaTime);
            }

            if (transform.position.x <= NotificationManager.Singleton.endPosObj.transform.position.x+1 && !hasStartedCountdown)
            {
                hasStartedCountdown = true;
                Debug.Log("finished animating");
                hasReachedResting = true;
                StartCoroutine(WaitTimer());
            }

            
            if (timeToGoBack)
            {
                Debug.Log("going back");
                transform.position = Vector3.Lerp(transform.position, NotificationManager.Singleton.startPosObj.transform.position, NotificationManager.Singleton.NotificationAnimationTime * Time.deltaTime);
            }

            if (transform.position.x >= NotificationManager.Singleton.startPosObj.transform.position.x-1)
            {
                NotificationManager.Singleton.Next();
                Destroy(gameObject);
            }
        }

        private IEnumerator WaitTimer()
        {
            yield return new WaitForSeconds(NotificationManager.Singleton.NotificationStayTime);
            timeToGoBack = true;
        }
    }
   
}