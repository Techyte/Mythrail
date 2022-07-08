using RiptideNetworking;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class GunManager : MonoBehaviour
{
    [SerializeField] private Player player;

    private bool[] inputs;
    private float scrollInput;
    [SerializeField] private int currentWeaponIndex;

    [SerializeField] private Weapon[] guns;

    [SerializeField] private ushort[] loadout;

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

        SendLoadoutInfo();
    }

    private void SendLoadoutInfo()
    {
        Message message = Message.Create(MessageSendMode.reliable, ServerToClientId.loadoutInfo);
        
        message.AddUShort(player.Id);
        message.AddUShort(loadout[0]);
        message.AddUShort(loadout[1]);
        
        NetworkManager.Singleton.Server.Send(message, player.Id);
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
            if(Physics.Raycast(spawn.position, bloomDirection, out hit, guns[loadout[currentWeaponIndex]].distance))
            {
                SendHitInformation(hit.point, hit.normal);
                
                if(hit.transform.gameObject.tag == "Player")
                {
                    hit.transform.gameObject.GetComponent<Player>().TakeDamage(guns[loadout[currentWeaponIndex]].damage);
                }
            }
            Debug.DrawLine(spawn.position, spawn.forward, Color.red, 10);

            message.AddFloat(guns[loadout[currentWeaponIndex]].recoil);
            message.AddFloat(guns[loadout[currentWeaponIndex]].kickBack);
            NetworkManager.Singleton.Server.SendToAll(message);
        }
    }

    private Vector3 GeneratrBloom()
    {
        Transform spawn = transform.Find("CamProxy");
        Vector3 bloom = spawn.position + spawn.forward * 1000f;

        bloom += Random.Range(-guns[loadout[currentWeaponIndex]].bloom, guns[loadout[currentWeaponIndex]].bloom) * spawn.up;
        bloom += Random.Range(-guns[loadout[currentWeaponIndex]].bloom, guns[loadout[currentWeaponIndex]].bloom) * spawn.right;
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
        SendLoadoutInfo();
        
        StartCoroutine(SwapInTimer());
        currentWeaponIndex = currentWeaponIndex == 0 ? 1 : 0;

        Message message = Message.Create(MessageSendMode.reliable, ServerToClientId.swapWeapon);
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