using System.Collections;
using System.Collections.Generic;
using Riptide;
using UnityEngine;
using UnityEngine.SceneManagement;

public struct PlayerMovementState
{
    public Vector3 position;
    public Vector3 forward;
    public bool didTeleport;
    public uint tick;
}

public struct PlayerInput
{
    public bool[] inputs;
    public Vector3 forward;
    public uint tick;
}

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Player player;

    public Transform camProxy;
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float runMultiplier = 1.3f;
    [SerializeField] private float crouchMultiplier = .5f;
    [SerializeField] private float jumpHeight = 7f;
    [SerializeField] private float gravity;

    private float _verticalRotation;

    private float _forwardVelocity, _sidewaysVelocity, _verticalVelocity;

    [SerializeField] private Transform groundDetector;
    [SerializeField] private float groundDistanceAllowed;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private CapsuleCollider collider;

    [SerializeField] private GameObject crouchingModel;
    [SerializeField] private GameObject defaultModel;

    [SerializeField] private Transform crouchingCameraPos;
    [SerializeField] private Transform defaultCameraPos;

    [SerializeField] private bool grounded = true;
    [SerializeField] private bool canJump = true;
    public bool canMove = true;

    private void Awake()
    {
        if (player == null)
            player = GetComponent<Player>();
    }

    private void Move(Vector3 inputDirection, PlayerInput input)
    {
        if(canMove && !player.respawning)
        {
            inputDirection.Normalize();
            
            // camProxy.forward = input.forward;
            // transform.rotation = FlattenQuaternion(camProxy.rotation);
            //
            // _forwardVelocity = inputDirection.y * movementSpeed;
            // _sidewaysVelocity = inputDirection.x * movementSpeed;
            //
            // if (input.inputs[5] && !input.inputs[6])
            // {
            //     _forwardVelocity *= runMultiplier;
            //     _sidewaysVelocity *= runMultiplier;
            // }
            //
            // if (SceneManager.GetActiveScene().name != "Lobby")
            // {
            //     if (input.inputs[6])
            //     {
            //         _forwardVelocity *= crouchMultiplier;
            //         _sidewaysVelocity *= crouchMultiplier;
            //         camProxy.position = crouchingCameraPos.position;
            //         defaultModel.SetActive(false);
            //         crouchingModel.SetActive(true);
            //     }
            //     else
            //     {
            //         camProxy.position = defaultCameraPos.position;
            //         defaultModel.SetActive(true);
            //         crouchingModel.SetActive(false);
            //     }
            // }
            //
            // // handle checking if we are on the ground
            // RaycastHit hit = new RaycastHit();
            //
            // if (Physics.Raycast(groundDetector.position, Vector3.down, out hit, groundDistanceAllowed, groundLayer))
            // {
            //     grounded = true;
            // }
            //
            // Vector3 direction = new Vector3(_sidewaysVelocity, 0, _forwardVelocity);
            // direction = Vector3.ClampMagnitude(direction, movementSpeed);
            //
            // if (grounded && input.inputs[4] && canJump)
            // {
            //      _verticalVelocity = jumpHeight;
            // }
            //
            // _verticalVelocity -= gravity * Time.fixedDeltaTime;
            // //direction.y = _verticalVelocity;
            //
            // direction = transform.TransformDirection(direction);

            MovePlayer(transform.TransformDirection(new Vector3(inputDirection.x, 0, inputDirection.y)));

            if (transform.position.y <= -15)
            {
                player.Died();
            }
        }
    }

    private void MovePlayer(Vector3 direction)
    {
        if (direction.magnitude > 0)
        {
            transform.position += direction;
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
}
