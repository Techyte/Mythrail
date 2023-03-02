using Mythrail.Multiplayer;
using UnityEngine.SceneManagement;

namespace Mythrail.General
{
    public static class ObjectLoaderManager
    {
        public static void LoadMainMenu()
        {
            NetworkManager.Singleton.SelfDestruct();
            NetworkManager.Singleton.Disconnect();
            SceneManager.LoadScene("MainMenu");
        }
    }
}