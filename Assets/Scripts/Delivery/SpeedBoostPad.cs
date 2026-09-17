using ArtifactCourier.Player;
using UnityEngine;

namespace ArtifactCourier.Delivery
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class SpeedBoostPad : MonoBehaviour
    {
        [SerializeField] private float multiplier = 1.65f;
        [SerializeField] private float duration = 5f;
        [SerializeField] private AudioClip boostClip;
        private bool consumed;

        public void Configure(AudioClip clip, float speedMultiplier = 1.65f, float seconds = 5f)
        {
            boostClip = clip;
            multiplier = speedMultiplier;
            duration = seconds;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (consumed) return;
            PlayerStatusEffects effects = other.GetComponentInParent<PlayerStatusEffects>();
            if (effects == null) return;

            consumed = true;
            effects.ApplyBoost(multiplier, duration);
            PlayerVisualEffects vfx = other.GetComponentInParent<PlayerVisualEffects>();
            vfx?.StartBoostTrail(duration);
            vfx?.PlayPickupBurst(transform.position, new Color(0.18f, 0.72f, 1f, 1f));
            if (boostClip != null) AudioSource.PlayClipAtPoint(boostClip, transform.position, 0.75f);
            Destroy(gameObject);
        }
    }
}
