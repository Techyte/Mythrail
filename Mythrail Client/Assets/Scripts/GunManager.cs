using UnityEngine;
using RiptideNetworking;
using System.Collections.Generic;
using System.Linq;

namespace MythrailEngine
{
    public class GunManager : MonoBehaviour
    {
        [SerializeField] private Player player;

        [SerializeField] private bool[] weaponInputs;
        [SerializeField] private GameObject gunModelHolder;
        [SerializeField] private GameObject currentGunModel;

        public int[] loadoutIndex = new int[2];

        public int currentWeaponIndex;
        
        public List<GameObject> weaponModels = new List<GameObject>();

        public static bool isAiming;

        private void Awake()
        {
            weaponInputs = new bool[3];
        }

        private void Update()
        {
            if (!player.IsLocal)
                return;
            if (Player.LocalPlayer.playerController.canMove)
            {
                if (Input.GetMouseButton(0))
                    weaponInputs[0] = true;
                if (Input.GetMouseButton(1))
                    weaponInputs[1] = true;
                if (Input.GetAxis("Mouse ScrollWheel") != 0)
                    weaponInputs[2] = true;   
            }
            
            currentGunModel.transform.localRotation = Quaternion.Lerp(currentGunModel.transform.localRotation, Quaternion.Euler(0, 0, 0), Time.deltaTime * 4f);
            currentGunModel.transform.localPosition = Vector3.Lerp(currentGunModel.transform.localPosition, weaponModels[currentWeaponIndex].transform.localPosition, Time.deltaTime * 4f);
        }

        private void FixedUpdate()
        {
            if (!player.IsLocal)
                return;
            
            Debug.Log("Fixed Update");
            
            SendWeaponInputs();

            for (int i = 0; i < weaponInputs.Length; i++)
                weaponInputs[i] = false;
        }

        [MessageHandler((ushort)ServerToClientId.playerShot)]
        private static void PlayerShot(Message message)
        {
            if (Player.list.TryGetValue(message.GetUShort(), out Player player))
                player.gunManager.Shot(message.GetFloat(), message.GetFloat());
        }

        [MessageHandler((ushort)ServerToClientId.swapWeapon)]
        private static void SwapWeapon(Message message)
        {
            ushort playerId = message.GetUShort();
            if (Player.list.TryGetValue(playerId, out Player player))
                player.gunManager.SwapWeapon(playerId, message.GetInt());
        }

        [MessageHandler((ushort)ServerToClientId.loadoutInfo)]
        private static void LoadoutInfo(Message message)
        {
            ushort playerId = message.GetUShort();
        
            int id0 = message.GetUShort();
            int id1 = message.GetUShort();
            
            if (Player.list.TryGetValue(playerId, out Player player))
                player.gunManager.AssignLoadout(id0, id1);
        }

        private void Shot(float recoil, float kickBack)
        {
            Debug.Log("Shot");
            currentGunModel.transform.localRotation = Quaternion.Euler(-recoil, 0, 0);
            currentGunModel.transform.localPosition -= currentGunModel.transform.worldToLocalMatrix.MultiplyVector(currentGunModel.transform.forward).normalized * kickBack;
            //Recoil goes here
        }

        private void SendWeaponInputs()
        {
            Message message = Message.Create(MessageSendMode.unreliable, ClientToServerId.weaponInput);
            message.AddBools(weaponInputs, false);
            NetworkManager.Singleton.Client.Send(message);
        }

        private void SwapWeapon(ushort playerId, int newWeaponIndex)
        {
            currentWeaponIndex = newWeaponIndex;
            if (weaponModels[newWeaponIndex])
                if (Player.list.TryGetValue(playerId, out Player player))
                {
                    player.gunManager.ChangePlayerGunModel(playerId, weaponModels[loadoutIndex[newWeaponIndex]]);   
                }
        }

        private void AssignLoadout(int id0, int id1)
        {
            loadoutIndex[0] = id0;
            loadoutIndex[1] = id1;
            
            Debug.Log("Assigning loadout");
        }

        private void ChangePlayerGunModel(int playerId, GameObject gunModel)
        {
            if (Player.list.TryGetValue((ushort)playerId, out Player player))
            {
                if (currentGunModel)
                    Destroy(currentGunModel);
                currentGunModel = Instantiate(gunModel);
                currentGunModel.transform.parent = gunModelHolder.transform;
                currentGunModel.transform.localPosition = gunModel.transform.localPosition;
                currentGunModel.transform.localScale = gunModel.transform.localScale;
                currentGunModel.transform.localRotation = gunModel.transform.localRotation;
            }
        }
    }
}