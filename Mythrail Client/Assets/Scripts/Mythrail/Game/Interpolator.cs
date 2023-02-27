using System.Collections.Generic;
using Mythrail.Multiplayer;
using UnityEngine;

namespace Mythrail.Game
{
    public class Interpolator : MonoBehaviour
    {
        [SerializeField] private float timeElapsed = 0f;
        [SerializeField] private float timeToReachTarget = 0.05f;
        [SerializeField] private float movementThreshold = 0.05f;

        private readonly List<TransformUpdate> futureTransformUpdates = new List<TransformUpdate>();
        private float squareMovementThreshold;
        private TransformUpdate to;
        private TransformUpdate from;
        private TransformUpdate previous;

        private void Start()
        {
            squareMovementThreshold = movementThreshold * movementThreshold;
            to = new TransformUpdate(NetworkManager.Singleton.ServerTick, false, transform.position);
            from = new TransformUpdate(NetworkManager.Singleton.ServerTick, false, transform.position);
            previous = new TransformUpdate(NetworkManager.Singleton.ServerTick, false, transform.position);   
        }

        private void Update()
        {
            for (int i = 0; i < futureTransformUpdates.Count; i++)
            {
                if (NetworkManager.Singleton.ServerTick >= futureTransformUpdates[i].Tick)
                {
                    if (futureTransformUpdates[i].IsTeliport)
                    {
                        to = futureTransformUpdates[i];
                        from = to;
                        previous = to;
                        transform.position = to.Position;
                    }
                    else
                    {
                        previous = to;
                        to = futureTransformUpdates[i];
                        from = new TransformUpdate(NetworkManager.Singleton.InterpolationTick, false, transform.position);
                    }

                    futureTransformUpdates.RemoveAt(i);
                    i--;
                    timeElapsed = 0f;
                    float ticksToReach = (to.Tick - from.Tick);
                    if (ticksToReach == 0f) ticksToReach = 1f;
                    timeToReachTarget = ticksToReach * Time.fixedDeltaTime;
                }
            }

            timeElapsed += Time.deltaTime;
            InterpolatePosition(timeElapsed / timeToReachTarget);
        }

        private void InterpolatePosition(float lerpAmount)
        {
            if ((to.Position - previous.Position).sqrMagnitude < squareMovementThreshold)
            {
                if (to.Position != from.Position)
                    transform.position = Vector3.Lerp(from.Position, to.Position, lerpAmount);

                return;
            }

            Vector3 newPosition = Vector3.LerpUnclamped(from.Position, to.Position, lerpAmount);

            try
            {
                transform.position = newPosition;
            }
            catch
            {
                Debug.Log(newPosition);
            }
        }

        public void NewUpdate(uint tick, bool isTeliport, Vector3 position)
        {
            if (IsInfinite(position))
                return;
            
            if (tick <= NetworkManager.Singleton.InterpolationTick)
                return; 

            for (int i = 0; i < futureTransformUpdates.Count; i++)
            {
                if (tick < futureTransformUpdates[i].Tick)
                {
                    futureTransformUpdates.Insert(i, new TransformUpdate(tick, isTeliport, position));
                    return;
                }
            }

            futureTransformUpdates.Add(new TransformUpdate(tick, isTeliport, position));
        }

        public bool IsInfinite(Vector3 position)
        {
            if (float.IsNegativeInfinity(position.x) || float.IsPositiveInfinity(position.x))
                return true;
            if (float.IsNegativeInfinity(position.y) || float.IsPositiveInfinity(position.y))
                return true;
            if (float.IsNegativeInfinity(position.z) || float.IsPositiveInfinity(position.z))
                return true;

            return false;
        }
    }
}