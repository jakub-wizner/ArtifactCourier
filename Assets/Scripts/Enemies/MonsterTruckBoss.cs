using ArtifactCourier.Effects;
using ArtifactCourier.Player;
using UnityEngine;

namespace ArtifactCourier.Enemies
{
    public sealed class MonsterTruckBoss : EnemyVehicle
    {
        protected override bool UsesCustomSteering => true;
        [SerializeField] private float shockwaveInterval = 5f;
        [SerializeField] private float shockwaveRange = 5.5f;
        [SerializeField] private float shotInterval = 1.8f;
        [SerializeField] private float shotRange = 10f;
        [SerializeField] private float projectileSpeed = 12f;
        [SerializeField] private float projectileDamage = 10f;
        [SerializeField] private AudioClip shockwaveClip;
        [SerializeField] private Sprite projectileSprite;
        [SerializeField] private Sprite[] muzzleFrames;
        [SerializeField] private Sprite[] impactFrames;
        private float nextShockwave;
        private float nextShot;
        private float nextArtillery=8f;

        public void ConfigureBoss(AudioClip clip, Sprite projectile, Sprite[] muzzleAnimation, Sprite[] projectileImpact)
        {
            shockwaveClip = clip;
            projectileDamage = Mathf.Max(8f, contactDamage * 0.65f);
            projectileSprite = projectile;
            muzzleFrames = muzzleAnimation;
            impactFrames = projectileImpact;
        }

        protected override void TickBehavior()
        {
            if (Target == null) return;
            float phaseSpeed = CurrentHealth <= maxHealth*.25f ? 1.3f : CurrentHealth <= maxHealth*.5f ? 1.15f : 1f;
            DriveToward(Target.position, phaseSpeed);

            // Level five: seismic slam. Final city adds a separate three-marker artillery attack.
            if (DistanceToPlayer < 12f && Time.time >= nextShockwave)
            {
                nextShockwave=Time.time+Mathf.Max(4.5f,shockwaveInterval+1f-Level*.1f);
                AttackWarning.Spawn(transform,transform.position,shockwaveRange,16f+Level*2,1.2f,new Color(1,.65f,.15f),.65f);
                PlayAttackAnimation(2f,new Color(1,.65f,.15f));
            }
            if(Level>=6 && DistanceToPlayer<18f && Time.time>=nextArtillery)
            {
                nextArtillery=Time.time+9f;
                Vector2 center=Target.position;Vector2 side=Target.right;
                for(int i=-1;i<=1;i++) AttackWarning.Spawn(transform,center+side*i*3.2f,2.2f,22f,1.4f+Mathf.Abs(i)*.35f,new Color(1,.2f,.3f));
            }

            if (DistanceToPlayer <= shotRange && Time.time >= nextShot)
            {
                nextShot = Time.time + shotInterval;
                PlayAttackAnimation(2.1f, new Color(1f, 0.3f, 0.15f, 1f));
                ShootAtPlayer();
            }
        }

        private void ShootAtPlayer()
        {
            if (Target == null || projectileSprite == null)
            {
                return;
            }

            Vector2 direction = ((Vector2)Target.position - (Vector2)transform.position).normalized;
            Vector3 spawnPosition = transform.position + (Vector3)(direction * 1.25f);
            TransientSpriteAnimation.Spawn(muzzleFrames, spawnPosition, 0.035f, 18, Vector3.one * 1.1f, Color.white);

            var projectileObject = new GameObject("Boss Projectile");
            projectileObject.transform.position = spawnPosition;
            var renderer = projectileObject.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 14;
            var collider = projectileObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.26f;
            var projectile = projectileObject.AddComponent<BossProjectile>();
            projectile.Configure(direction, projectileSpeed, projectileDamage, 3.5f, projectileSprite, impactFrames);
        }
    }
}
