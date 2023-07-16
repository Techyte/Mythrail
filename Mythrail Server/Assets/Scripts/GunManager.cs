using Riptide;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class GunManager : MonoBehaviour
{
    [SerializeField] private Player player;

    [SerializeField] private bool[] inputs = new bool[4];
    [SerializeField] private int currentWeaponIndex;

    [SerializeField] private Weapon[] guns;

    [SerializeField] private ushort[] loadout;

    [SerializeField] private bool canSwapIn = true;
    [SerializeField] private bool canShoot = true;
    public bool isAiming;

    private void OnValidate()
    {
        if (player == null)
            player = GetComponent<Player>();
    }

    private void Start()
    {
        if(SceneManager.GetActiveScene().buildIndex != 0) return;

        canShoot = true;
        canSwapIn = true;

        SendLoadoutInfo();
    }

    private void SendLoadoutInfo()
    {
        Message message = Message.Create(MessageSendMode.Reliable, ServerToClientId.loadoutInfo);
        
        message.AddUShort(player.Id);
        message.AddUShort(loadout[0]);
        message.AddUShort(loadout[1]);
        
        NetworkManager.Singleton.Server.Send(message, player.Id);
    }

    private bool hasAssignedInputsOnce;
    public void SetInputs(bool[] inputs)
    {
        this.inputs = inputs;
        if (!hasAssignedInputsOnce)
        {
            hasAssignedInputsOnce = true;
        }
    }

    private bool shootCheck;
    private void FixedUpdate()
    {
        if(SceneManager.GetActiveScene().buildIndex == 0) return;
        
        if(hasAssignedInputsOnce)
        {
            if (inputs[0] && canShoot && shootCheck)
            {
                Shoot(GeneratrBloom());
            }
            else if (!shootCheck)
            {
                shootCheck = true;
            }

            isAiming = inputs[1];

            if (inputs[2] && canSwapIn)
            {
                SwitchWeapon();
            }
        }
    }

    private void Shoot(Vector3 bloomDirection)
    {
        if (canShoot)
        {
            StartCoroutine(ShootTimer());
            Message message = Message.Create(MessageSendMode.Reliable, ServerToClientId.playerShot);
            message.AddUShort(player.Id);

            Transform spawn = transform.Find("CamProxy");
            RaycastHit hit;
            if(Physics.Raycast(spawn.position, bloomDirection, out hit, guns[loadout[currentWeaponIndex]].distance))
            {
                SendHitInformation(hit.point, hit.normal);
                
                if(hit.transform.gameObject.CompareTag("Player"))
                {
                    Player hitPlayer = hit.transform.gameObject.GetComponent<Player>();
                    if(hitPlayer.Id != player.Id)
                    {
                        if(!hitPlayer.respawning)
                        {
                            hitPlayer.TakeDamage(guns[loadout[currentWeaponIndex]].damage, player.Id);
                        }
                    }
                }
            }
            Debug.DrawRay(spawn.position, bloomDirection, Color.red, 10);

            message.AddFloat(guns[loadout[currentWeaponIndex]].recoil);
            message.AddFloat(guns[loadout[currentWeaponIndex]].kickBack);
            NetworkManager.Singleton.Server.SendToAll(message);
        }
    }

    private Vector3 GeneratrBloom()
    {
        Transform spawn = transform.Find("CamProxy");
        Vector3 bloom = spawn.position + spawn.forward * 1000f;

        if(isAiming)
        {
            bloom += Random.Range(-guns[loadout[currentWeaponIndex]].bloom / 2, guns[loadout[currentWeaponIndex]].bloom / 2) * spawn.up;
            bloom += Random.Range(-guns[loadout[currentWeaponIndex]].bloom / 2, guns[loadout[currentWeaponIndex]].bloom / 2) * spawn.right;
        }
        else
        {
            bloom += Random.Range(-guns[loadout[currentWeaponIndex]].bloom, guns[loadout[currentWeaponIndex]].bloom) *
                     spawn.up;
            bloom += Random.Range(-guns[loadout[currentWeaponIndex]].bloom, guns[loadout[currentWeaponIndex]].bloom) *
                     spawn.right;
        }
        bloom -= spawn.position;
        bloom.Normalize();

        return bloom;
    }

    private void SendHitInformation(Vector3 position, Vector3 normal)
    {
        Message message = Message.Create(MessageSendMode.Reliable, ServerToClientId.bulletHole);
        message.AddVector3(position);
        message.AddVector3(normal);
        NetworkManager.Singleton.Server.SendToAll(message);
    }

    private void SwitchWeapon()
    {   
        SendLoadoutInfo();
        
        StartCoroutine(SwapInTimer());
        currentWeaponIndex = currentWeaponIndex == 0 ? 1 : 0;

        Message message = Message.Create(MessageSendMode.Reliable, ServerToClientId.swapWeapon);
        message.AddUShort(player.Id);
        message.AddInt(currentWeaponIndex);
        NetworkManager.Singleton.Server.SendToAll(message);
        
        SendLoadoutInfo();
    }

    private IEnumerator SwapInTimer()
    {
        canSwapIn = false;
        yield return new WaitForSeconds(guns[loadout[currentWeaponIndex]].swapInRate);
        canSwapIn = true;
    }

    private IEnumerator ShootTimer()
    {
        canShoot = false;
        yield return new WaitForSeconds(guns[loadout[currentWeaponIndex]].fireRate);
        canShoot = true;
    }
}