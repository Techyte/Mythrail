using UnityEngine;
using Photon.Pun;
using System.Collections;

public class GameManager : MonoBehaviourPunCallbacks
{
    public string playerPrefab;
    public Transform[] spawnPoints;
    public static Transform[] publicSpawnPoints;

    public static GameManager instance;

    [Space]
    public float PickupDropDelay;
    public GameObject[] pickups;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Debug.Log("More than one instance of the GameManager was found");
            Destroy(gameObject);
        }
    }

    public void Start()
    {
        publicSpawnPoints = spawnPoints;
        Spawn();
        StartCoroutine(PickupDrop());
    }

    public void Spawn()
    {
        Transform spawn = spawnPoints[Random.Range(0, spawnPoints.Length)];
        PhotonNetwork.Instantiate(playerPrefab, spawn.position, spawn.rotation);
    }

    IEnumerator PickupDrop()
    {
        int xPos = Random.Range(-49, 49);
        int yPos = Random.Range(-49, 49);
        Vector3 spawnPos = new Vector3(xPos, 30, yPos);
        int pickupID = Random.Range(0, pickups.Length);

        photonView.RPC("SpawnPickup", RpcTarget.All, pickupID, spawnPos);

        yield return new WaitForSeconds(PickupDropDelay);
        StartCoroutine(PickupDrop());
    }

    [PunRPC]
    void SpawnPickup(int pickupID, Vector3 spawnPos)
    {
        Instantiate(pickups[pickupID], spawnPos, Quaternion.identity);
    }
}
