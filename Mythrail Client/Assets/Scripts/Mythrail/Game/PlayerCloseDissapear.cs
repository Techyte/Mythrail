using UnityEngine;

public class PlayerCloseDissapear : MonoBehaviour
{
    [SerializeField] private MeshRenderer _renderer;
    [SerializeField] private bool playerNear;

    private void Update()
    {
        _renderer.enabled = !playerNear;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            playerNear= true;
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            playerNear = false;
        }
    }
}
