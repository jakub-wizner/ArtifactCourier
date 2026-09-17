using ArtifactCourier.Core;
using ArtifactCourier.Player;
using ArtifactCourier.Traffic;
using UnityEngine;

namespace ArtifactCourier.Driving
{
    [RequireComponent(typeof(CarController))]
    public sealed class DrivingStyle : MonoBehaviour
    {
        public int Score { get; private set; }
        public int Best { get; private set; }
        public int Combo { get; private set; } = 1;
        public string Toast => Time.time < toastUntil ? toast : "";
        public string Challenge => $"{challengeNames[challenge]}  {Mathf.Min(progress, goals[challenge]):0}/{goals[challenge]:0}  •  {Mathf.CeilToInt(challengeTime)}s";
        private readonly string[] challengeNames = { "DRIFT SECONDS", "CLEAN PASSES", "DELIVER AN ARTIFACT" };
        private readonly float[] goals = { 4f, 2f, 1f };
        private CarController car;
        private Rigidbody2D body;
        private TrafficVehicle[] traffic;
        private float[] previousProjection;
        private float[] passCooldown;
        private LevelController level;
        private int deliveries;
        private int challenge;
        private float progress;
        private float challengeTime = 40f;
        private float comboTime;
        private float driftTime;
        private float collisionCooldown;
        private float toastUntil;
        private string toast;
        private string bestKey;
        private float boostFxTime;
        private PlayerVisualEffects effects;

        private void Start()
        {
            car = GetComponent<CarController>();
            body = GetComponent<Rigidbody2D>();
            effects = GetComponent<PlayerVisualEffects>();
            level = LevelController.Instance;
            traffic = FindObjectsByType<TrafficVehicle>(FindObjectsSortMode.None);
            previousProjection = new float[traffic.Length];
            passCooldown = new float[traffic.Length];
            bestKey = "Courier.StyleBest." + (level == null ? 0 : level.LevelIndex);
            Best = PlayerPrefs.GetInt(bestKey, 0);
            if (level != null)
            {
                deliveries = level.DeliveredCount;
                level.ProgressChanged += OnProgress;
                level.LevelCompleted += SaveBest;
            }
            Announce("SHIFT NITRO  •  CTRL DRIFT  •  CLEAN PASSES BUILD STREAKS");
        }

        private void Update()
        {
            if (Time.timeScale == 0f || !car.ControlsEnabled || (level != null && level.IsComplete)) return;
            float dt = Time.deltaTime;
            comboTime -= dt;
            if (comboTime <= 0f) Combo = 1;
            challengeTime -= dt;
            if (challengeTime <= 0f) NextChallenge();
            if (car.IsDrifting)
            {
                driftTime += dt;
                if (challenge == 0) progress += dt;
                if (driftTime >= 1f) { driftTime -= 1f; Award(25, "DRIFT"); }
            }
            else driftTime = 0f;
            CheckChallenge();
            if (car.IsBoosting && Time.time >= boostFxTime)
            {
                effects?.StartBoostTrail(0.25f);
                boostFxTime = Time.time + 0.28f;
            }
            Vector2 direction = body.linearVelocity.normalized;
            for (int i = 0; i < traffic.Length; i++)
            {
                if (traffic[i] == null) continue;
                Vector2 relative = traffic[i].transform.position - transform.position;
                float projection = Vector2.Dot(relative, direction);
                float lateral = Mathf.Abs(direction.x * relative.y - direction.y * relative.x);
                if (car.Speed > 6f && previousProjection[i] > 0f && projection <= 0f &&
                    lateral > 1.25f && lateral < 2.6f && relative.sqrMagnitude < 10f &&
                    Time.time > passCooldown[i] && Time.time > collisionCooldown)
                {
                    passCooldown[i] = Time.time + 12f;
                    Award(100, "CLEAN PASS");
                    car.RefillNitro(0.12f);
                    if (challenge == 1) progress++;
                    CheckChallenge();
                }
                previousProjection[i] = projection;
            }
        }

        private void OnProgress()
        {
            if (level.DeliveredCount <= deliveries) return;
            deliveries = level.DeliveredCount;
            Award(500, "DELIVERY");
            car.RefillNitro(0.4f);
            GetComponent<PlayerHealth>()?.AddShield(10f);
            if (challenge == 2) progress++;
            CheckChallenge();
        }

        private void CheckChallenge()
        {
            if (progress < goals[challenge]) return;
            Award(300, "CONTRACT COMPLETE");
            GameSession.Instance.EarnCoins(15);
            car.RefillNitro(0.5f);
            NextChallenge();
        }

        private void NextChallenge()
        {
            challenge = (challenge + 1) % goals.Length;
            if (challenge == 2 && level != null && level.DeliveredCount >= level.RequiredDeliveries) challenge = 0;
            progress = 0f;
            challengeTime = 40f;
        }

        public void CollectCoin() => Award(50, "COIN");

        private void Award(int points, string reason)
        {
            if (Time.timeScale <= 0f || !car.ControlsEnabled) return;
            Score += points * Combo;
            Best = Mathf.Max(Best, Score);
            Announce($"{reason}  +{points * Combo}");
            Combo = Mathf.Min(5, Combo + 1);
            comboTime = 8f;
        }

        private void Announce(string message) { toast = message; toastUntil = Time.time + 3f; }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            collisionCooldown = Time.time + 2f;
            if (collision.relativeVelocity.magnitude < 3f) return;
            Combo = 1;
            comboTime = 0f;
            Announce("STREAK RESET  •  FIND YOUR FLOW");
        }

        public void SaveBest()
        {
            if (string.IsNullOrEmpty(bestKey)) return;
            PlayerPrefs.SetInt(bestKey, Mathf.Max(PlayerPrefs.GetInt(bestKey, 0), Best));
            PlayerPrefs.Save();
        }

        private void OnDestroy()
        {
            if (level != null) { level.ProgressChanged -= OnProgress; level.LevelCompleted -= SaveBest; }
            SaveBest();
        }
    }
}
