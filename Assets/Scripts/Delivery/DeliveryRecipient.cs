using ArtifactCourier.Core;
using ArtifactCourier.Player;
using UnityEngine;

namespace ArtifactCourier.Delivery
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class DeliveryRecipient : MonoBehaviour
    {
        [SerializeField] private string expectedArtifactId = "ART-001";
        [SerializeField] private string recipientName = "Recipient";
        [SerializeField] private Color recipientColor = Color.white;
        [SerializeField] private AudioClip deliveryClip;
        [SerializeField] private GameObject deliveredMarker;
        private bool completed;

        public bool Completed => completed;
        public string RecipientName => recipientName;
        public Color RecipientColor => recipientColor;
        private SpriteRenderer spriteRenderer;
        private TextMesh label;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            EnsureLabel();
            ApplyVisuals();
        }

        public void Configure(string id, AudioClip clip, Color color, string displayName)
        {
            expectedArtifactId = id;
            deliveryClip = clip;
            recipientColor = color;
            recipientName = displayName;
            ApplyVisuals();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (completed) return;
            var cargo = other.GetComponentInParent<PlayerCargo>();
            if (cargo == null || !cargo.TryDeliver(expectedArtifactId)) return;

            if (LevelController.Instance == null || !LevelController.Instance.MarkDelivered(expectedArtifactId))
            {
                cargo.TryCollect(expectedArtifactId);
                return;
            }

            completed = true;
            if (deliveryClip != null) AudioSource.PlayClipAtPoint(deliveryClip, transform.position, 0.9f);
            if (deliveredMarker != null) deliveredMarker.SetActive(true);
            if (spriteRenderer != null) spriteRenderer.color = new Color(0.65f, 1f, 0.7f, 1f);
            if (label != null) { label.color = new Color(0.8f, 1f, 0.8f, 1f); label.text = recipientName + "  ✓"; }
            var ring = transform.Find("Mission Color Ring");
            if (ring != null) ring.GetComponent<SpriteRenderer>().color = new Color(0.3f,1f,0.55f);
        }

        private void EnsureLabel()
        {
            if (label != null)
            {
                return;
            }

            var labelGo = new GameObject("Recipient Label");
            labelGo.transform.SetParent(transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 1.6f, 0f) / Mathf.Max(0.01f, transform.localScale.x);
            labelGo.transform.localScale = Vector3.one / Mathf.Max(0.01f, transform.localScale.x);
            label = labelGo.AddComponent<TextMesh>();
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.08f;
            label.fontSize = 40;
            labelGo.GetComponent<MeshRenderer>().sortingOrder = 21;
            label.color = Color.white;
        }

        private void ApplyVisuals()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }
            EnsureLabel();
            if (label != null)
            {
                label.text = recipientName;
                label.color = recipientColor;
            }
        }
    }
}
