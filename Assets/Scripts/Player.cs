using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;
using System.Collections;

public class Player : MonoBehaviourPunCallbacks
{
    //Variables
    public Camera PlayerCamera;

    public LayerMask ground;

    public GameObject groundDetector;

    public GameObject cameraParent;

    public Rigidbody rb;

    public int maxHealth = 50;

    public float lookSensitivity = 1f;

    float CameraPitch = 0;

    public float MovementMultiplyer = 2000;

    public float RunningMultiplyer = 1.5f;

    public float HorizontalDrag = 0.1f;

    public float JumpStrength = 400;

    public Transform weapon;

    public Transform weaponParent;

    public TextMeshPro Username;

    public new CapsuleCollider collider;

    public MeshFilter filter;

    public Mesh defaultMesh, CrouchingMesh;

    public bool canJump = true;

    public TextMeshProUGUI killCounter;

    public TextMeshProUGUI deathCounter;



    [HideInInspector]public ProfileData playerProfile;



    private Transform UIHealthBar;



    private Vector3 targetWeaponBobPosition;

    private Vector3 weaponParentOrign;



    private float movementCounter;

    private float idleCounter;

    public int kills;

    public int deaths;

    public GunManager gunManager;



    Vector2 strafeValue;

    private float baseFOV;

    private float runFOVModifyer = 1.25f;

    public int currentHealth;

    private TextMeshProUGUI UIUsername;

    [Space]
    [Header("Controlls")]
    public KeyCode RunButton = KeyCode.LeftShift;

    public KeyCode CrouchButton = KeyCode.LeftControl;

    public KeyCode pauseButton = KeyCode.Escape;



    private void Start()
    {
        //So that we can set the health back to max with pickups later on
        currentHealth = maxHealth;

        if (!photonView.IsMine) gameObject.layer = 9;

        killCounter = GameObject.Find("KillCounter").GetComponent<TextMeshProUGUI>();

        deathCounter = GameObject.Find("DeathCounter").GetComponent<TextMeshProUGUI>();

        //Sets health bar and username correctly to it can be updated and sent to all other players
        if (photonView.IsMine)
        {
            UIHealthBar = GameObject.Find("HUD/Health/Bar").transform;
            UIUsername = GameObject.Find("HUD/Username/UsernameText").GetComponent<TextMeshProUGUI>();
            RefreshHealthBar();

            UIUsername.text = Launcher.myProfile.username;
            photonView.RPC("SyncProfile", RpcTarget.All, Launcher.myProfile.username);
        }

        //So we can go back to the baseFOV after we are done running or aiming
        baseFOV = PlayerCamera.fieldOfView;
        cameraParent.SetActive(photonView.IsMine);

        //So we can go back to the origonal weapon position if we need to
        weaponParentOrign = weaponParent.localPosition;

        //Locks and hides the cursor so it wont move when we move out mouse to look around
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }



    private void Update()
    {
        //To make sure we don't accedentaly acces the wrong players
        if (!photonView.IsMine) return;

        deathCounter.text = deaths.ToString();
        killCounter.text = kills.ToString();
        Debug.Log(kills);

        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }

        //Sets all of the values we will need for the update
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        strafeValue.x = Input.GetAxisRaw("Horizontal");
        strafeValue.y = Input.GetAxisRaw("Vertical");
        strafeValue.Normalize();

        bool isGrounded = Physics.Raycast(groundDetector.transform.position, Vector3.down, 0.1f, ground);

        bool isCrouching = Input.GetKey(CrouchButton);

        bool pause = Input.GetKeyDown(pauseButton);

        //Sees if we need to pause the players game
        if (pause)
        {
            GameObject.Find("Pause").GetComponent<Pause>().TogglePause();
        }

        //sets all the values to 0 so we can't do anything while paused
        if (Pause.paused)
        {
            strafeValue.x = 0f;
            strafeValue.y = 0f;
            mouseX = 0f;
            mouseY = 0f;
            isGrounded = false;
            pause = false;
            isCrouching = false;
        }

        //Sets the FOV of the camera smaller if we are aiming
        if (GunManager.isAiming)
        {
            PlayerCamera.fieldOfView = Mathf.Lerp(PlayerCamera.fieldOfView, 45, Time.deltaTime * 8f);
        }
        else
        {
            PlayerCamera.fieldOfView = Mathf.Lerp(PlayerCamera.fieldOfView, baseFOV, Time.deltaTime * 8f);
        }

        //Crouches if we want to crouch
        photonView.RPC("Crouch", RpcTarget.All, isCrouching);

        //Checks if we want to start running
        bool Run = Input.GetKey(RunButton);
        bool isRunning = Run && strafeValue.y > 0;

