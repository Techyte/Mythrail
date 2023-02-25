using System;
using UnityEngine;

namespace Mythrail.Weapons
{
    public class Gun : MonoBehaviour
    {
        [SerializeField] private Vector3 aimPos;
        [SerializeField] private Vector3 defaultPos;

        public void Aim(bool isAiming)
        {
            if (isAiming)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, aimPos, 0.05f);
            }
            else
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, defaultPos, 0.05f);
            }
        }
    }
   
}