using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MythrailEngine
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
            if (SceneManager.GetActiveScene().buildIndex != 1)
            {
                to = new TransformUpdate(NetworkManager.Singleton.ServerTick, false, transform.position);
                from = new TransformUpdate(NetworkManager.Singleton.ServerTick, false, transform.position);
                previous = new TransformUpdate(NetworkManager.Singleton.ServerTick, false, transform.position);   
            }
            else
            {
                to = new TransformUpdate(LobbyNetworkManager.Singleton.ServerTick, false, transform.position);
                from = new TransformUpdate(LobbyNetworkManager.Singleton.ServerTick, false, transform.position);
                previous = new TransformUpdate(LobbyNetworkManager.Singleton.ServerTick, false, transform.position);
            }
        }

        private void Update()
        {
            if (SceneManager.GetActiveScene().buildIndex != 1)
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
                        timeToReachTarget = (to.Tick - from.Tick) * Time.fixedDeltaTime;
                    }
                }   
            }
            else
            {
                for (int i = 0; i < futureTransformUpdates.Count; i++)
                {
                    if (LobbyNetworkManager.Singleton.ServerTick >= futureTransformUpdates[i].Tick)
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
                            from = new TransformUpdate(LobbyNetworkManager.Singleton.InterpolationTick, false, transform.position);
                        }

                        futureTransformUpdates.RemoveAt(i);
                        i--;
                        timeElapsed = 0f;
                        timeToReachTarget = (to.Tick - from.Tick) * Time.fixedDeltaTime;
                    }
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

            transform.position = Vector3.LerpUnclamped(from.Position, to.Position, lerpAmount);
        }

        public void NewUpdate(uint tick, bool isTeliport, Vector3 position)
        {
            if (SceneManager.GetActiveScene().buildIndex != 1)
            {
                if (tick <= NetworkManager.Singleton.InterpolationTick)
                    return;   
            }
            else
            {
                if (tick <= LobbyNetworkManager.Singleton.InterpolationTick)
                    return;
            }

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
    }

}