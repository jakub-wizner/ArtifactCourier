using System;
using System.Collections;
using ArtifactCourier.Core;
using ArtifactCourier.Effects;
using ArtifactCourier.Enemies;
using ArtifactCourier.Traffic;
using UnityEngine;

namespace ArtifactCourier.Player
{
    public sealed class PlayerPulseAttack : MonoBehaviour
    {
        [Header("Basic Pulse - Space")]
        [SerializeField] private float radius = 2.8f;
        [SerializeField] private int damage = 2;
        [SerializeField] private float cooldown = 1.1f;
        [SerializeField] private AudioClip pulseClip;

        [Header("Level 3 EMP Shockwave - Q")]
        [SerializeField] private bool empUnlocked;
        [SerializeField] private float empRadius = 5.4f;
        [SerializeField] private int empDamage = 5;
        [SerializeField] private float empCooldown = 5.3f;
        [SerializeField] private float empDisruptionSeconds = 2.2f;
        [SerializeField] private Sprite[] empFrames;

        [Header("Level 5 Courier Bolt - E")]
        [SerializeField] private bool rangedUnlocked;
        [SerializeField] private int rangedDamage = 12;
        [SerializeField] private float rangedCooldown = 1.75f;
        [SerializeField] private float projectileSpeed = 16f;
        [SerializeField] private int rangedPenetrations = 2;
        [SerializeField] private Sprite projectileSprite;
        [SerializeField] private Sprite[] rangedMuzzleFrames;
        [SerializeField] private Sprite[] rangedImpactFrames;

        private float nextReadyTime;
        private float nextEmpTime;
        private float nextRangedTime;
        private float damageMultiplier = 1f;
        private float cooldownMultiplier = 1f;
        private Coroutine damageBoostRoutine;
        private Coroutine cooldownBoostRoutine;
        private PlayerVisualEffects visualEffects;

        public event Action<float> CooldownStarted;
        public bool Ready => Time.time >= nextReadyTime;
        public bool EmpUnlocked => empUnlocked;
        public bool RangedUnlocked => rangedUnlocked;
        public bool EmpReady => empUnlocked && Time.time >= nextEmpTime;
        public bool RangedReady => rangedUnlocked && Time.time >= nextRangedTime;
        public float Cooldown => cooldown;

