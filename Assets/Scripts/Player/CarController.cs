using System;
using UnityEngine;

namespace ArtifactCourier.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class CarController : MonoBehaviour
    {
        [Header("Drive")]
        [SerializeField] private float acceleration = 18f;
        [SerializeField] private float reverseAcceleration = 11f;
        [SerializeField] private float maxSpeed = 13f;
        [SerializeField] private float turnSpeed = 170f;
        [SerializeField, Range(0f, 1f)] private float lateralGrip = 0.86f;
        [SerializeField] private float rollingDrag = 0.55f;

        private Rigidbody2D body;
        private float throttleInput;
        private float steeringInput;
        private float externalSpeedMultiplier = 1f;
        private bool controlsEnabled = true;
        private bool handbrake;
        private bool nitroLocked;
        private float nitro = 1f;
        private float kitNitroDuration=1f;
        public void SetKitNitroDuration(float multiplier){kitNitroDuration=Mathf.Max(1f,multiplier);}
        private float rechargeDelay;
        public bool IsBoosting { get; private set; }
        public bool IsDrifting => handbrake && Speed > 4f && Mathf.Abs(steeringInput) > 0.15f;
        public float Nitro => nitro;
        public bool ControlsEnabled => controlsEnabled;

        public event Action<float> SpeedChanged;
        public float Speed => body == null ? 0f : body.linearVelocity.magnitude;
        public float MaxSpeed => maxSpeed * externalSpeedMultiplier * (IsBoosting ? 1.38f : 1f);

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
        }

        private void Update()
        {
            if (!controlsEnabled || Time.timeScale == 0f)
            {
                throttleInput = 0f;
                steeringInput = 0f;
                handbrake = false;
                IsBoosting = false;
                return;
            }

            throttleInput = ReadThrottle();
            steeringInput = ReadSteering();
            handbrake = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool boostHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (!boostHeld && nitro >= 0.2f) nitroLocked = false;
            IsBoosting = boostHeld && !nitroLocked && nitro > 0f && throttleInput > 0f && !handbrake;
            if (IsBoosting)
            {
                nitro = Mathf.Max(0f, nitro - Time.deltaTime / (2.8f*kitNitroDuration));
                rechargeDelay = 1.25f;
                if (nitro <= 0f) { nitroLocked = true; IsBoosting = false; }
            }
            else
            {
                rechargeDelay -= Time.deltaTime;
                if (rechargeDelay <= 0f) nitro = Mathf.Min(1f, nitro + Time.deltaTime / 7f);
            }
            SpeedChanged?.Invoke(Speed);
        }

        private void FixedUpdate()
        {
            ApplyEngineForce();
            ReduceSideSlip();
            ApplySteering();
            ApplyRollingDrag();
            ClampSpeed();
        }

        private static float ReadThrottle()
        {
            float value = 0f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) value += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) value -= 1f;
            return Mathf.Clamp(value, -1f, 1f);
        }

        private static float ReadSteering()
        {
            float value = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) value += 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) value -= 1f;
            return Mathf.Clamp(value, -1f, 1f);
        }

        private void ApplyEngineForce()
        {
            float forwardSpeed = Vector2.Dot(body.linearVelocity, transform.up);
            float allowedSpeed = MaxSpeed;
            if (throttleInput > 0f && forwardSpeed >= allowedSpeed) return;
            if (throttleInput < 0f && forwardSpeed <= -allowedSpeed * 0.45f) return;

            float force = throttleInput >= 0f ? acceleration : reverseAcceleration;
            body.AddForce((Vector2)transform.up * (throttleInput * force * (IsBoosting ? 1.8f : 1f)), ForceMode2D.Force);
        }

        private void ReduceSideSlip()
        {
            Vector2 right = transform.right;
            Vector2 lateralVelocity = right * Vector2.Dot(body.linearVelocity, right);
            body.linearVelocity -= lateralVelocity * (handbrake ? 0.075f : lateralGrip);
        }

        private void ApplySteering()
        {
            if (body.linearVelocity.sqrMagnitude < 0.08f) return;
            float direction = Vector2.Dot(body.linearVelocity, transform.up) >= 0f ? 1f : -1f;
            float speedFactor = Mathf.Clamp01(body.linearVelocity.magnitude / 2f);
            float highSpeedControl = Mathf.Lerp(1f, 0.72f, Mathf.InverseLerp(7f, 19f, Speed));
            float rotation = steeringInput * highSpeedControl * turnSpeed * (handbrake ? 1.2f : 1f) * speedFactor * direction * Time.fixedDeltaTime;
            body.MoveRotation(body.rotation + rotation);
        }

        private void ApplyRollingDrag()
        {
            float factor = Mathf.Clamp01(1f - (handbrake ? 1.4f : rollingDrag) * Time.fixedDeltaTime);
            body.linearVelocity *= factor;
        }

        private void ClampSpeed()
        {
            float limit = MaxSpeed;
            if (body.linearVelocity.magnitude > limit)
                body.linearVelocity = body.linearVelocity.normalized * Mathf.MoveTowards(Speed, limit, 20f * Time.fixedDeltaTime);
        }

        public void ConfigureClass(float speed, float thrust, float steering)
        { maxSpeed=speed; acceleration=thrust; turnSpeed=steering; }

        public void SetExternalSpeedMultiplier(float multiplier) => externalSpeedMultiplier = Mathf.Clamp(multiplier, 0.2f, 3f);
        public void SetControlsEnabled(bool enabled) => controlsEnabled = enabled;

        public void RefillNitro(float amount) => nitro = Mathf.Clamp01(nitro + amount);

        public void Knockback(Vector2 impulse)
        {
            body.AddForce(impulse, ForceMode2D.Impulse);
        }
    }
}
