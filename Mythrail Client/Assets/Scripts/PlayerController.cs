using RiptideNetworking;
using UnityEngine;

namespace Mythrail
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private Transform camTransform;

        [SerializeField] private bool[] movementInputs;
        public bool[] MovementInputs => movementInputs;

        private void Start()
        {
            movementInputs = new bool[6];
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.W))
                movementInputs[0] = true;
            if (Input.GetKey(KeyCode.S))
                movementInputs[1] = true;
            if (Input.GetKey(KeyCode.A))
                movementInputs[2] = true;
            if (Input.GetKey(KeyCode.D))
                movementInputs[3] = true;
            if (Input.GetKey(KeyCode.Space))
                movementInputs[4] = true;
            if (Input.GetKey(KeyCode.LeftShift))
                movementInputs[5] = true;
        }

        private void FixedUpdate()
        {
            SendMovementInput();

            for (int i = 0; i < movementInputs.Length; i++)
                movementInputs[i] = false;
        }

        private void SendMovementInput()
        {
            Message message = Message.Create(MessageSendMode.unreliable, ClientToServerId.movementInput);
            message.AddBools(movementInputs, false);
            message.AddVector3(camTransform.forward);
            NetworkManager.Singleton.Client.Send(message);
        }
    }

}