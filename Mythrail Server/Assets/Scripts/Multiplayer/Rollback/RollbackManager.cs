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

        public void RollbackAllPlayerStatesTo(uint tick)
        {
            Debug.Log("rolling back");
            uint bufferIndex = tick % PlayerMovement.BUFFER_SIZE;

            foreach (var playerToRollback in Player.list.Values)
            {
                playerToRollback.Movement.AssertStateFromBufferIndex(bufferIndex);
            }
        }

        public void ResetAllPlayersToPresentPosition(ushort exception)
        {
            foreach (var player in Player.list.Values)
            {
                if(player.Id != exception)
                {
                    player.Movement.ResetToPresentPosition();
                }
            }
        }
    }   
}