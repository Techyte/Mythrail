using System;
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
        [SerializeField] private CapsuleCollider collider;

        [SerializeField] private Transform groundDetector;
        [SerializeField] private float groundDistanceAllowed;
        [SerializeField] private LayerMask groundLayer;

        private bool canJump = true;
        private bool grounded = true;
        public bool canMove = true;

        private Player _player;

        // end of client prediction

        private void Awake()
        {
            _player = GetComponent<Player>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.W))
            {
                
            }
        }

        public void HandleTick()
        {
            
        }
        
        private void Move(Vector2 inputDirection, bool jump, bool sprint, bool isCrouching)
        {
            bool respawning = SceneManager.GetActiveScene().name == "Lobby" ? false : _player.respawning;
            
            if(canMove && !respawning)
            {
                inputDirection.Normalize();
                transform.rotation = FlattenQuaternion(camTransform.rotation);

                // _forwardVelocity = inputDirection.y * movementSpeed;
                // _sidewaysVelocity = inputDirection.x * movementSpeed;
                
                // if (sprint && !isCrouching)
                // {
                //     _forwardVelocity *= runMultiplier;
                //     _sidewaysVelocity *= runMultiplier;
                // }

                // // to avoid weird errors, we cannot process crouching in the lobby
                // if(SceneManager.GetActiveScene().name != "Lobby")
                // {
                //     if (isCrouching)
                //     {
                //         StartCrouching();
                //     }
                //     else
                //     {
                //         StopCrouching();
                //     }
                // }

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
                //direction.y = _verticalVelocity;

                direction = transform.TransformDirection(direction);

                MovePlayer(transform.TransformDirection(new Vector3(inputDirection.x, 0, inputDirection.y)));
            }
        }

        private void StartCrouching()
        {
            _forwardVelocity *= crouchMultiplier;
            _sidewaysVelocity *= crouchMultiplier;
            camTransform.position = crouchingCameraPos.position;
            defaultModel.SetActive(false);
            crouchingModel.SetActive(true);
        }

        private void StopCrouching()
        {
            camTransform.position = defaultCameraPos.position;
            defaultModel.SetActive(true);
            crouchingModel.SetActive(false);
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
                transform.position += direction;
            }
        }
    }
}