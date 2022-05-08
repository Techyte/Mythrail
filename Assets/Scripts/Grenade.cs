using Photon.Pun;
using UnityEngine;

public class Grenade : MonoBehaviourPunCallbacks
{
    public Collider[] hitColliders;
    public float pintime;
    public float graniteRadiusEffect;
    public GameObject ExplosionEffectParticles;
    GameObject effect;
    bool hasExploded;
    public GameObject creator;

    void Start()
    {
        pintime = 6.0f;
        graniteRadiusEffect = 20.0f;
        Invoke("InitialExplosion", pintime);
    }

    private void Update()
    {
        photonView.ViewID = 2;
        if (transform.position.y <= 1.5)
        {
            photonView.RPC("ExplosionEffect", RpcTarget.All);
        }
    }

    [PunRPC]
    public void ExplosionEffect()
    {
        creator.GetComponent<GrenadeManager>().currentGrenade = null;
        hasExploded = true;
        effect = Instantiate(ExplosionEffectParticles, transform.position, Quaternion.identity);
        Invoke("DestroyEffect", pintime);
        gameObject.SetActive(false);
        hitColliders = Physics.OverlapSphere(transform.position, graniteRadiusEffect);

        foreach (var item in hitColliders)
        {
            if (item.GetComponent<Rigidbody>() != null)
            {
                Vector3 distance = transform.position - item.transform.position;

                item.GetComponent<Rigidbody>().AddForce((-transform.position + item.transform.position).normalized * (500.0f / distance.magnitude));
                item.GetComponent<Rigidbody>().AddForce(item.transform.up * (500.0f / distance.magnitude));

                if(item.GetComponent<Player>() != null)
                {
                    item.transform.gameObject.GetPhotonView().RPC("TakeDamage", RpcTarget.All, 25);
                }

                if (item.GetComponent<Grenade>() != null)
                {
                    item.transform.gameObject.GetPhotonView().RPC("ExplosionEffect", RpcTarget.All);
                }
            }
        }
    }

    void InitialExplosion()
    {
        if (!hasExploded)
        {
            photonView.RPC("ExplosionEffect", RpcTarget.All);
        }
    }

    void DestroyEffect()
    {
        Destroy(effect);
        Destroy(gameObject);
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, 2.0f);
    }
}
