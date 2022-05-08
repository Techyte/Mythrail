using Photon.Pun;
using UnityEngine;
using System.Collections;
using TMPro;

public class GrenadeManager : MonoBehaviourPunCallbacks
{
    public int clip = 5;
    public GameObject grenade;
    public Transform playerCamera;
    public Transform grenadeSpawnPos;
    public GameObject currentGrenade;
    public float grenadeThrowingDelay = 1f;
    [SerializeField]bool canThrow = true;
    TextMeshProUGUI clipDisplay;


    private void Start()
    {
        clipDisplay = GameObject.Find("GrenadeDisplay").GetComponent<TextMeshProUGUI>();
    }
    void Update()
    {
        if (!photonView.IsMine) return;

        if(clip > 50)
        {
            clip = 50;
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            if (clip > 0 && canThrow && !currentGrenade)
            {
                ThrowGrenade();
                StartCoroutine(grenadeThrowingTimer());
            }
            else if (currentGrenade)
            {
                currentGrenade.GetPhotonView().RPC("ExplosionEffect", RpcTarget.All);
            }
        }

        clipDisplay.text = clip.ToString();
    }

    IEnumerator grenadeThrowingTimer()
    {
        canThrow = false;
        yield return new WaitForSeconds(grenadeThrowingDelay);
        canThrow = true;
    }

    [PunRPC]
    void ThrowGrenade()
    {
        GameObject newGrenade =  Instantiate(grenade, grenadeSpawnPos.position, grenadeSpawnPos.transform.rotation);
        newGrenade.GetComponent<Grenade>().creator = gameObject;
        Debug.Log(newGrenade.GetComponent<Grenade>().creator);
        currentGrenade = newGrenade;

        Vector3 direction = transform.forward;
        Debug.Log(transform.forward);
        direction.y -= playerCamera.rotation.y;
        newGrenade.GetComponent<Rigidbody>().velocity = playerCamera.forward * 15f;

        clip--;
    }
}
