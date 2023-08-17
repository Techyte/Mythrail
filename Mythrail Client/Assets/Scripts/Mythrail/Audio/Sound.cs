using UnityEngine;

namespace Mythrail.Audio
{
    public class Sound
    {
        public AudioSource audioSource;
        public AudioClip clip;
        public float volume;
        public float pitch;

        private AudioManager _manager;

        public Sound(AudioManager manager)
        {
            _manager = manager;
        }

        public void GetReadyForUse(float volume, float pitch)
        {
            this.volume = volume;
            this.pitch = pitch;
        }

        public void Play()
        {
            audioSource.volume = volume * _manager.GetVolumeMultiplier();
            audioSource.pitch = pitch;
            audioSource.Play();
        }
    }   
}