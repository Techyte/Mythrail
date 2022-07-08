using UnityEngine;

namespace MythrailEngine
{
    public class ObjectLookAt : MonoBehaviour
    {
        public Transform target;

        void Update()
        {
            Vector3 dir = target.position - transform.position;
            Quaternion lookRot = Quaternion.LookRotation(dir);
            lookRot.x = 0; lookRot.z = 0;
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Mathf.Clamp01(3.0f * Time.maximumDeltaTime));
        }
    }
}
