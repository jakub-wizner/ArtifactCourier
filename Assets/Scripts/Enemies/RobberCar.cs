using ArtifactCourier.Delivery;
using ArtifactCourier.Player;
using UnityEngine;

namespace ArtifactCourier.Enemies
{
    public sealed class RobberCar : EnemyVehicle
    {
        [SerializeField] private float stealCooldown = 2.5f;
        [SerializeField] private AudioClip stealClip;
        [SerializeField] private AudioClip pickupClip;
        [SerializeField] private Sprite artifactSprite;
        private float nextStealTime;
        private string stolenArtifactId;

        public bool HasStolenCargo => !string.IsNullOrEmpty(stolenArtifactId);
        protected override bool UsesCustomSteering => HasStolenCargo;

        public void ConfigureRobber(AudioClip clip, Sprite cargoSprite, AudioClip recoveredPickupClip)
        {
            stealClip = clip;
            artifactSprite = cargoSprite;
            pickupClip = recoveredPickupClip;
        }

        protected override void OnPlayerCollision(Collider2D playerCollider)
        {
            if (HasStolenCargo || Time.time < nextStealTime) return;
            var cargo = playerCollider.GetComponentInParent<PlayerCargo>();
            if (cargo == null || !cargo.TryStealRandom(out stolenArtifactId)) return;

            nextStealTime = Time.time + stealCooldown;
            PlayAttackAnimation(1.5f, new Color(1f, 0.45f, 0.55f, 1f));
            if (stealClip != null) AudioSource.PlayClipAtPoint(stealClip, transform.position, 0.9f);
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.color = new Color(1f, 0.6f, 0.6f, 1f);
        }

        protected override void TickBehavior()
        {
            if(HasStolenCargo && Target!=null)
            {
                Vector2 escape=(Vector2)transform.position+((Vector2)transform.position-(Vector2)Target.position).normalized*8f;
                DriveToward(escape,1.15f+Level*.02f);
            }
        }

        public override void TakeHit(int amount, Vector3 attackerPosition)
        {
            ReturnCargoToPlayer();
            base.TakeHit(amount, attackerPosition);
        }

        protected override void BeforeDeath()
        {
            if (!ReturnCargoToPlayer()) DropStolenCargo();
        }

        private bool ReturnCargoToPlayer()
        {
            if (!HasStolenCargo || Target == null) return !HasStolenCargo;
            var cargo = Target.GetComponent<PlayerCargo>();
            if (cargo == null || !cargo.TryCollect(stolenArtifactId)) return false;

            stolenArtifactId = null;
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.color = Color.white;
            return true;
        }

        private void DropStolenCargo()
        {
            if (!HasStolenCargo) return;
            var go = new GameObject($"Recovered {stolenArtifactId}");
            go.transform.position = transform.position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = artifactSprite;
            if (artifactSprite != null) go.transform.localScale = Vector3.one * (2.5f / Mathf.Max(artifactSprite.bounds.size.x,artifactSprite.bounds.size.y));
            renderer.sortingOrder = 8;
            var collider = go.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 1.35f / go.transform.localScale.x;
            go.AddComponent<ArtifactPickup>().Configure(stolenArtifactId, pickupClip);
            stolenArtifactId = null;
        }
    }
}
