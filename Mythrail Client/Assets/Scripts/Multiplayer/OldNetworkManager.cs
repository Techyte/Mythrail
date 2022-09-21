using UnityEngine;
using RiptideNetworking;

namespace MythrailEngine
{
    public class OldNetworkManager : MonoBehaviour
    {
        private static OldNetworkManager _singleton;
        public static OldNetworkManager Singleton
        {
            get => _singleton;
            private set
            {
                if (_singleton == null)
                    _singleton = value;
                else if (_singleton != value)
                {
                    Debug.Log($"{nameof(OldNetworkManager)} instance already exists, destroying duplicate!");
                    Destroy(value);
                }
            }
        }

        public Client Client { get; private set; }

        private uint _serverTick;
        public uint ServerTick
        {
            get => _serverTick;
            private set
            {
                _serverTick = value;
                InterpolationTick = (uint)(value - TicksBetweenPositionUpdates);
            }
        }
        public uint InterpolationTick { get; private set; }
        private uint _ticksBetweenPositionUpdates = 2;
        public uint TicksBetweenPositionUpdates
        {
            get => _ticksBetweenPositionUpdates;
            set
            {
                _ticksBetweenPositionUpdates = value;
                InterpolationTick = (uint)(ServerTick - value);
            }
        }

        [SerializeField] private string ip;
        [SerializeField] private ushort port;
        [SerializeField] private string username;
    }

}