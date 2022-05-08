using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class GunManager : MonoBehaviourPunCallbacks
{
    #region Variables
    public Transform WeaponParent;
    public List<Gun> GunLoadout = new List<Gun>();
    public GameObject bulletHolePrefab;
    public LayerMask canBeShot;
    public static bool isAiming;
    public GameObject startingWeapon;

    private float currentColldown;
    private int currentIndex = 0;
    public GameObject currentWeapon;
    Transform anchor;
    Transform aimingPos;
    Transform defaultPos;

    Player player;

    public GameObject hitSourcePlayer;

    #endregion

    private void Start()
    {
        currentWeapon = startingWeapon;
        player = gameObject.GetComponent<Player>();
    }


    #region MonoBehaviour Callbacks

    private void Update()
    {
        if (!photonView.IsMine) return;
        if (Pause.paused) return;

        #region Equip Inputs
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            photonView.RPC("Equip", RpcTarget.All, 0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            photonView.RPC("Equip", RpcTarget.All, 1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            photonView.RPC("Equip", RpcTarget.All, 2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            photonView.RPC("Equip", RpcTarget.All, 3);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            photonView.RPC("Equip", RpcTarget.All, 4);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            photonView.RPC("Equip", RpcTarget.All, 5);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            photonView.RPC("Equip", RpcTarget.All, 6);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            photonView.RPC("Equip", RpcTarget.All, 7);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            photonView.RPC("Equip", RpcTarget.All, 8);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            photonView.RPC("Equip", RpcTarget.All, 9);
        }
        #endregion

        if(gameObject.transform.position.y <= 1.5)
        {
            player.gameObject.GetPhotonView().RPC("TakeDamagePlayer", RpcTarget.All, 100);
        }

        isAiming = Input.GetMouseButton(1);

        if (currentWeapon)
        {
            Aim(isAiming);
        }

        if (currentWeapon != null)
        {
            if (photonView.IsMine)
            {
                if (Input.GetMouseButton(0) && currentColldown <= 0)
                {
                    photonView.RPC("Shoot", RpcTarget.All, GeneratrBloom());
                }

                if (currentColldown > 0) currentColldown -= Time.deltaTime;
            }
        }

        if (currentWeapon != null)
        {
            currentWeapon.transform.localPosition = Vector3.Lerp(currentWeapon.transform.localPosition, Vector3.zero, Time.deltaTime * 4f);
        }
    }

    #endregion


    #region Private Methods

    private Vector3 GeneratrBloom()
    {
        Transform spawn = transform.Find("Cameras/Normal Camera");
        Vector3 bloom = spawn.position + spawn.forward * 1000f;

        bloom += Random.Range(-GunLoadout[currentIndex].bloom, GunLoadout[currentIndex].bloom) * spawn.up;
        bloom += Random.Range(-GunLoadout[currentIndex].bloom, GunLoadout[currentIndex].bloom) * spawn.right;
        bloom -= spawn.position;
        bloom.Normalize();

        return bloom;
    }

    [PunRPC]
    public void Shoot(Vector3 bloomPos)
    {
        currentColldown = GunLoadout[currentIndex].fireRate;

        //Recoil and kickback
        currentWeapon.transform.Rotate(-GunLoadout[currentIndex].recoil, 0, 0);
        currentWeapon.transform.position -= currentWeapon.transform.forward * GunLoadout[currentIndex].kickBack;

        Transform spawn = transform.Find("Cameras/Normal Camera");
        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(spawn.position, bloomPos, out hit, GunLoadout[currentIndex].distance, canBeShot))
        {
            GameObject newHole = Instantiate(bulletHolePrefab, hit.point + hit.normal * 0.001f, Quaternion.identity) as GameObject;
            newHole.transform.LookAt(hit.point + hit.normal);
            newHole.transform.parent = hit.transform;
            Destroy(newHole, 5f);

            if (photonView.IsMine)
            {
                if(hit.transform.gameObject.layer == 9)
                {
                    player.gameObject.GetPhotonView().RPC("TakeDamageFromPlayer", RpcTarget.All, GunLoadout[currentIndex].damage, hit.transform.gameObject.GetComponent<Player>());
                }
                if (hit.transform.GetComponent<Grenade>() != null)
                {
                    hit.transform.gameObject.GetPhotonView().RPC("ExplosionEffect", RpcTarget.All);
                }
            }
        }
    }

    [PunRPC]
    private void Equip(int id)
    {
        //Making sure that the slot we are tring to equip actually has a weapon in it
        //if (!GunLoadout[id]) return;
        if (GunLoadout[id] == GunLoadout[currentIndex]) return;

        //So we dont spawn in more than 1 weapon per player at a time
        if (currentWeapon != null) Destroy(currentWeapon);

        currentIndex = id;

        //Spawning and swaying weapon
        GameObject newWeapon = Instantiate(GunLoadout[id].prefab, WeaponParent.position, WeaponParent.rotation, WeaponParent);
        newWeapon.transform.localPosition = Vector3.zero;
        newWeapon.transform.localEulerAngles = Vector3.zero;

        currentWeapon = newWeapon;
    }

    private void Aim(bool _isAiming)
    {
        if (photonView.IsMine)
        {
            if (!anchor && !aimingPos && !defaultPos)
            {
                anchor = currentWeapon.transform.Find("Anchor");
                aimingPos = currentWeapon.transform.Find("States/ADS");
                defaultPos = currentWeapon.transform.Find("States/HIP");
            }

            if (_isAiming)
            {
                anchor.position = Vector3.Lerp(anchor.position, aimingPos.position, Time.deltaTime * GunLoadout[currentIndex].aimSpeed);
            }
            else
            {
                anchor.position = Vector3.Lerp(anchor.position, defaultPos.position, Time.deltaTime * GunLoadout[currentIndex].aimSpeed);
            }
        }
    }
    #endregion
}