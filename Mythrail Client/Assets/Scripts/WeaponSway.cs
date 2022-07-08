using UnityEngine;

namespace MythrailEngine
{
    public class WeaponSway : MonoBehaviour
    {
        public float intensity = 10f;
        public float smooth = 10f;

        private Quaternion orignRotation;

        private void Start()
        {
            orignRotation = transform.localRotation;
        }

        private void Update()
        {
            UpdateSway();
        }

        private void UpdateSway()
        {
            float MouseX = Input.GetAxis("Mouse X");
            float MouseY = Input.GetAxis("Mouse Y");

            Quaternion xAdjustment = Quaternion.AngleAxis(-intensity * MouseX, Vector3.up);
            Quaternion yAdjustment = Quaternion.AngleAxis(intensity * MouseY, Vector3.right);
            Quaternion targetRotation = orignRotation * xAdjustment * yAdjustment;

            transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, Time.deltaTime * smooth);
        }
    }   
}