using ArtifactCourier.Core;
using ArtifactCourier.Effects;
using ArtifactCourier.Player;
using UnityEngine;

namespace ArtifactCourier.Enemies
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class EnemyVehicle : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] protected float acceleration = 10f;
        [SerializeField] protected float maxSpeed = 7f;
        [SerializeField] protected float turnRate = 125f;
        [SerializeField, Range(0f, 1f)] protected float sideGrip = 0.75f;

        [Header("Combat")]
        [SerializeField] protected int maxHealth = 2;
        [SerializeField] protected float contactDamage = 10f;
        [SerializeField] protected float collisionCooldown = 0.7f;
        [SerializeField] protected bool boss;
        [SerializeField] protected AudioClip hitClip;
        [SerializeField] protected AudioClip defeatClip;
        [SerializeField] private Sprite[] destructionFrames;
        [SerializeField] private Sprite[] attackFrames;

        protected Rigidbody2D Body { get; private set; }
        protected Transform Target { get; private set; }
        protected int CurrentHealth { get; private set; }
        protected bool Defeated { get; private set; }
        private float nextCollisionDamageTime;
        private float disruptedUntil;

        protected virtual void Awake()
        {
            Body = GetComponent<Rigidbody2D>();
            Body.gravityScale = 0f;
            CurrentHealth = maxHealth;

            Collider2D vehicleCollider = GetComponent<Collider2D>();
            if (vehicleCollider != null)
            {
                PhysicsMaterial2D lowFriction = new("Enemy Low Friction")
                {
                    friction = 0f,
                    bounciness = 0.06f
                };
                vehicleCollider.sharedMaterial = lowFriction;
            }
        }

        protected virtual void Start()
        {
            var player = FindFirstObjectByType<CarController>();
            Target = player == null ? null : player.transform;
        }

        protected virtual void FixedUpdate()
        {
            if (Defeated || Target == null) return;
            if (!UsesCustomSteering || IsDisrupted) DriveToward(Target.position);
            if (!IsDisrupted) TickBehavior();
        }

        protected virtual bool UsesCustomSteering => false;
        protected virtual void TickBehavior() { }

        protected void DriveToward(Vector2 point, float speedMultiplier = 1f)
        {
            Vector2 toTarget = point - (Vector2)transform.position;
            if (toTarget.sqrMagnitude < 0.01f) return;

            float signed = Vector2.SignedAngle(transform.up, toTarget.normalized);
            float rotationStep = Mathf.Clamp(signed, -turnRate * Time.fixedDeltaTime, turnRate * Time.fixedDeltaTime);
            Body.MoveRotation(Body.rotation + rotationStep);

            float disruptionMultiplier = Time.time < disruptedUntil ? 0.42f : 1f;
            float limit = maxSpeed * Mathf.Max(0.25f, speedMultiplier) * disruptionMultiplier;
            if (Body.linearVelocity.magnitude < limit)
                Body.AddForce((Vector2)transform.up * acceleration, ForceMode2D.Force);

            Vector2 lateral = (Vector2)transform.right * Vector2.Dot(Body.linearVelocity, transform.right);
            Body.linearVelocity -= lateral * sideGrip;
            if (Body.linearVelocity.magnitude > limit)
                Body.linearVelocity = Body.linearVelocity.normalized * limit;
        }

        public bool IsDisrupted => Time.time < disruptedUntil;
        protected int Level => LevelController.Instance == null ? 0 : LevelController.Instance.LevelIndex;

        protected float DistanceToPlayer => Target == null ? float.PositiveInfinity : Vector2.Distance(transform.position, Target.position);

        protected virtual void OnCollisionEnter2D(Collision2D collision)
        {
            if (Defeated || Time.time < nextCollisionDamageTime) return;
            var health = collision.collider.GetComponentInParent<PlayerHealth>();
            if (health == null) return;

            nextCollisionDamageTime = Time.time + collisionCooldown;
            PlayAttackAnimation(boss ? 2.6f : 1.55f);
            health.TakeDamage(contactDamage);
            var car = collision.collider.GetComponentInParent<CarController>();
            if (car != null)
            {
                Vector2 away = ((Vector2)car.transform.position - (Vector2)transform.position).normalized;
                car.Knockback(away * 3.1f);
                Body.AddForce(-away * 2.2f, ForceMode2D.Impulse);
            }
            OnPlayerCollision(collision.collider);
        }

        protected virtual void OnPlayerCollision(Collider2D playerCollider) { }

        protected void PlayAttackAnimation(float scale = 1.6f, Color? tint = null)
        {
            TransientSpriteAnimation.Spawn(attackFrames, transform.position, 0.045f, 19, Vector3.one * scale, tint ?? Color.white);
        }


        public void ApplyDisruption(float seconds)
        {
            disruptedUntil = Mathf.Max(disruptedUntil, Time.time + Mathf.Max(0.1f, seconds));
            PlayAttackAnimation(1.75f, new Color(0.35f, 0.85f, 1f, 1f));
        }

        public virtual void TakeHit(int amount, Vector3 attackerPosition)
        {
            if (Defeated || amount <= 0) return;
            CurrentHealth -= amount;
            if (hitClip != null) AudioSource.PlayClipAtPoint(hitClip, transform.position, boss ? 1f : 0.7f);

            Vector2 away = ((Vector2)transform.position - (Vector2)attackerPosition).normalized;
            // Damage does not cancel driving momentum; explicit abilities own knockback.
            OnDamaged();

            if (CurrentHealth <= 0) Die();
        }

        protected virtual void OnDamaged() { }

        protected virtual void Die()
        {
            if (Defeated) return;
            Defeated = true;
            BeforeDeath();
            if (defeatClip != null) AudioSource.PlayClipAtPoint(defeatClip, transform.position, boss ? 1f : 0.75f);
            TransientSpriteAnimation.Spawn(destructionFrames, transform.position, 0.045f, 20, boss ? Vector3.one * 2f : Vector3.one * 1.35f, Color.white);
            if (LevelController.Instance != null) LevelController.Instance.NotifyEnemyDefeated(boss);
            Destroy(gameObject);
        }

        protected virtual void BeforeDeath() { }

        public void ConfigureBase(float moveSpeed, int health, float damage, bool isBoss, AudioClip hit, AudioClip defeat, Sprite[] destroyFrames = null, Sprite[] enemyAttackFrames = null)
        {
            maxSpeed = moveSpeed;
            maxHealth = Mathf.Max(1, health);
            CurrentHealth = maxHealth;

            Collider2D vehicleCollider = GetComponent<Collider2D>();
            if (vehicleCollider != null)
            {
                PhysicsMaterial2D lowFriction = new("Enemy Low Friction")
                {
                    friction = 0f,
                    bounciness = 0.06f
                };
                vehicleCollider.sharedMaterial = lowFriction;
            }
            contactDamage = Mathf.Max(0f, damage);
            boss = isBoss;
            hitClip = hit;
            defeatClip = defeat;
            destructionFrames = destroyFrames;
            attackFrames = enemyAttackFrames;
        }
    }
}
