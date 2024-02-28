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

[RequireComponent(typeof(Rigidbody))]
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

    public Rigidbody Rb => _rb;
    
    private Rigidbody _rb;

    [SerializeField] private Transform groundDetector;
    [SerializeField] private float groundDistanceAllowed;
    [SerializeField] private LayerMask groundLayer;

    [SerializeField] private GameObject crouchingModel;
    [SerializeField] private GameObject defaultModel;

    [SerializeField] private Transform crouchingCameraPos;
    [SerializeField] private Transform defaultCameraPos;

    [SerializeField] private bool grounded = true;
    [SerializeField] private bool canJump = true;
    public bool canMove = true;

    public bool didTeleport; //TODO: did teleport apparently is not being used

    public const int BUFFER_SIZE = 1024;

    private PlayerMovementState[] _stateBuffer;

    private Queue<PlayerInput> inputQueue = new Queue<PlayerInput>();
    private PlayerInput lastInputAddedToQueue = new PlayerInput();

    private PlayerMovementState lastRecordedState = new PlayerMovementState();

    private void Awake()
    {
        _stateBuffer = new PlayerMovementState[BUFFER_SIZE];
        if (player == null)
            player = GetComponent<Player>();
        _rb = GetComponent<Rigidbody>();
    }

    private PlayerMovementState GetMostRecentState()
    {
        uint bufferIndex = NetworkManager.Singleton.CurrentTick % BUFFER_SIZE;

        PlayerMovementState state = _stateBuffer[bufferIndex];

        return !state.Equals(default(PlayerMovementState)) ? state : lastRecordedState;
    }

    public void AssertStateFromBufferIndex(uint index)
    {
        PlayerMovementState state = _stateBuffer[index];

        if (state.Equals(default(PlayerMovementState)))
        {
            state = GetMostRecentState();
        }
        
        transform.position = state.position;
        camProxy.forward = state.forward;
    }

    public void SetStateAtTick(uint tick, PlayerMovementState state)
    {
        uint bufferIndex = tick % BUFFER_SIZE;

        SetStateAt(bufferIndex, state);
    }

    public void SetStateAt(uint bufferIndex, PlayerMovementState state)
    {
        lastRecordedState = state;
        _stateBuffer[bufferIndex] = state;
    }

    public void HandleTick()
    {
        // the buffer index that the input was sent on
        uint bufferIndex = BUFFER_SIZE+1;

        while (inputQueue.Count > 0)
        {
            PlayerInput currentInput = inputQueue.Dequeue();

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

            // so we arnt rolling back when this is the first tick
            if(currentInput.tick != 0)
            {
                // so we are simulating from the right point
                RollbackToTick(currentInput.tick - 1);
            }
            
            Move(inputDirection, currentInput);

            PlayerMovementState state = new PlayerMovementState();
            state.position = transform.position;
            state.tick = currentInput.tick;
            state.didTeleport = false;

            SetStateAt(bufferIndex, state);
        }

        // if we actually handled any inputs this tick
        if (bufferIndex != BUFFER_SIZE + 1)
        {
            SendNewState(_stateBuffer[bufferIndex]);
        }
        // handled all inputs, nothing new to handle, so we send the current state to handle gravity and such (i think, still need to test it)
        else 
        {
            uint currentTickBufferIndex = NetworkManager.Singleton.CurrentTick % BUFFER_SIZE;
            SendNewState(_stateBuffer[currentTickBufferIndex]);
        }
    }

    private void RollbackToTick(uint tick)
    {
        uint bufferIndex = tick % BUFFER_SIZE;

        PlayerMovementState state = _stateBuffer[bufferIndex];

        transform.position = state.position;
    }

    public void SetCurrentStateBuffer()
    {
        uint bufferIndex = NetworkManager.Singleton.CurrentTick % BUFFER_SIZE;
        
        PlayerMovementState predictedState = new PlayerMovementState();
        predictedState.position = transform.position;
        predictedState.tick = NetworkManager.Singleton.CurrentTick;
        predictedState.didTeleport = false;
        predictedState.forward = camProxy.forward;
        
        _stateBuffer[bufferIndex] = predictedState;
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

            // handle checking if we are on the ground
            RaycastHit hit = new RaycastHit();

            if (Physics.Raycast(groundDetector.position, Vector3.down, out hit, groundDistanceAllowed, groundLayer))
            {
                grounded = true;
            }

            Vector3 direction = new Vector3(_sidewaysVelocity, 0, _forwardVelocity);
            direction = Vector3.ClampMagnitude(direction, movementSpeed);
            
            if (grounded && input.inputs[4] && canJump)
            {
                 _verticalVelocity = jumpHeight;
            }

            _verticalVelocity -= gravity * Time.fixedDeltaTime;
            //direction.y = _verticalVelocity;

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
            transform.position += movementSpeed * Time.fixedDeltaTime * direction;
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
        bool foundOneValidInput = false;
        
        for (int i = 0; i < input.inputs.Length; i++)
        {
            if (input.inputs[i])
            {
                foundOneValidInput = true;
            }else if (input.forward != lastInputAddedToQueue.forward)
            {
                foundOneValidInput = true;
            }
        }
        
        if(foundOneValidInput)
        {
            inputQueue.Enqueue(input);
            lastInputAddedToQueue = input;
        }
    }

    private void SendNewState(PlayerMovementState state)
    {
        Debug.Log("sending a new state");
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
