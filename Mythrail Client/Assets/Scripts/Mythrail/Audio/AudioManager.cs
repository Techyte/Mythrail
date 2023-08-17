using System.Collections.Generic;
using UnityEngine;

namespace Mythrail.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager instance;
        
        private List<Sound> _sounds = new List<Sound>();

        private float _volumeMultiplier;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            AudioClip[] clips = Resources.LoadAll<AudioClip>("Sounds");

            foreach (AudioClip c in clips)
            {
                Sound s = new Sound(this);
                
                s.audioSource = gameObject.AddComponent<AudioSource>();
                s.clip = c;
                s.audioSource.clip = s.clip;
                s.audioSource.playOnAwake = false;
                
                _sounds.Add(s);
            }
        }

        public float GetVolumeMultiplier()
        {
            return _volumeMultiplier;
        }

        public void ChangeVolume(float value)
        {
            _volumeMultiplier = value;
        }

        public void Play(string name, float volume = 1, float pitch = 1)
        {
            foreach (Sound s in _sounds)
            {
                s.GetReadyForUse(volume, pitch);
                s.Play();
            }
        }
    }   
}