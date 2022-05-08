using UnityEngine;
using Photon.Pun;

public class Pickup : MonoBehaviourPunCallbacks
{
    public int grenadeAmount;
    public int healthAmount;
    public GameObject player;

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.GetComponent<Player>() != null)
        {
            player = other.gameObject;

            photonView.ViewID = 1;

            photonView.RPC("GiveHealth", RpcTarget.All);
            photonView.RPC("GiveGrenade", RpcTarget.All);
        }
    }

    [PunRPC]
    public void GiveHealth()
    {
        player.GetComponent<Player>().currentHealth += healthAmount;
        Destroy(gameObject);
    }

    [PunRPC]
    public void GiveGrenade()
    {
        player.GetComponent<GrenadeManager>().clip += grenadeAmount;
        Destroy(gameObject);
    }
}
