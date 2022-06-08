using UnityEngine;
using RiptideNetworking;
using System.Collections.Generic;

namespace Mythrail
{
    public class GunManager : MonoBehaviour
    {
        [SerializeField] private Player player;

        [SerializeField] private bool[] weaponInputs;
        [SerializeField] private GameObject gunModelHolder;
        [SerializeField] private GameObject currentGunModel;

        [SerializeField] int currentWeaponIndex;

        [SerializeField] List<GameObject> weaponModels = new List<GameObject>();
        private Dictionary<int, GameObject> weaponIdTable = new Dictionary<int, GameObject>();

        private void Start()
        {
            weaponInputs = new bool[2];
            InitilizeWeaponModels();
        }

        private void InitilizeWeaponModels()
        {
            for(int i = 0; i < weaponModels.Count; i++)
            {
                weaponIdTable.Add(i, weaponModels[i]);
            }
        }

        private void Update()
        {
            if (!player.IsLocal)
                return;
            if (Input.GetMouseButton(0))
                weaponInputs[0] = true;
            if (Input.GetMouseButton(1))
                weaponInputs[1] = true;
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
                player.gunManager.Shot();
        }

        [MessageHandler((ushort)ServerToClientId.swapWeapon)]
        private static void SwapWeapon(Message message)
        {
            ushort playerId = message.GetUShort();
            if (Player.list.TryGetValue(playerId, out Player player))
                player.gunManager.SwapWeapon(playerId, message.GetInt());
        }

        private void Shot()
        {
            Debug.Log("Shot");
        }

        private void SendWeaponInputs()
        {
            Message message = Message.Create(MessageSendMode.unreliable, ClientToServerId.weaponInput);
            message.AddBools(weaponInputs, false);
            message.AddFloat(Input.GetAxis("Mouse ScrollWheel"));
            NetworkManager.Singleton.Client.Send(message);
        }

        private void SwapWeapon(ushort playerId, int newWeaponIndex)
        {
            currentWeaponIndex = newWeaponIndex;
            if (weaponIdTable.TryGetValue(newWeaponIndex, out GameObject model))
            {
                if(Player.list.TryGetValue(playerId, out Player player))
                {
                    player.gunManager.ChangePlayerGunModel((int)playerId, model);
                }
            }
        }

        public void ChangePlayerGunModel(int playerId, GameObject gunModel)
        {
            if (Player.list.TryGetValue((ushort)playerId, out Player player))
            {
                if (currentGunModel)
                    Destroy(currentGunModel);
                currentGunModel = Instantiate(gunModel, gunModelHolder.transform.position, Quaternion.identity);
                currentGunModel.transform.parent = gunModelHolder.transform;
            }
        }
    }
}