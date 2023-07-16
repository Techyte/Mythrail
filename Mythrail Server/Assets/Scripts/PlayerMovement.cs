using System.Collections;
using Riptide;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Player player;

    [SerializeField] private LayerMask ground;
    public Transform camProxy;
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float runMultiplier = 1.3f;
    [SerializeField] private float crouchMultiplier = .5f;
    [SerializeField] private float jumpHeight = 7f;
    [SerializeField] private float gravity;

    private float _verticalRotation;

    private float _forwardVelocity, _sidewaysVelocity, _verticalVelocity;

    public CharacterController Controller => _controller;
    
    private CharacterController _controller;

    [SerializeField] private GameObject crouchingModel;
    [SerializeField] private GameObject defaultModel;

    [SerializeField] private Transform crouchingCameraPos;
    [SerializeField] private Transform defaultCameraPos;

    [SerializeField] private bool canJump = true;
    public bool canMove = true;

    [SerializeField] private bool[] inputs = new bool[6];

    private bool didTeleport;

    private void Start()
    {
        if (player == null)
            player = GetComponent<Player>();
        _controller = GetComponent<CharacterController>();
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
        if(canMove && !player.respawning)
        {
            inputDirection.Normalize();
            transform.rotation = FlattenQuaternion(camProxy.rotation);

            _forwardVelocity = inputDirection.y * movementSpeed;
            _sidewaysVelocity = inputDirection.x * movementSpeed;

            if (sprint && !isCrouching)
            {
                _forwardVelocity *= runMultiplier;
                _sidewaysVelocity *= runMultiplier;
            }

            if (isCrouching)
            {
                _forwardVelocity *= crouchMultiplier;
                _sidewaysVelocity *= crouchMultiplier;
                camProxy.position = crouchingCameraPos.position;
                defaultModel.SetActive(false);
                crouchingModel.SetActive(true);
            }
            else
            {
                camProxy.position = defaultCameraPos.position;
                defaultModel.SetActive(true);
                crouchingModel.SetActive(false);
            }

            Vector3 direction = new Vector3(_sidewaysVelocity, 0, _forwardVelocity);
            direction = Vector3.ClampMagnitude(direction, movementSpeed);

            if (_controller.isGrounded && jump && canJump)
            {
                _verticalVelocity = jumpHeight;
            }

            _verticalVelocity -= gravity * Time.deltaTime;
            direction.y = _verticalVelocity;

            direction = transform.TransformDirection(direction);

            MovePlayer(direction);

            if (transform.position.y <= -15)
            {
                player.Died();
            }

            SendMovement();
        }
    }

    private void MovePlayer(Vector3 direction)
    {
        if (direction.magnitude > 0)
        {
            _controller.Move(direction * Time.deltaTime);   
        }
    }

    public void StartRespawnDelay()
    {
        StartCoroutine(RespawnDelay());
    }

    private IEnumerator RespawnDelay()
    {
        canMove = false;
        player.respawning = true;
        yield return new WaitForSeconds(player.RespawnDelay);
        SendCanRespawn();
    }

    private void SendCanRespawn()
    {
        Message message = Message.Create(MessageSendMode.Reliable, ServerToClientId.playerCanRespawn);
        NetworkManager.Singleton.Server.Send(message, player.Id);
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
            Message message = Message.Create(MessageSendMode.Unreliable, ServerToClientId.playerMovement);
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
            Message message = Message.Create(MessageSendMode.Unreliable, LobbyServerToClient.playerMovement);
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
