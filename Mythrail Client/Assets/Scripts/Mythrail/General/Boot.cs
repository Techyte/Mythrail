using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mythrail.General
{
    public class Boot : MonoBehaviour
    {
        [SerializeField] private string sceneToLoadName = "MainMenu";
        [SerializeField] private GameObject[] dontDestroyOnLoadObjects;

        private void Start()
        {
            foreach (var obj in dontDestroyOnLoadObjects)
            {
                DontDestroyOnLoad(obj);
            }

            SceneManager.LoadScene(sceneToLoadName);
        }
    }   
}