        //Changes the speed and FOV if we are running
        float adjustedSpeed = MovementMultiplyer;
        if (isRunning && !GunManager.isAiming)
        {
            adjustedSpeed *= RunningMultiplyer;
            PlayerCamera.fieldOfView = Mathf.Lerp(PlayerCamera.fieldOfView, baseFOV * runFOVModifyer, Time.deltaTime * 8f);
        }
        else
        {
            PlayerCamera.fieldOfView = Mathf.Lerp(PlayerCamera.fieldOfView, baseFOV, Time.deltaTime * 8f);
        }

        //If we are aiming to crouching lower the speed
        if (GunManager.isAiming || isCrouching)
        {
            adjustedSpeed /= 2;
        }


        //Allows us to look around
        Vector2 lookValue = new Vector2(mouseX, mouseY);
        CameraPitch += -lookValue.y * lookSensitivity;

        if (CameraPitch < -90) CameraPitch = -90;
        if (CameraPitch > 90) CameraPitch = 90;

        PlayerCamera.transform.localEulerAngles = new Vector3(CameraPitch, 0, 0);
        transform.eulerAngles += new Vector3(0, lookValue.x * lookSensitivity, 0);

        weapon.rotation = PlayerCamera.transform.rotation;


        //Allows us to move
        rb.AddRelativeForce(new Vector3(strafeValue.x, 0, strafeValue.y) * adjustedSpeed * Time.deltaTime);

        //Allows us to jump
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && canJump)
        {
            rb.AddForce(Vector3.up * JumpStrength);
        }

        //Headbob
        if (strafeValue.x == 0 && strafeValue.y == 0)
        {
            if (GunManager.isAiming)
            {
                HeadBob(idleCounter, 0.01f, 0.01f);
                idleCounter += Time.deltaTime * 1.5f;
            }
            else
            {
                HeadBob(idleCounter, 0.02f, 0.02f);
                idleCounter += Time.deltaTime * 1.5f;
            }
            weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 2f);
        }
        else
        {
            if (GunManager.isAiming)
            {
                HeadBob(idleCounter, 0.01f, 0.01f);
                idleCounter += Time.deltaTime * 1.50f;
            }
            else
            {
                HeadBob(movementCounter, 0.035f, 0.035f);
                movementCounter += Time.deltaTime * 2f;
            }
            weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 6f);
        }

        //Syncs the displayed health with the actuall health
        RefreshHealthBar();
    }

    [PunRPC]
    public void Crouch(bool isCrouching)
    {
        if (isCrouching)
        {
            filter.mesh = CrouchingMesh;
            collider.height = 1f;
            canJump = false;
        }
        else
        {
            filter.mesh = defaultMesh;
            collider.height = 2f;
            canJump = true;
        }
    }



    private void FixedUpdate()
    {
        if (!photonView.IsMine) return;
        rb.velocity -= new Vector3(rb.velocity.x * HorizontalDrag, 0, rb.velocity.z * HorizontalDrag);
    }



    void HeadBob(float z, float xIntensity, float yIntensity)
    {
        targetWeaponBobPosition = weaponParent.localPosition + new Vector3 (Mathf.Cos(z) * xIntensity, Mathf.Sin(z * 2) * yIntensity, 0);
    }



    void RefreshHealthBar()
    {
        float healthRatio = (float)currentHealth / (float)maxHealth;
        UIHealthBar.localScale = Vector3.Lerp(UIHealthBar.localScale , new Vector3(healthRatio, 1, 1), Time.deltaTime * 8f);

        if(currentHealth <= 10)
        {
            UIHealthBar.GetComponent<Image>().color = Color.red;
        }
        else
        {
            UIHealthBar.GetComponent<Image>().color = Color.green;
        }
    }


    [PunRPC]
    public void TakeDamagePlayer(int damage)
    {
        if (photonView.IsMine)
        {
            currentHealth -= damage;
            RefreshHealthBar();

            if(currentHealth <= 0)
            {
                deaths++;
                Spawn();
            }
        }
    }

    [PunRPC]
    public void TakeDamagerFromPlayer(int damage, Player damageSource)
    {
        if (photonView.IsMine)
        {
            currentHealth -= damage;
            RefreshHealthBar();

            if (currentHealth <= 0)
            {
                deaths++;
                damageSource.kills++;
                Spawn();
            }
        }
    }

    void Spawn()
    {
        Transform spawn = GameManager.instance.spawnPoints[Random.Range(0, GameManager.instance.spawnPoints.Length)];
        transform.position = spawn.position;
        transform.rotation = spawn.rotation;

        //if (gunManager.currentWeapon != null) Destroy(gunManager.currentWeapon);
        currentHealth = maxHealth;
    }



#region PunRPCs
    [PunRPC]
    private void SyncProfile(string username)
    {
        Username.text = username;
    }
#endregion
}