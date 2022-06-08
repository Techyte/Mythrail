using RiptideNetworking;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Player player;

    [SerializeField] private LayerMask ground;
    [SerializeField] private Transform camProxy;
    [SerializeField] private GameObject groundDetector;
    [SerializeField] private float MovementMultiplyer = 2000;
    [SerializeField] private float RunSpeed = 2500;
    [SerializeField] private float HorizontalDrag = 0.1f;
    [SerializeField] private float JumpStrength = 400;
    
    [SerializeField] private Rigidbody rb;

    [SerializeField] private bool canJump = true;
    public bool camMove;

    private bool[] inputs;

    private bool didTeleport;

    private void OnValidate()
    {
        if (player == null)
            player = GetComponent<Player>();
    }

    private void Start()
    {
        inputs = new bool[6];
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

        Move(inputDirection, inputs[4], inputs[5]);
    }

    private void Move(Vector2 inputDirection, bool jump, bool sprint)
    {
        inputDirection.Normalize();
        transform.rotation = FlattenQuaternion(camProxy.rotation);

        float adjustedSpeed = jump ? MovementMultiplyer : RunSpeed;

        bool isGrounded = Physics.Raycast(groundDetector.transform.position, Vector3.down, 0.1f, ground);

        Vector3 move = new Vector3(Time.deltaTime * adjustedSpeed * inputDirection.x, 0, Time.deltaTime * adjustedSpeed * inputDirection.y);

        // Move the controller
        rb.AddRelativeForce(move);

        if (canJump && jump && isGrounded)
        {
            rb.AddForce(Vector3.up * JumpStrength);
        }

        if (transform.position.y <= -15)
        {
            rb.velocity = Vector3.zero;
            transform.position = new Vector3(0, 10, 0);
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
        Message message = Message.Create(MessageSendMode.unreliable, ServerToClientId.playerMovement);
        message.AddUShort(player.Id);
        message.AddUInt(NetworkManager.Singleton.CurrentTick);
        message.AddBool(didTeleport);
        message.AddVector3(transform.position);
        message.AddVector3(camProxy.forward);
        NetworkManager.Singleton.Server.SendToAll(message);

        didTeleport = false;
    }
}
