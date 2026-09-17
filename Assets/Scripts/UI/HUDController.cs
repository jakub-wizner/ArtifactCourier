using ArtifactCourier.Core;
using ArtifactCourier.Player;
using UnityEngine;
using UnityEngine.UI;

namespace ArtifactCourier.UI
{
    public sealed class HUDController : MonoBehaviour
    {
        [SerializeField] private Text cityText;
        [SerializeField] private Text speedText;
        [SerializeField] private Text healthText;
        [SerializeField] private Text enemiesText;
        [SerializeField] private Text cargoText;
        [SerializeField] private Text deliveryText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text pulseText;
        [SerializeField] private Text objectiveText;
        [SerializeField] private RectTransform healthBarFill;
        [SerializeField] private RectTransform shieldBarFill;
        [SerializeField] private RectTransform enemyBarFill;

        private CarController car;
        private PlayerHealth health;
        private PlayerCargo cargo;
        private PlayerStatusEffects effects;
        private PlayerPulseAttack pulse;
        private LevelController level;

        public void Configure(Text city, Text speed, Text hp, Text enemies, Text cargoValue, Text deliveries, Text status, Text pulseValue, Text objective,
            RectTransform hpFill, RectTransform shieldFill, RectTransform enemyFill)
        {
            cityText = city;
            speedText = speed;
            healthText = hp;
            enemiesText = enemies;
            cargoText = cargoValue;
            deliveryText = deliveries;
            statusText = status;
            pulseText = pulseValue;
            objectiveText = objective;
            healthBarFill = hpFill;
            shieldBarFill = shieldFill;
            enemyBarFill = enemyFill;
        }

        private void Start()
        {
            car = FindFirstObjectByType<CarController>();
            health = FindFirstObjectByType<PlayerHealth>();
            cargo = FindFirstObjectByType<PlayerCargo>();
            effects = FindFirstObjectByType<PlayerStatusEffects>();
            pulse = FindFirstObjectByType<PlayerPulseAttack>();
            level = LevelController.Instance;
        }

        private void Update()
        {
            if (level != null)
            {
                if (cityText != null) cityText.text = $"LEVEL {level.LevelIndex + 1}  /  {level.CityName.ToUpperInvariant()}";
                if (deliveryText != null) deliveryText.text = $"DELIVERIES  {level.DeliveredCount}/{level.RequiredDeliveries}";
                if (enemiesText != null) enemiesText.text = $"HOSTILES  {level.EnemiesRemaining}/{level.TotalEnemies}";
                if (enemyBarFill != null)
                {
                    float ratio = level.TotalEnemies <= 0 ? 0f : level.EnemiesRemaining / (float)level.TotalEnemies;
                    SetBar(enemyBarFill, ratio);
                }
                if (objectiveText != null)
                {
                    string bossGoal = level.BossRequired && !level.BossDefeated ? "  +  DEFEAT CITY BOSS" : string.Empty;
                    objectiveText.text = $"OBJECTIVE  RETURN ALL ARTIFACTS{bossGoal}";
                }
            }

            if (speedText != null && car != null) speedText.text = $"SPEED  {car.Speed:0.0}";
            if (healthText != null && health != null)
            {
                healthText.text = health.Invulnerable
                    ? $"HP  {health.Current:0}/{health.Max:0}   INVULNERABLE"
                    : health.Shield > 0f
                        ? $"HP  {health.Current:0}/{health.Max:0}   SHIELD {health.Shield:0}"
                        : $"HP  {health.Current:0}/{health.Max:0}";
                SetBar(healthBarFill, health.Max <= 0f ? 0f : health.Current / health.Max);
                SetBar(shieldBarFill, Mathf.Clamp01(health.Shield / 140f));
            }
            if (cargoText != null && cargo != null) cargoText.text = $"CARGO  {cargo.Count}/{cargo.Capacity}";
            if (statusText != null && effects != null)
            {
                statusText.text = effects.CurrentMultiplier > 1.01f ? $"DRIVE BOOST  x{effects.CurrentMultiplier:0.00}"
                    : effects.CurrentMultiplier < 0.99f ? $"DRIVE SLOWED  x{effects.CurrentMultiplier:0.00}"
                    : "DRIVE  NORMAL";
            }
            if (pulseText != null && pulse != null) pulseText.text = pulse.GetAbilityStatus();
        }

        private static void SetBar(RectTransform fill, float ratio)
        {
            if (fill == null) return;
            Vector2 max = fill.anchorMax;
            max.x = Mathf.Clamp01(ratio);
            fill.anchorMax = max;
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
        }
    }
}
