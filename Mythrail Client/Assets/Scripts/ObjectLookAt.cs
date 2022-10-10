using System;
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
                Vector3 dir = target.position - transform.position;
                Debug.Log(dir.sqrMagnitude);
                if(dir.sqrMagnitude > 0.000001)
                {
                    Quaternion lookRot = Quaternion.LookRotation(dir);
                    lookRot.x = 0;
                    lookRot.z = 0;
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRot,
                        Mathf.Clamp01(3.0f * Time.maximumDeltaTime));
                }
            }
        }
    }
}
