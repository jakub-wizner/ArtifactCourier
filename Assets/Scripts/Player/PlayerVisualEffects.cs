using System.Collections;
using ArtifactCourier.Effects;
using UnityEngine;

namespace ArtifactCourier.Player
{
    public sealed class PlayerVisualEffects : MonoBehaviour
    {
        [SerializeField] private Sprite[] attackFrames;
        [SerializeField] private Sprite[] pickupFrames;
        [SerializeField] private Sprite[] chargeFrames;
        [SerializeField] private Sprite[] boostFrames;

        private Coroutine chargeRoutine;
        private Coroutine boostRoutine;

        public void Configure(Sprite[] attack, Sprite[] pickup, Sprite[] charge, Sprite[] boost)
        {
            attackFrames = attack;
            pickupFrames = pickup;
            chargeFrames = charge;
            boostFrames = boost;
        }

        public void PlayAttackPulse()
        {
            TransientSpriteAnimation.Spawn(attackFrames, transform.position, 0.05f, 30, Vector3.one * 2.4f, Color.white);
        }

        public void PlayPickupBurst(Vector3 position, Color tint)
        {
            TransientSpriteAnimation.Spawn(pickupFrames, position, 0.045f, 28, Vector3.one * 1.3f, tint);
        }

        public void PlayBonusBurst(Vector3 position, Color tint, string styleKey)
        {
            Sprite[] frames = pickupFrames;
            float scale = 1.45f;
            float frameTime = 0.045f;

            switch (styleKey)
            {
                case "nitro":
                    frames = boostFrames;
                    scale = 1.75f;
                    frameTime = 0.04f;
                    break;
                case "shield":
                    frames = chargeFrames;
                    scale = 1.65f;
                    break;
                case "attack":
                    frames = attackFrames;
                    scale = 1.8f;
                    break;
                case "holy":
                    frames = chargeFrames;
                    scale = 1.95f;
                    frameTime = 0.05f;
                    break;
            }

            TransientSpriteAnimation.Spawn(frames, position, frameTime, 30, Vector3.one * scale, tint);
        }

        public void StartChargingEffect()
        {
            if (chargeRoutine == null)
            {
                chargeRoutine = StartCoroutine(ChargeRoutine());
            }
        }

        public void StopChargingEffect()
        {
            if (chargeRoutine != null)
            {
                StopCoroutine(chargeRoutine);
                chargeRoutine = null;
            }
        }

        public void StartBoostTrail(float duration)
        {
            if (boostRoutine != null)
            {
                StopCoroutine(boostRoutine);
            }
            boostRoutine = StartCoroutine(BoostRoutine(duration));
        }

        private IEnumerator ChargeRoutine()
        {
            while (true)
            {
                Vector3 jitter = new(Random.Range(-0.2f, 0.2f), Random.Range(-0.25f, 0.25f), 0f);
                TransientSpriteAnimation.Spawn(chargeFrames, transform.position + jitter, 0.04f, 27, Vector3.one * 1.4f, Color.white);
                yield return new WaitForSeconds(0.10f);
            }
        }

        private IEnumerator BoostRoutine(float duration)
        {
            float endTime = Time.time + Mathf.Max(0.1f, duration);
            while (Time.time < endTime)
            {
                Vector3 spawn = transform.position - transform.up * 0.55f + (Vector3)(Random.insideUnitCircle * 0.18f);
                TransientSpriteAnimation.Spawn(boostFrames, spawn, 0.04f, 12, Vector3.one * 1.3f, Color.white, 0f, false, transform.eulerAngles.z + 180f);
                yield return new WaitForSeconds(0.11f);
            }
            boostRoutine = null;
        }
    }
}
