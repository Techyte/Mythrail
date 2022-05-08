using UnityEngine;
using Photon.Pun;

public class WeaponsSway : MonoBehaviourPunCallbacks
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
        if (Pause.paused) return;
        UpdateSway();
    }

    private void UpdateSway()
    {
        float MouseX = Input.GetAxis("Mouse X");
        float MouseY = Input.GetAxis("Mouse Y");

        if (photonView.IsMine)
        {
            MouseX = 0;
            MouseY = 0;
        }

        Quaternion xAdjustment = Quaternion.AngleAxis(-intensity * MouseX, Vector3.up);
        Quaternion yAdjustment = Quaternion.AngleAxis(intensity * MouseY, Vector3.right);
        Quaternion targetRotation = orignRotation * xAdjustment * yAdjustment;

        if (GunManager.isAiming)
        {
            transform.localRotation = Quaternion.Lerp(transform.localRotation, orignRotation, Time.deltaTime * smooth);
        }
        else
        {
            transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, Time.deltaTime * smooth / 2);
        }
    }
}
