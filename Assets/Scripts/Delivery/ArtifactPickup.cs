using ArtifactCourier.Player;
using UnityEngine;

namespace ArtifactCourier.Delivery
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class ArtifactPickup : MonoBehaviour
    {
        [SerializeField] private string artifactId = "ART-001";
        [SerializeField] private AudioClip pickupClip;
        [SerializeField] private float rotateSpeed = 0f;
        [SerializeField] private Color artifactColor = Color.white;

        private SpriteRenderer spriteRenderer;

        public string ArtifactId => artifactId;
        public Color ArtifactColor => artifactColor;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            ApplyColor();
        }

        public void Configure(string id, AudioClip clip)
        {
            Configure(id, clip, artifactColor);
        }

        public void Configure(string id, AudioClip clip, Color color)
        {
            artifactId = id;
            pickupClip = clip;
            artifactColor = color;
            ApplyColor();
        }

        private void Update()
        {
            transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var cargo = other.GetComponentInParent<PlayerCargo>();
            if (cargo == null || !cargo.TryCollect(artifactId)) return;
            if (pickupClip != null) AudioSource.PlayClipAtPoint(pickupClip, transform.position, 0.8f);
            other.GetComponentInParent<PlayerVisualEffects>()?.PlayPickupBurst(transform.position, artifactColor);
            gameObject.SetActive(false);
        }

        private void ApplyColor()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white; // Matching color lives on the numbered beacon, preserving the illustration.
            }
        }
    }
}
