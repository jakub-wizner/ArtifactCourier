using System.Collections.Generic;
using ArtifactCourier.Effects;
using ArtifactCourier.Enemies;
using ArtifactCourier.Traffic;
using UnityEngine;

namespace ArtifactCourier.Player
{
    [RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
    public sealed class PlayerProjectile : MonoBehaviour
    {
        private Vector2 direction;
        private float speed;
        private int damage;
        private float expiry;
        private int remainingPenetrations;
        private Sprite[] impactFrames;
        private readonly HashSet<int> hitObjects = new();

        public void Configure(Vector2 travelDirection, float projectileSpeed, int projectileDamage, float lifetime, Sprite sprite, Sprite[] impact, int penetrations = 0)
        {
            direction = travelDirection.sqrMagnitude < 0.01f ? Vector2.up : travelDirection.normalized;
            speed = projectileSpeed;
            damage = Mathf.Max(1, projectileDamage);
            expiry = Time.time + Mathf.Max(0.2f, lifetime);
            impactFrames = impact;
            remainingPenetrations = Mathf.Max(0, penetrations);

            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 18;
            transform.right = direction; // New illustrated bolts point along their local +X axis.

            Collider2D collider = GetComponent<Collider2D>();
            collider.isTrigger = true;
        }

        private void Update()
        {
            transform.position += (Vector3)(direction * speed * Time.deltaTime);
            if (Time.time >= expiry) Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            EnemyVehicle enemy = other.GetComponentInParent<EnemyVehicle>();
            if (enemy != null)
            {
                if (!RegisterHit(enemy.gameObject)) return;
                enemy.TakeHit(damage, transform.position - (Vector3)direction);
                VehicleImpact();
                return;
            }

            TrafficVehicle traffic = other.GetComponentInParent<TrafficVehicle>();
            if (traffic != null)
            {
                if (!RegisterHit(traffic.gameObject)) return;
                traffic.TakeHit(damage, transform.position - (Vector3)direction);
                VehicleImpact();
                return;
            }

            if (!other.isTrigger && other.GetComponentInParent<CarController>() == null)
            {
                ImpactAndDestroy();
            }
        }

        private bool RegisterHit(GameObject target)
        {
            int id = target.GetInstanceID();
            if (hitObjects.Contains(id)) return false;
            hitObjects.Add(id);
            return true;
        }

        private void VehicleImpact()
        {
            TransientSpriteAnimation.Spawn(impactFrames, transform.position, 0.035f, 20, Vector3.one * 1.15f, Color.white);
            if (remainingPenetrations > 0)
            {
                remainingPenetrations--;
                return;
            }
            Destroy(gameObject);
        }

        private void ImpactAndDestroy()
        {
            TransientSpriteAnimation.Spawn(impactFrames, transform.position, 0.04f, 20, Vector3.one * 1.1f, Color.white);
            Destroy(gameObject);
        }
    }
}
