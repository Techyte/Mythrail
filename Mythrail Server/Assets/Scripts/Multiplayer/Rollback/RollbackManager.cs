using UnityEngine;

namespace Multiplayer.Rollback
{
    public class RollbackManager : MonoBehaviour
    {
        public static RollbackManager Instance;

        private void Awake()
        {
            Instance = this;
        }
    }   
}