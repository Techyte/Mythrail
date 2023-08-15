using System;
using System.Collections;
using System.Collections.Generic;
using Multiplayer.Rollback;
using Riptide;
using UnityEngine;
using UnityEngine.SceneManagement;

public struct PlayerMovementState
{
    public Vector3 position;
    public PlayerInput inputUsed;
    public bool didTeleport;
    public uint tick;
}

public struct PlayerInput
{
    public bool[] inputs;
    public Vector3 forward;
    public uint tick;
}

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

    public bool didTeleport;

    public const int BUFFER_SIZE = 1024;

    private PlayerMovementState[] _stateBuffer;

    private Queue<PlayerInput> inputQueue = new Queue<PlayerInput>();

    private void Awake()
    {
        _stateBuffer = new PlayerMovementState[BUFFER_SIZE];
        if (player == null)
            player = GetComponent<Player>();
        _controller = GetComponent<CharacterController>();
    }

    public PlayerMovementState GetMostRecentState()
    {
        return _stateBuffer[NetworkManager.Singleton.CurrentTick];
    }

    public void AssertStateFromBufferIndex(uint index)
    {
        PlayerMovementState state = _stateBuffer[index];

        _controller.enabled = false;
        
        transform.position = state.position;
        camProxy.forward = state.inputUsed.forward;
        
        _controller.enabled = true;
    }

    public void ResetToPresentPosition()
    {
        uint bufferIndex = NetworkManager.Singleton.CurrentTick % BUFFER_SIZE;
        
        PlayerMovementState state = _stateBuffer[bufferIndex];

        _controller.enabled = false;
        
        transform.position = state.position;
        camProxy.forward = state.inputUsed.forward;
        
        _controller.enabled = true;
    }

    public void HandleTick()
    {
        uint bufferIndex = BUFFER_SIZE+1;

        while (inputQueue.Count > 0)
        {
            PlayerInput currentInput = inputQueue.Dequeue();
            
            Debug.Log(currentInput.inputs[0]);

            //RollbackManager.Instance.RollbackOtherPlayerStatesTo(currentInput.tick, player.Id);

            bufferIndex = currentInput.tick % BUFFER_SIZE;

            Vector2 inputDirection = Vector2.zero;
            if (currentInput.inputs[0])
                inputDirection.y += 1;

            if (currentInput.inputs[1])
                inputDirection.y -= 1;

            if (currentInput.inputs[2])
                inputDirection.x -= 1;

            if (currentInput.inputs[3])
                inputDirection.x += 1;

            Move(inputDirection, currentInput);

            PlayerMovementState state = new PlayerMovementState();
            state.position = transform.position;
            state.tick = currentInput.tick;
            state.didTeleport = false;
            state.inputUsed = currentInput;

            _stateBuffer[bufferIndex] = state;
        }

        if (bufferIndex != BUFFER_SIZE + 1)
        {
            SendNewState(_stateBuffer[bufferIndex]);
        }
        
        //RollbackManager.Instance.ResetAllPlayersToPresentPosition(player.Id);
    }

    private void Move(Vector3 inputDirection, PlayerInput input)
    {
        if(canMove && !player.respawning)
        {
            inputDirection.Normalize();

            camProxy.forward = input.forward;
            transform.rotation = FlattenQuaternion(camProxy.rotation);

            _forwardVelocity = inputDirection.y * movementSpeed;
            _sidewaysVelocity = inputDirection.x * movementSpeed;

            if (input.inputs[5] && !input.inputs[6])
            {
                _forwardVelocity *= runMultiplier;
                _sidewaysVelocity *= runMultiplier;
            }

            if (SceneManager.GetActiveScene().name != "Lobby")
            {
                if (input.inputs[6])
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
            }

            Vector3 direction = new Vector3(_sidewaysVelocity, 0, _forwardVelocity);
            direction = Vector3.ClampMagnitude(direction, movementSpeed);

            if (_controller.isGrounded && input.inputs[4] && canJump)
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

    public void SetInputs(PlayerInput input)
    {
        inputQueue.Enqueue(input);
    }

    private void SendNewState(PlayerMovementState state)
    {
        if (SceneManager.GetActiveScene().name != "Lobby")
        {
            Message message = Message.Create(MessageSendMode.Unreliable, ServerToClientId.playerMovement);
            message.AddUShort(player.Id);
            message.AddPlayerState(state);
            NetworkManager.Singleton.Server.SendToAll(message);

            didTeleport = false;   
        }
        else
        {
            Message message = Message.Create(MessageSendMode.Unreliable, LobbyServerToClientId.playerMovement);
            message.AddUShort(player.Id);
            message.AddPlayerState(state);
            NetworkManager.Singleton.Server.SendToAll(message);

            didTeleport = false;
        }
    }
}
