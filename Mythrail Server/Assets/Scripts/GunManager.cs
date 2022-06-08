using RiptideNetworking;
using System.Collections;
using UnityEngine;

public class GunManager : MonoBehaviour
{
    [SerializeField] private Player player;

    [SerializeField] private Transform weaponSpawnPos;

    [SerializeField] private Weapon[] defaultWeapons = new Weapon[2];

    private bool[] inputs;
    private float scrollInput;
    [SerializeField] private int currentWeaponIndex;

    private Weapon[] loadout;

    private bool canSwapIn;
    private bool canShoot;

    private void OnValidate()
    {
        if (player == null)
            player = GetComponent<Player>();
    }

    private void Start()
    {
        inputs = new bool[2];

        canShoot = true;
        canSwapIn = true;

        loadout = defaultWeapons;
    }

    public void SetInputs(bool[] inputs, float scrollInput)
    {
        this.inputs = inputs;
        this.scrollInput = scrollInput;
    }

    private void FixedUpdate()
    {
        if (inputs[0] && canShoot)
            Shoot(GeneratrBloom());
        if (inputs[1])
            Aim();

        if(scrollInput!=0 && canSwapIn)
        {
            SwitchWeapon();
        }
    }

    private void Shoot(Vector3 bloomDirection)
    {
        if (canShoot)
        {
            StartCoroutine(ShootTimer());
            Message message = Message.Create(MessageSendMode.reliable, ServerToClientId.playerShot);
            message.AddUShort(player.Id);

            Transform spawn = transform.Find("CamProxy");
            RaycastHit hit;
            if(Physics.Raycast(spawn.position, bloomDirection, out hit, loadout[currentWeaponIndex].distance))
            {
                SendHitInformation(hit.point, hit.normal);
                
                if(hit.transform.gameObject.tag == "Player")
                {
                    hit.transform.gameObject.GetComponent<Player>().TakeDamage(loadout[currentWeaponIndex].damage);
                }
            }
            Debug.DrawLine(spawn.position, spawn.forward, Color.red, 10);

            NetworkManager.Singleton.Server.SendToAll(message);
        }
    }

    private Vector3 GeneratrBloom()
    {
        Transform spawn = transform.Find("CamProxy");
        Vector3 bloom = spawn.position + spawn.forward * 1000f;

        bloom += Random.Range(-loadout[currentWeaponIndex].bloom, loadout[currentWeaponIndex].bloom) * spawn.up;
        bloom += Random.Range(-loadout[currentWeaponIndex].bloom, loadout[currentWeaponIndex].bloom) * spawn.right;
        bloom -= spawn.position;
        bloom.Normalize();

        return bloom;
    }

    private void SendHitInformation(Vector3 position, Vector3 normal)
    {
        Message message = Message.Create(MessageSendMode.reliable, ServerToClientId.bulletHole);
        message.AddVector3(position);
        message.AddVector3(normal);
        NetworkManager.Singleton.Server.SendToAll(message);
    }

    private void Aim()
    {

    }

    private void SwitchWeapon()
    {
        StartCoroutine(SwapInTimer());
        currentWeaponIndex = (currentWeaponIndex == 0) ? 1 : 0;

        Message message = Message.Create(MessageSendMode.reliable, ServerToClientId.swapWeapon);
        message.AddUShort(player.Id);
        message.AddInt(currentWeaponIndex);
        NetworkManager.Singleton.Server.SendToAll(message);
    }

    private IEnumerator SwapInTimer()
    {
        canSwapIn = false;
        yield return new WaitForSeconds(loadout[currentWeaponIndex].swapInRate);
        canSwapIn = true;
    }

    private IEnumerator ShootTimer()
    {
        canShoot = false;
        yield return new WaitForSeconds(loadout[currentWeaponIndex].fireRate);
        canShoot = true;
    }
}