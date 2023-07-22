using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Mythrail.Notifications
{
    public class Notification : MonoBehaviour
    {
        public Image Logo => logo;
        public TextMeshProUGUI Title => title;
        public TextMeshProUGUI Content => content;
        
        [SerializeField] private Image logo;
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private TextMeshProUGUI content;
        [SerializeField] private Animator animator;
        [SerializeField] private Button button;
        [SerializeField] private Transform startPos;

        public event EventHandler Clicked;

        public float stayTime = 2;

        [SerializeField] private bool hasStayed = false;

        private void Start()
        {
            button.onClick.AddListener(delegate
            {
                Clicked?.Invoke(gameObject, EventArgs.Empty);
            });

            StartCoroutine(Wait());
        }

        private IEnumerator Wait()
        {
            yield return new WaitForSeconds(stayTime);
            hasStayed = true;
            animator.SetTrigger("TimeElapsed");
        }

        private void Update()
        {
            if (button.transform.position.x == startPos.position.x && hasStayed)
            {
                Destroy(gameObject);
            }
        }
    }
   
}