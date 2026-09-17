using ArtifactCourier.Player;
using UnityEngine;

namespace ArtifactCourier.Delivery
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class OilSlick : MonoBehaviour
    {
        [SerializeField] private float slowMultiplier = 0.58f;
        [SerializeField] private float slowSeconds = 2.2f;
        [SerializeField] private float lifeSeconds = 8f;
        [SerializeField] private AudioClip slowClip;

        public void Configure(AudioClip clip, float lifetime = 8f)
        {
            slowClip = clip;
            lifeSeconds = lifetime;
        }

        private void Start() => Destroy(gameObject, lifeSeconds);

        private void OnTriggerEnter2D(Collider2D other)
        {
            var effects = other.GetComponentInParent<PlayerStatusEffects>();
            if (effects == null) return;
            effects.ApplySlow(slowMultiplier, slowSeconds);
            if (slowClip != null) AudioSource.PlayClipAtPoint(slowClip, transform.position, 0.65f);
            Destroy(gameObject);
        }
    }
}
