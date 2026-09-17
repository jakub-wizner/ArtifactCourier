using ArtifactCourier.Delivery;
using UnityEngine;

namespace ArtifactCourier.Enemies
{
    public sealed class OilDropperCar : EnemyVehicle
    {
        [SerializeField] private float dropInterval = 3.2f;
        [SerializeField] private Sprite oilSprite;
        [SerializeField] private AudioClip slowClip;
        private float nextDrop;

        public void ConfigureOil(Sprite sprite, AudioClip clip)
        {
            oilSprite = sprite;
            slowClip = clip;
        }

        protected override void TickBehavior()
        {
            if (Time.time < nextDrop) return;
            nextDrop = Time.time + Mathf.Max(1.8f,dropInterval-Level*.18f);
            PlayAttackAnimation(1.45f, new Color(0.45f, 0.45f, 0.45f, 1f));
            var go = new GameObject("Oil Slick");
            go.transform.position = transform.position - transform.up * 1.1f;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = oilSprite;
            renderer.sortingOrder = 2;
            var collider = go.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = .8f + Level*.06f;
            go.AddComponent<OilSlick>().Configure(slowClip);
        }
    }
}
