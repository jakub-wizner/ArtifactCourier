using UnityEngine;

namespace ArtifactCourier.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class CityMusicPlayer : MonoBehaviour
    {
        [SerializeField] private AudioClip music;
        [SerializeField, Range(0f, 1f)] private float volume = 0.22f;
        private AudioSource source;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            ApplySettings();
        }

        private void Start()
        {
            if (music != null && !source.isPlaying)
            {
                source.Play();
            }
        }

        public void Configure(AudioClip clip, float musicVolume)
        {
            music = clip;
            volume = Mathf.Clamp01(musicVolume);
            if (source == null)
            {
                source = GetComponent<AudioSource>();
            }
            ApplySettings();
        }

        private void ApplySettings()
        {
            if (source == null)
            {
                return;
            }
            source.clip = music;
            source.loop = true;
            source.playOnAwake = true;
            source.volume = volume;
            source.spatialBlend = 0f;
        }
    }
}