        private void Awake()
        {
            visualEffects = GetComponent<PlayerVisualEffects>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space)) TryAttack();
            if (empUnlocked && Input.GetKeyDown(KeyCode.Q)) TryEmp();
            if (rangedUnlocked && Input.GetKeyDown(KeyCode.E)) TryRangedAttack();
        }

        public void Configure(AudioClip clip, int levelIndex, Sprite projectile, Sprite[] empAnimation, Sprite[] rangedMuzzle, Sprite[] rangedImpact)
        {
            pulseClip = clip;
            damage = CombatBalance.PulseDamage(levelIndex);
            radius = CombatBalance.PulseRadius(levelIndex);
            cooldown = CombatBalance.PulseCooldown(levelIndex);

            empUnlocked = levelIndex >= 2;
            empDamage = CombatBalance.EmpDamage(levelIndex);
            empRadius = CombatBalance.EmpRadius(levelIndex);
            empCooldown = CombatBalance.EmpCooldown(levelIndex);
            empDisruptionSeconds = 2.0f + levelIndex * 0.2f;
            empFrames = empAnimation;

            rangedUnlocked = levelIndex >= 4;
            rangedDamage = CombatBalance.RangedDamage(levelIndex);
            rangedCooldown = CombatBalance.RangedCooldown(levelIndex);
            projectileSpeed = 17f;
            rangedPenetrations = 2;
            projectileSprite = projectile;
            rangedMuzzleFrames = rangedMuzzle;
            rangedImpactFrames = rangedImpact;
        }

        public void ConfigureClass(int kind, int rank)
        { cooldown = kind==0?0.42f:kind==1?1.6f:0.85f; cooldown *= rank>0?0.85f:1f; }

        public string GetAbilityStatus()
        {
            string pulseState = Ready ? "SPACE ATTACK" : "SPACE …";
            if (!empUnlocked) return pulseState;
            string empState = EmpReady ? "Q SPECIAL" : "Q …";
            if (!rangedUnlocked) return $"{pulseState}   {empState}";
            string rangedState = RangedReady ? "E FINISHER" : "E …";
            return $"{pulseState}   {empState}   {rangedState}";
        }

        public void TryAttack()
        {
            if (!Ready || Time.timeScale <= 0f) return;
            float actualCooldown = cooldown * cooldownMultiplier;
            nextReadyTime = Time.time + actualCooldown;
            CooldownStarted?.Invoke(actualCooldown);
            if (pulseClip != null) AudioSource.PlayClipAtPoint(pulseClip, transform.position, 0.85f);
            var vehicle = GetComponent<VehicleLoadout>();
            if (vehicle != null) { vehicle.BasicAttack(); return; }
            visualEffects?.PlayAttackPulse();
            DamageVehiclesInRadius(radius, ScaledDamage(damage));
        }

        private void TryEmp()
        {
            if (!EmpReady || Time.timeScale <= 0f) return;
            nextEmpTime = Time.time + empCooldown * cooldownMultiplier;
            if (GetComponent<VehicleLoadout>()?.ClassSpecial(false) == true) return;
            TransientSpriteAnimation.Spawn(empFrames, transform.position, 0.05f, 31, Vector3.one * 4.1f, Color.white);
            int actualDamage = ScaledDamage(empDamage);

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, empRadius);
            foreach (Collider2D hit in hits)
            {
                EnemyVehicle enemy = hit.GetComponentInParent<EnemyVehicle>();
                if (enemy != null)
                {
                    enemy.TakeHit(actualDamage, transform.position);
                    enemy.ApplyDisruption(empDisruptionSeconds);
                    Rigidbody2D enemyBody = enemy.GetComponent<Rigidbody2D>();
                    if (enemyBody != null)
                    {
                        Vector2 away = ((Vector2)enemy.transform.position - (Vector2)transform.position).normalized;
                        enemyBody.AddForce(away * 3.2f, ForceMode2D.Impulse);
                    }
                    continue;
                }

                TrafficVehicle traffic = hit.GetComponentInParent<TrafficVehicle>();
                traffic?.TakeHit(Mathf.Max(1, actualDamage - 1), transform.position);
            }
        }

        private void TryRangedAttack()
        {
            if (!RangedReady || Time.timeScale <= 0f || projectileSprite == null) return;
            nextRangedTime = Time.time + rangedCooldown * cooldownMultiplier;
            if (GetComponent<VehicleLoadout>()?.ClassSpecial(true) == true) return;
            Vector2 direction = transform.up;
            Vector3 spawnPosition = transform.position + transform.up * 1.2f;
            TransientSpriteAnimation.Spawn(rangedMuzzleFrames, spawnPosition, 0.035f, 32, Vector3.one * 1.45f, Color.white);

            GameObject projectile = new("Player Courier Bolt");
            projectile.transform.position = spawnPosition;
            projectile.AddComponent<SpriteRenderer>();
            CircleCollider2D collider = projectile.AddComponent<CircleCollider2D>();
            collider.radius = 0.25f;
            projectile.AddComponent<PlayerProjectile>().Configure(direction, projectileSpeed, ScaledDamage(rangedDamage), 4.5f, projectileSprite, rangedImpactFrames, rangedPenetrations);
        }

        public void ApplyAttackOverdrive(float multiplier, float duration)
        {
            if (damageBoostRoutine != null) StopCoroutine(damageBoostRoutine);
            damageBoostRoutine = StartCoroutine(DamageBoostRoutine(Mathf.Max(1f, multiplier), Mathf.Max(0.2f, duration)));
        }

        public void ApplyCooldownBoost(float multiplier, float duration)
        {
            if (cooldownBoostRoutine != null) StopCoroutine(cooldownBoostRoutine);
            cooldownBoostRoutine = StartCoroutine(CooldownBoostRoutine(Mathf.Clamp(multiplier, 0.35f, 1f), Mathf.Max(0.2f, duration)));
        }

        private IEnumerator DamageBoostRoutine(float multiplier, float duration)
        {
            damageMultiplier = multiplier;
            yield return new WaitForSeconds(duration);
            damageMultiplier = 1f;
            damageBoostRoutine = null;
        }

        private IEnumerator CooldownBoostRoutine(float multiplier, float duration)
        {
            cooldownMultiplier = multiplier;
            yield return new WaitForSeconds(duration);
            cooldownMultiplier = 1f;
            cooldownBoostRoutine = null;
        }

        private int ScaledDamage(int baseDamage) => Mathf.Max(1, Mathf.RoundToInt(baseDamage * damageMultiplier));

        private void DamageVehiclesInRadius(float attackRadius, int amount)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRadius);
            foreach (Collider2D hit in hits)
            {
                EnemyVehicle enemy = hit.GetComponentInParent<EnemyVehicle>();
                if (enemy != null)
                {
                    enemy.TakeHit(amount, transform.position);
                    continue;
                }

                TrafficVehicle traffic = hit.GetComponentInParent<TrafficVehicle>();
                traffic?.TakeHit(amount, transform.position);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
