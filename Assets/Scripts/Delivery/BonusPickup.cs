using ArtifactCourier.Player;
using UnityEngine;

namespace ArtifactCourier.Delivery
{
    public enum BonusKind
    {
        RepairKit,
        Shield,
        Nitro,
        CargoExpansion,
        AttackOverdrive,
        Invulnerability,
        CooldownChip,
        StyleCoin
    }

    [RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
    public sealed class BonusPickup : MonoBehaviour
    {
        [SerializeField] private BonusKind kind;
        [SerializeField] private float amount = 25f;
        [SerializeField] private float duration = 8f;
        [SerializeField] private AudioClip pickupClip;
        [SerializeField] private float spinSpeed = 0f;
        private Vector3 baseScale;
        private bool collected;
        public BonusKind Kind => kind;

        private void Awake()
        {
            Collider2D collider = GetComponent<Collider2D>();
            collider.isTrigger = true;
            baseScale = transform.localScale;
        }

        private void Update()
        {
            transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
            float pulse = 1f + Mathf.Sin(Time.time * 2.8f) * 0.025f;
            transform.localScale = baseScale * pulse;
        }

        public void Configure(BonusKind bonusKind, float bonusAmount, float seconds, AudioClip clip)
        {
            kind = bonusKind;
            amount = bonusAmount;
            duration = seconds;
            pickupClip = clip;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected) return;
            CarController car = other.GetComponentInParent<CarController>();
            if (car == null) return;
            collected = true;

            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
            PlayerPulseAttack attacks = other.GetComponentInParent<PlayerPulseAttack>();
            PlayerVisualEffects vfx = other.GetComponentInParent<PlayerVisualEffects>();

            switch (kind)
            {
                case BonusKind.StyleCoin:
                    ArtifactCourier.Core.GameSession.Instance.ClaimCoin(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name+":"+gameObject.name);
                    other.GetComponentInParent<ArtifactCourier.Driving.DrivingStyle>()?.CollectCoin();
                    car.RefillNitro(0.06f);
                    break;
                case BonusKind.RepairKit:
                    health?.Heal(amount);
                    break;
                case BonusKind.Shield:
                    health?.AddShield(amount);
                    break;
                case BonusKind.Nitro:
                    other.GetComponentInParent<PlayerStatusEffects>()?.ApplyBoost(Mathf.Max(1.05f, amount), duration);
                    vfx?.StartBoostTrail(duration);
                    break;
                case BonusKind.CargoExpansion:
                    other.GetComponentInParent<PlayerCargo>()?.IncreaseCapacity(Mathf.RoundToInt(amount));
                    break;
                case BonusKind.AttackOverdrive:
                    attacks?.ApplyAttackOverdrive(Mathf.Max(1.1f, amount), duration);
                    break;
                case BonusKind.Invulnerability:
                    health?.ApplyInvulnerability(duration);
                    break;
                case BonusKind.CooldownChip:
                    attacks?.ApplyCooldownBoost(Mathf.Clamp(amount, 0.4f, 0.95f), duration);
                    break;
            }

            vfx?.PlayBonusBurst(transform.position, GetBonusColor(), GetBonusStyleKey());
            if (pickupClip != null)
            {
                AudioSource.PlayClipAtPoint(pickupClip, transform.position, 0.75f);
            }
            GetComponent<Collider2D>().enabled = false;
            Destroy(gameObject);
        }

        private string GetBonusStyleKey()
        {
            return kind switch
            {
                BonusKind.RepairKit => "repair",
                BonusKind.Shield => "shield",
                BonusKind.Nitro => "nitro",
                BonusKind.CargoExpansion => "cargo",
                BonusKind.AttackOverdrive => "attack",
                BonusKind.Invulnerability => "holy",
                BonusKind.CooldownChip => "attack",
                _ => "repair"
            };
        }

        private Color GetBonusColor()
        {
            return kind switch
            {
                BonusKind.RepairKit => new Color(0.35f, 1f, 0.45f, 1f),
                BonusKind.Shield => new Color(0.25f, 0.65f, 1f, 1f),
                BonusKind.Nitro => new Color(0.20f, 0.85f, 1f, 1f),
                BonusKind.CargoExpansion => new Color(1f, 0.78f, 0.20f, 1f),
                BonusKind.AttackOverdrive => new Color(1f, 0.35f, 0.22f, 1f),
                BonusKind.Invulnerability => new Color(0.95f, 0.88f, 0.38f, 1f),
                BonusKind.CooldownChip => new Color(0.70f, 0.42f, 1f, 1f),
                _ => Color.white
            };
        }
    }
}
