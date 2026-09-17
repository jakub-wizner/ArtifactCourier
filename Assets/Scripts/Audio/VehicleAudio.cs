using ArtifactCourier.Player;
using UnityEngine;

namespace ArtifactCourier.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class VehicleAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip engineLoop;
        [SerializeField] private float minimumPitch = 0.78f;
        [SerializeField] private float maximumPitch = 1.45f;
        [SerializeField] private float volume = 0.32f;
        [SerializeField, Range(0f, 1f)] private float spatialBlend = 0f;

        private AudioSource source;
        private Rigidbody2D body;
        private CarController playerCar;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            body = GetComponent<Rigidbody2D>();
            playerCar = GetComponent<CarController>();
        }

        private void Start()
        {
            source.clip = engineLoop;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = spatialBlend;
            source.volume = volume;
            if (engineLoop != null) source.Play();
        }

        private void Update()
        {
            float speed = body == null ? 0f : body.linearVelocity.magnitude;
            float reference = playerCar == null ? 12f : Mathf.Max(1f, playerCar.MaxSpeed);
            source.pitch = Mathf.Lerp(minimumPitch, maximumPitch, Mathf.Clamp01(speed / reference));
        }

        public void Configure(AudioClip clip, float audioVolume = 0.32f, float blend = 0f)
        {
            engineLoop = clip;
            volume = audioVolume;
            spatialBlend = Mathf.Clamp01(blend);
        }
    }
}
