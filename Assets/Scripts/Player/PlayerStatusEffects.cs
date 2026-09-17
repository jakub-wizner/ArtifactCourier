using System;
using System.Collections;
using UnityEngine;

namespace ArtifactCourier.Player
{
    [RequireComponent(typeof(CarController))]
    public sealed class PlayerStatusEffects : MonoBehaviour
    {
        private CarController car;
        private Coroutine activeRoutine;
        private float currentMultiplier = 1f;

        public event Action<float, float> SpeedEffectChanged;
        public float CurrentMultiplier => currentMultiplier;

        private void Awake() => car = GetComponent<CarController>();

        public void ApplyBoost(float multiplier, float duration) => ApplyTimedMultiplier(Mathf.Max(1f, multiplier), duration);
        public void ApplySlow(float multiplier, float duration) => ApplyTimedMultiplier(Mathf.Clamp(multiplier, 0.2f, 0.95f), duration);

        private void ApplyTimedMultiplier(float multiplier, float duration)
        {
            if (activeRoutine != null) StopCoroutine(activeRoutine);
            activeRoutine = StartCoroutine(MultiplierRoutine(multiplier, Mathf.Max(0.1f, duration)));
        }

        private IEnumerator MultiplierRoutine(float multiplier, float duration)
        {
            currentMultiplier = multiplier;
            car.SetExternalSpeedMultiplier(multiplier);
            SpeedEffectChanged?.Invoke(multiplier, duration);
            yield return new WaitForSeconds(duration);
            currentMultiplier = 1f;
            car.SetExternalSpeedMultiplier(1f);
            SpeedEffectChanged?.Invoke(1f, 0f);
            activeRoutine = null;
        }
    }
}
