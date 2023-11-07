using Mythrail.General;
using Mythrail.Multiplayer;
using Riptide;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mythrail.Players
{
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
    
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private Transform camTransform;

        [SerializeField] private bool[] movementInputs = new bool[6];

        private float _forwardVelocity, _sidewaysVelocity, _verticalVelocity;

        [SerializeField] private float movementSpeed = 5f;
        [SerializeField] private float runMultiplier = 1.3f;
        [SerializeField] private float crouchMultiplier = .5f;
        [SerializeField] private float jumpHeight = 7f;
        [SerializeField] private float gravity;
        [SerializeField] private Transform crouchingCameraPos;
        [SerializeField] private Transform defaultCameraPos;
        [SerializeField] private GameObject crouchingModel;
        [SerializeField] private GameObject defaultModel;

        [SerializeField] private Transform groundDetector;
        [SerializeField] private float groundDistanceAllowed;
        [SerializeField] private LayerMask groundLayer;

        private bool canJump = true;
        private bool grounded = true;
        public bool canMove = true;

        private Player _player;
        private Rigidbody _rb;
        
        // client prediction

        private const int BUFFER_SIZE = 1024;
        private PlayerInput[] _inputBuffer;
        private PlayerMovementState[] _stateBuffer;

        private PlayerMovementState _lastServerState;
        private PlayerMovementState _lastProcessedState;

        private uint tick => NetworkManager.Singleton.ServerTick;

        // end of client prediction

        private void Awake()
        {
            _player = GetComponent<Player>();
            _rb = GetComponent<Rigidbody>();

            _inputBuffer = new PlayerInput[BUFFER_SIZE];
            _stateBuffer = new PlayerMovementState[BUFFER_SIZE];
        }

        public void HandleTick()
        {
            // if stuff wont die on us and the last server state we received is not equal to last thing we corrected
            if (!_lastServerState.Equals(default(PlayerMovementState)) &&
                (_lastProcessedState.Equals(default(PlayerMovementState)) || !_lastServerState.Equals(_lastProcessedState)))
            {
                Reconcile();
            }
            
            uint bufferIndex = tick % BUFFER_SIZE;

            PlayerInput input = new PlayerInput();

            input.inputs = movementInputs.Copy();
            input.forward = camTransform.forward;
            input.tick = tick;

            _inputBuffer[bufferIndex] = input;

            PlayerMovementState state = new PlayerMovementState();
            Movement(input);
            state.position = transform.position;
            state.inputUsed = input;
            state.tick = tick;

            _stateBuffer[bufferIndex] = state;
            
            SendMovementInput(input);
        }

        private void Reconcile()
        {
            Debug.Log("reconcile function");
            _lastProcessedState = _lastServerState;

            uint serverStateBufferIndex = _lastServerState.tick % BUFFER_SIZE;
            float positionError =
                Vector3.Distance(_lastServerState.position, _stateBuffer[serverStateBufferIndex].position);
            Debug.Log($"server says at {_lastServerState.tick} we were at {_lastServerState.position}");
            Debug.Log($"we say at {_stateBuffer[serverStateBufferIndex].tick} we were at {_stateBuffer[serverStateBufferIndex].position}");
            
            if (positionError > 0.001f)
            {
                Debug.Log("need to reconcile");

                _stateBuffer[serverStateBufferIndex] = _lastServerState;

                uint tickToProcess = _lastServerState.tick + 1;

                while (tickToProcess <= tick)
                {
                    Debug.Log("correcting");
                    
                    uint bufferIndex = tickToProcess % BUFFER_SIZE;
                    uint previousBufferIndex = (tickToProcess - 1) % BUFFER_SIZE;
                    
                    transform.position = _stateBuffer[previousBufferIndex].position;
                    transform.forward = _stateBuffer[previousBufferIndex].inputUsed.forward;

                    PlayerInput input = _inputBuffer[previousBufferIndex];
                    
                    Debug.Log(transform.position);
                    Movement(input);
                    Debug.Log(transform.position);

                    PlayerMovementState recalculatedState = new PlayerMovementState();
                    recalculatedState.position = transform.position;
                    recalculatedState.inputUsed = input;
                    recalculatedState.didTeleport = false;
                    recalculatedState.tick = tickToProcess;

                    _stateBuffer[bufferIndex] = recalculatedState;

                    tickToProcess++;
                }
            }
        }

        private void Update()
        {
            for (int i = 0; i < movementInputs.Length; i++)
                movementInputs[i] = false;
            if (canMove)
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
                if (Input.GetKey(KeyCode.LeftControl))
                    movementInputs[6] = true;
            }
        }

        private void Movement(PlayerInput input)
        {
            Vector2 inputDirection = Vector2.zero;
            if (input.inputs[0])
                inputDirection.y += 1;

            if (input.inputs[1])
                inputDirection.y -= 1;

            if (input.inputs[2])
                inputDirection.x -= 1;

            if (input.inputs[3])
                inputDirection.x += 1;

            Move(inputDirection, input.inputs[4], input.inputs[5], input.inputs[6]);
        }
        
        private void Move(Vector2 inputDirection, bool jump, bool sprint, bool isCrouching)
        {
            bool respawning = SceneManager.GetActiveScene().name == "Lobby" ? false : _player.respawning;
            
            if(canMove && !respawning)
            {
                inputDirection.Normalize();
                transform.rotation = FlattenQuaternion(camTransform.rotation);

                _forwardVelocity = inputDirection.y * movementSpeed;
                _sidewaysVelocity = inputDirection.x * movementSpeed;
                
                if (sprint && !isCrouching)
                {
                    _forwardVelocity *= runMultiplier;
                    _sidewaysVelocity *= runMultiplier;
                }

                // to avoid weird errors, we cannot process crouching in the lobby
                if(SceneManager.GetActiveScene().name != "Lobby")
                {
                    if (isCrouching)
                    {
                        _forwardVelocity *= crouchMultiplier;
                        _sidewaysVelocity *= crouchMultiplier;
                        camTransform.position = crouchingCameraPos.position;
                        defaultModel.SetActive(false);
                        crouchingModel.SetActive(true);
                    }
                    else
                    {
                        camTransform.position = defaultCameraPos.position;
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

                if (grounded && jump && canJump)
                {
                    _verticalVelocity = jumpHeight;
                }

                _verticalVelocity -= gravity * Time.fixedDeltaTime;
                direction.y = _verticalVelocity;

                direction = transform.TransformDirection(direction);

                MovePlayer(direction);
            }
        }
    
        private Quaternion FlattenQuaternion(Quaternion quaternion)
        {
            quaternion.x = 0;
            quaternion.z = 0;
            return quaternion;
        }

        private void MovePlayer(Vector3 direction)
        {
            if (direction.magnitude > 0)
            {
                _rb.velocity = direction * Time.deltaTime;
            }
        }

        private void SendMovementInput(PlayerInput input)
        {
            if (SceneManager.GetActiveScene().name == "Lobby")
            {
                Message message = Message.Create(MessageSendMode.Unreliable, ClientToLobbyServer.movementInput);
                message.AddPlayerInput(input);
                NetworkManager.Singleton.Client.Send(message);    
            }
            else
            {
                Message message = Message.Create(MessageSendMode.Unreliable, ClientToServerId.movementInput);
                message.AddPlayerInput(input);
                NetworkManager.Singleton.Client.Send(message);  
            }
        }

        public void ReceivedServerMovementState(PlayerMovementState state)
        {
            _lastServerState = state;
        }
    }
}