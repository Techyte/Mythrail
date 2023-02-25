using UnityEngine;

namespace Mythrail.Game
{
    public class TransformUpdate
    {
        public uint Tick { get; private set; }
        public bool IsTeliport { get; private set; }
        public Vector3 Position { get; private set; }

        public TransformUpdate(uint tick, bool isTeliport, Vector3 position)
        {
            Tick = tick;
            IsTeliport = isTeliport;
            Position = position;
        }
    }

}