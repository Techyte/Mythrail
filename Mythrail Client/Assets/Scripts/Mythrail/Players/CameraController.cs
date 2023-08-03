using Mythrail.General;
using Mythrail.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mythrail.Players
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private float sensitivity = 100f;
        [SerializeField] private float clampAngle = 85f;

        public bool canPause = true;
        [SerializeField] private GameObject pauseScreen;

        private float verticalRotation;
        private float horizontalRotation;

        private void Start()
        {
            verticalRotation = transform.localEulerAngles.x;
            horizontalRotation = transform.localEulerAngles.y;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                ToggleCursorMode();
            
            if(SceneManager.GetActiveScene().name == "Lobby")
            {
                if (LobbyPlayer.LocalPlayer)
                {
                    if (Cursor.lockState != CursorLockMode.None && LobbyPlayer.LocalPlayer.playerController.canMove)
                        Look();
                }
            }
            else
            {
                if (Player.LocalPlayer)
                {
                    if (Cursor.lockState != CursorLockMode.None && Player.LocalPlayer.playerController.canMove)
                        Look();
                }
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && canPause)
            {
                Cursor.lockState = CursorLockMode.None;
                pauseScreen.SetActive(true);
            }
        }

        private void Look()
        {
            float mouseVertical = -Input.GetAxis("Mouse Y");
            float mouseHorizontal = Input.GetAxis("Mouse X");

            verticalRotation += mouseVertical * sensitivity * Time.deltaTime;
            horizontalRotation += mouseHorizontal * sensitivity * Time.deltaTime;

            verticalRotation = Mathf.Clamp(verticalRotation, -clampAngle, clampAngle);

            transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
            player.rotation = Quaternion.Euler(0f, horizontalRotation, 0f);
        }

        public void ToggleCursorMode()
        {
            if (canPause)
            {
                if (Cursor.lockState == CursorLockMode.None)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    pauseScreen.SetActive(false);
                    Cursor.visible = false;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.None;
                    pauseScreen.SetActive(true);
                    Cursor.visible = true;
                }   
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                pauseScreen.SetActive(false);
                Cursor.visible = true;
            }
        }

        public void Resume()
        {
            ToggleCursorMode();
        }

        public void Exit()
        {
            Cursor.lockState = CursorLockMode.None;
            pauseScreen.SetActive(true);
            Player.list.Clear();
            NetworkManager.Singleton.Disconnect();
            SceneManager.LoadScene("MainMenu");
        }
    }

}