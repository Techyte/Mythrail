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

        public void RollbackOtherPlayerStatesTo(uint tick, ushort exception)
        {
            uint bufferIndex = tick % PlayerMovement.BUFFER_SIZE;

            foreach (var playerToRollback in Player.list.Values)
            {
                if(playerToRollback.Id != exception)
                {
                    playerToRollback.Movement.AssertStateFromBufferIndex(bufferIndex);
                }
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