using ArtifactCourier.Player;
using UnityEngine;

namespace ArtifactCourier.Delivery
{
    [RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
    public sealed class GasStation : MonoBehaviour
    {
        [SerializeField] private float healPerSecond = 16f;
        [SerializeField] private float chargeRange = 2.3f;
        [SerializeField] private AudioClip healClip;
        [SerializeField] private Color readyTint = Color.white;
        [SerializeField] private Color chargingTint = new(0.75f, 1f, 0.8f, 1f);

        private SpriteRenderer spriteRenderer;
        private PlayerHealth nearbyPlayerHealth;
        private PlayerVisualEffects nearbyPlayerVfx;
        private AudioSource chargingAudio;
        private TextMesh promptLabel;
        private bool wasChargingLastFrame;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (GetComponent<Collider2D>() is Collider2D collider)
            {
                collider.isTrigger = true;
            }

            chargingAudio = gameObject.AddComponent<AudioSource>();
            chargingAudio.clip = healClip;
            chargingAudio.loop = true;
            chargingAudio.playOnAwake = false;
            chargingAudio.volume = 0.35f;
            chargingAudio.spatialBlend = 0.35f;
            EnsurePromptLabel();
            UpdatePrompt(false);
        }

        private void Update()
        {
            bool inRange = nearbyPlayerHealth != null && Vector2.Distance(transform.position, nearbyPlayerHealth.transform.position) <= chargeRange;
            bool canCharge = inRange && nearbyPlayerHealth.Current < nearbyPlayerHealth.Max - 0.01f;
            UpdatePrompt(canCharge);

            bool charging = false;
            if (canCharge && Input.GetKey(KeyCode.C))
            {
                float heal = healPerSecond * Time.deltaTime;
                nearbyPlayerHealth.Heal(heal);
                nearbyPlayerVfx?.StartChargingEffect();
                charging = true;
                if (!chargingAudio.isPlaying && healClip != null)
                {
                    chargingAudio.Play();
                }
            }

            if (!charging)
            {
                nearbyPlayerVfx?.StopChargingEffect();
                if (chargingAudio.isPlaying)
                {
                    chargingAudio.Stop();
                }
            }

            spriteRenderer.color = charging ? chargingTint : readyTint;
            wasChargingLastFrame = charging;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            nearbyPlayerHealth = other.GetComponentInParent<PlayerHealth>();
            nearbyPlayerVfx = other.GetComponentInParent<PlayerVisualEffects>();
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            var health = other.GetComponentInParent<PlayerHealth>();
            if (health != null)
            {
                nearbyPlayerHealth = health;
                nearbyPlayerVfx = other.GetComponentInParent<PlayerVisualEffects>();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerHealth>() == nearbyPlayerHealth)
            {
                nearbyPlayerVfx?.StopChargingEffect();
                nearbyPlayerHealth = null;
                nearbyPlayerVfx = null;
                if (chargingAudio.isPlaying)
                {
                    chargingAudio.Stop();
                }
                UpdatePrompt(false);
            }
        }

        public void Configure(float healRate, float activationRange, AudioClip clip)
        {
            healPerSecond = Mathf.Max(1f, healRate);
            chargeRange = Mathf.Max(0.5f, activationRange);
            healClip = clip;
            if (chargingAudio != null)
            {
                chargingAudio.clip = healClip;
            }
        }

        private void EnsurePromptLabel()
        {
            if (promptLabel != null)
            {
                return;
            }

            var labelGo = new GameObject("Prompt Label");
            labelGo.transform.SetParent(transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            promptLabel = labelGo.AddComponent<TextMesh>();
            promptLabel.text = string.Empty;
            promptLabel.anchor = TextAnchor.MiddleCenter;
            promptLabel.alignment = TextAlignment.Center;
            promptLabel.characterSize = 0.08f;
            promptLabel.fontSize = 42;
            promptLabel.color = new Color(0.85f, 1f, 0.9f, 1f);
        }

        private void UpdatePrompt(bool visible)
        {
            EnsurePromptLabel();
            promptLabel.text = visible ? "Hold 'C' for charging" : string.Empty;
        }
    }
}
