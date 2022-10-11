using UnityEngine;

namespace MythrailEngine
{
    public class ObjectLookAt : MonoBehaviour
    {
        public Transform target;

        void Update()
        {
            if(target)
            {
                transform.LookAt(transform.position + target.forward);
            }
        }
    }
}
