using UnityEngine;
using RiptideNetworking;
using System.Collections.Generic;

namespace MythrailEngine
{
    public class GunManager : MonoBehaviour
    {
        [SerializeField] private Player player;

        [SerializeField] private bool[] weaponInputs = new bool[4];
        [SerializeField] private GameObject gunModelHolder;
        [SerializeField] private GameObject currentGunModel;

        public int[] loadoutIndex = new int[2];

        public int currentWeaponIndex;
        
        public List<GameObject> weaponModels = new List<GameObject>();

        public bool isAiming => weaponInputs[1];

        private void Update()
        {
            currentGunModel.transform.localPosition = Vector3.Lerp(currentGunModel.transform.localPosition, Player.LocalPlayer.gunManager.weaponModels[currentWeaponIndex].transform.localPosition, Time.deltaTime * 4f);
            currentGunModel.transform.localRotation = Quaternion.Lerp(currentGunModel.transform.localRotation, Quaternion.Euler(0, 0, 0), Time.deltaTime * 4f);
            
            if (!player.IsLocal)
                return;
            if (Player.LocalPlayer.playerController.canMove)
            {
                if (Input.GetMouseButton(0))
                {
                    weaponInputs[0] = true;
                }
                
                if (Input.GetMouseButton(1))
                {
                    weaponInputs[1] = true;
                }
                
                if (Input.GetAxis("Mouse ScrollWheel") != 0 || Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Alpha2))
                {
                    weaponInputs[2] = true;
                }
                
                currentGunModel.GetComponent<Gun>().Aim(isAiming);
            }
        }

        private void FixedUpdate()
        {
            if (!player.IsLocal)
                return;
            
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
            {
                player.gunManager.SwapWeapon(message.GetInt());
            }
        }

        [MessageHandler((ushort)ServerToClientId.loadoutInfo)]
        private static void LoadoutInfo(Message message)
        {
            ushort playerId = message.GetUShort();
        
            int id0 = message.GetUShort();
            int id1 = message.GetUShort();
            
            if (Player.list.TryGetValue(playerId, out Player player))
            {
                player.gunManager.AssignLoadout(id0, id1);
            }
        }

        private void Shot(float recoil, float kickBack)
        {
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

        private void SwapWeapon(int newWeaponIndex)
        {
            currentWeaponIndex = newWeaponIndex;
            if (Player.LocalPlayer.gunManager.weaponModels[newWeaponIndex])
            {
                ChangePlayerGunModel(Player.LocalPlayer.gunManager.weaponModels[loadoutIndex[newWeaponIndex]]);
                UIManager.Singleton.GunName.text = Player.LocalPlayer.gunManager.weaponModels[newWeaponIndex].name.ToUpper();
            }
        }

        private void AssignLoadout(int id0, int id1)
        {
            loadoutIndex[0] = id0;
            loadoutIndex[1] = id1;
        }

        private void ChangePlayerGunModel(GameObject gunModel)
        {
            if (currentGunModel)
            {
                Destroy(currentGunModel);
            }
            currentGunModel = Instantiate(gunModel);
            currentGunModel.transform.parent = gunModelHolder.transform;
            currentGunModel.transform.localPosition = gunModel.transform.localPosition;
            currentGunModel.transform.localScale = gunModel.transform.localScale;
            currentGunModel.transform.localRotation = gunModel.transform.localRotation;
        }
    }
}