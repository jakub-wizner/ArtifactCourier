using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArtifactCourier.Player
{
    public sealed class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        
        private float currentHealth;
        private float shield;
        private bool defeated;
        private bool invulnerable;
        private Coroutine invulnerabilityRoutine;

        public event Action<float, float> HealthChanged;
        public float Current => currentHealth;
        public float Max => maxHealth;
        public float Shield => shield;
        public bool Invulnerable => invulnerable;

        private void Awake() => currentHealth = maxHealth;
        private void Start() => HealthChanged?.Invoke(currentHealth, maxHealth);

        public void Configure(float maximumHealth)
        {
            maxHealth = Mathf.Max(20f, maximumHealth);
            currentHealth = maxHealth;
            HealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void TakeDamage(float amount)
        {
            if (defeated || invulnerable || amount <= 0f || Time.timeScale <= 0f ||
                (ArtifactCourier.Core.LevelController.Instance != null && ArtifactCourier.Core.LevelController.Instance.IsComplete)) return;

            float remaining = amount;
            if (shield > 0f)
            {
                float absorbed = Mathf.Min(shield, remaining);
                shield -= absorbed;
                remaining -= absorbed;
            }

            if (remaining > 0f)
            {
                currentHealth = Mathf.Max(0f, currentHealth - remaining);
            }

            HealthChanged?.Invoke(currentHealth, maxHealth);
            if (currentHealth <= 0f)
            {
                defeated = true;
                GetComponent<CarController>()?.SetControlsEnabled(false);
                ArtifactCourier.UI.RunResultScreen.Show(false);
            }
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || defeated) return;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            HealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void AddShield(float amount)
        {
            if (amount <= 0f || defeated) return;
            shield = Mathf.Min(140f, shield + amount);
            HealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void ApplyInvulnerability(float duration)
        {
            if (invulnerabilityRoutine != null) StopCoroutine(invulnerabilityRoutine);
            invulnerabilityRoutine = StartCoroutine(InvulnerabilityRoutine(Mathf.Max(0.2f, duration)));
        }

        private IEnumerator InvulnerabilityRoutine(float duration)
        {
            invulnerable = true;
            HealthChanged?.Invoke(currentHealth, maxHealth);
            yield return new WaitForSeconds(duration);
            invulnerable = false;
            invulnerabilityRoutine = null;
            HealthChanged?.Invoke(currentHealth, maxHealth);
        }


    }
}
