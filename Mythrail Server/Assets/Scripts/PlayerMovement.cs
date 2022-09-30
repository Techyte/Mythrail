using RiptideNetworking;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Player player;

    [SerializeField] private LayerMask ground;
    public Transform camProxy;
    [SerializeField] private GameObject groundDetector;
    [SerializeField] private float MovementMultiplyer = 2000;
    [SerializeField] private float RunSpeed = 2500;
    [SerializeField] private float HorizontalDrag = 0.1f;
    [SerializeField] private float JumpStrength = 400;
    
    [SerializeField] private Rigidbody rb;

    [SerializeField] private GameObject crouchingModel;
    [SerializeField] private GameObject defaultModel;

    [SerializeField] private Transform crouchingCameraPos;
    [SerializeField] private Transform defaultCameraPos;

    [SerializeField] private bool canJump = true;
    public bool camMove;

    [SerializeField] private bool[] inputs = new bool[6];

    private bool didTeleport;

    private void OnValidate()
    {
        if (player == null)
            player = GetComponent<Player>();
    }

    private void FixedUpdate()
    {
        Vector2 inputDirection = Vector2.zero;
        if (inputs[0])
            inputDirection.y += 1;

        if (inputs[1])
            inputDirection.y -= 1;

        if (inputs[2])
            inputDirection.x -= 1;

        if (inputs[3])
            inputDirection.x += 1;

        Move(inputDirection, inputs[4], inputs[5], inputs[6]);
    }

    private void Move(Vector2 inputDirection, bool jump, bool sprint, bool isCrouching)
    {
        inputDirection.Normalize();
        transform.rotation = FlattenQuaternion(camProxy.rotation);

        float adjustedSpeed = sprint && !player.GunManager.isAiming ? MovementMultiplyer : RunSpeed;

        if (player.GunManager.isAiming || isCrouching)
        {
            adjustedSpeed /= 2;
        }

        if (isCrouching)
        {
            defaultModel.SetActive(false);
            crouchingModel.SetActive(true);
            camProxy.position = crouchingCameraPos.position;
        }
        else
        {
            defaultModel.SetActive(true);
            crouchingModel.SetActive(false);
            camProxy.position = defaultCameraPos.position;
        }

        bool isGrounded = Physics.Raycast(groundDetector.transform.position, Vector3.down, 0.1f, ground);

        Vector3 move = new Vector3(Time.deltaTime * adjustedSpeed * inputDirection.x, 0, Time.deltaTime * adjustedSpeed * inputDirection.y);

        rb.AddRelativeForce(move);

        if (canJump && jump && isGrounded && !isCrouching)
        {
            rb.AddForce(Vector3.up * JumpStrength);
        }

        if (transform.position.y <= -15)
        {
            player.Died();
        }
        
        rb.velocity-= new Vector3(rb.velocity.x * HorizontalDrag, 0, rb.velocity.z * HorizontalDrag);

        SendMovement();
    }
    
    private Quaternion FlattenQuaternion(Quaternion quaternion)
    {
        quaternion.x = 0;
        quaternion.z = 0;
        return quaternion;
    }

    public void SetInputs(bool[] inputs, Vector3 forward)
    {
        this.inputs = inputs;
        camProxy.forward = forward;
    }

    private void SendMovement()
    {
        if (SceneManager.GetActiveScene().buildIndex != 0)
        {
            Message message = Message.Create(MessageSendMode.unreliable, ServerToClientId.playerMovement);
            message.AddUShort(player.Id);
            message.AddUInt(NetworkManager.Singleton.CurrentTick);
            message.AddBool(didTeleport);
            message.AddVector3(transform.position);
            message.AddVector3(camProxy.forward);
            message.AddBool(inputs[6]);
            NetworkManager.Singleton.Server.SendToAll(message);

            didTeleport = false;   
        }
        else
        {
            Message message = Message.Create(MessageSendMode.unreliable, LobbyServerToClient.playerMovement);
            message.AddUShort(player.Id);
            message.AddUInt(NetworkManager.Singleton.CurrentTick);
            message.AddBool(didTeleport);
            message.AddVector3(transform.position);
            message.AddVector3(camProxy.forward);
            NetworkManager.Singleton.Server.SendToAll(message);

            didTeleport = false;
        }
    }
}
