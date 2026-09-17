using ArtifactCourier.Effects;
using ArtifactCourier.Player;
using UnityEngine;

namespace ArtifactCourier.Enemies
{
    [RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
    public sealed class BossProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 10f;
        [SerializeField] private float damage = 8f;
        [SerializeField] private float lifetime = 3.5f;
        [SerializeField] private Vector2 direction = Vector2.up;
        [SerializeField] private Sprite[] impactFrames;

        private float expiryTime;

        private void Start()
        {
            expiryTime = Time.time + lifetime;
        }

        private void Update()
        {
            transform.position += (Vector3)(direction.normalized * speed * Time.deltaTime);
            if (Time.time >= expiryTime)
            {
                Destroy(gameObject);
            }
        }

        public void Configure(Vector2 travelDirection, float projectileSpeed, float projectileDamage, float secondsToLive, Sprite sprite, Sprite[] impact)
        {
            direction = travelDirection.sqrMagnitude < 0.001f ? Vector2.up : travelDirection.normalized;
            speed = projectileSpeed;
            damage = projectileDamage;
            lifetime = secondsToLive;
            impactFrames = impact;

            var renderer = GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 14;
            transform.right = direction; // New illustrated bolts point along their local +X axis.

            if (GetComponent<Collider2D>() is Collider2D collider)
            {
                collider.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var playerHealth = other.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                var car = other.GetComponentInParent<CarController>();
                if (car != null)
                {
                    car.Knockback(direction * 1.8f);
                }
                Explode();
                return;
            }

            if (other.GetComponent<Collider2D>() != null && !other.isTrigger)
            {
                Explode();
            }
        }

        private void Explode()
        {
            TransientSpriteAnimation.Spawn(impactFrames, transform.position, 0.04f, 20, Vector3.one, Color.white);
            Destroy(gameObject);
        }
    }
}
