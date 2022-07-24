using RiptideNetworking;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MythrailEngine
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private Transform camTransform;

        [SerializeField] private bool[] movementInputs;

        private void Start()
        {
            movementInputs = new bool[6];
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.W))
            {
                movementInputs[0] = true;
                if (Input.GetKey(KeyCode.LeftShift))
                    movementInputs[5] = true;
            }
            if (Input.GetKey(KeyCode.S))
                movementInputs[1] = true;
            if (Input.GetKey(KeyCode.A))
                movementInputs[2] = true;
            if (Input.GetKey(KeyCode.D))
                movementInputs[3] = true;
            if (Input.GetKey(KeyCode.Space))
                movementInputs[4] = true;
        }

        private void FixedUpdate()
        {
            SendMovementInput();

            for (int i = 0; i < movementInputs.Length; i++)
                movementInputs[i] = false;
        }

        private void SendMovementInput()
        {
            if (SceneManager.GetActiveScene().buildIndex != 1)
            {
                Message message = Message.Create(MessageSendMode.unreliable, ClientToServerId.movementInput);
                message.AddBools(movementInputs, false);
                message.AddVector3(camTransform.forward);
                NetworkManager.Singleton.Client.Send(message);   
            }
            else
            {
                Message message = Message.Create(MessageSendMode.unreliable, ClientToLobbyServer.movementInput);
                message.AddBools(movementInputs, false);
                message.AddVector3(camTransform.forward);
                LobbyNetworkManager.Singleton.Client.Send(message);
            }
        }
    }
}