using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArtifactCourier.Core
{
    public sealed class LevelController : MonoBehaviour
    {
        public static LevelController Instance { get; private set; }

        [SerializeField] private int levelIndex;
        [SerializeField] private string cityName = "City";
        [SerializeField] private int requiredDeliveries = 3;
        [SerializeField] private bool bossRequired;
        [SerializeField] private int totalEnemies;

        private readonly HashSet<string> deliveredIds = new();
        private int enemiesDefeated;
        private bool bossDefeated;
        private bool completed;

        public event Action ProgressChanged;
        public event Action LevelCompleted;

        public int LevelIndex => levelIndex;
        public string CityName => cityName;
        public int RequiredDeliveries => requiredDeliveries;
        public int DeliveredCount => deliveredIds.Count;
        public int EnemiesDefeated => enemiesDefeated;
        public int TotalEnemies => totalEnemies;
        public int EnemiesRemaining => Mathf.Max(0, totalEnemies - enemiesDefeated);
        public bool BossRequired => bossRequired;
        public bool BossDefeated => bossDefeated;
        public bool IsComplete => completed;

        private void Awake() => Instance = this;

        private void Start()
        {
            GameSession.Instance.EnterLevel(levelIndex);
            ProgressChanged?.Invoke();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Configure(int index, string city, int deliveries, bool requiresBoss, int enemyCount)
        {
            levelIndex = index;
            cityName = city;
            requiredDeliveries = deliveries;
            bossRequired = requiresBoss;
            totalEnemies = Mathf.Max(0, enemyCount);
        }

        public bool MarkDelivered(string artifactId)
        {
            if (ArtifactCourier.UI.RunResultScreen.IsOpen) return false;
            if (string.IsNullOrWhiteSpace(artifactId) || !deliveredIds.Add(artifactId)) return false;
            ProgressChanged?.Invoke();
            EvaluateCompletion();
            return true;
        }

        public void NotifyEnemyDefeated(bool wasBoss)
        {
            enemiesDefeated = Mathf.Min(totalEnemies, enemiesDefeated + 1);
            if (wasBoss) bossDefeated = true;
            ProgressChanged?.Invoke();
            EvaluateCompletion();
        }

        private void EvaluateCompletion()
        {
            if (completed || ArtifactCourier.UI.RunResultScreen.IsOpen) return;
            bool deliveriesDone = deliveredIds.Count >= requiredDeliveries;
            bool bossDone = !bossRequired || bossDefeated;
            if (!deliveriesDone || !bossDone) return;

            completed = true;
            GameSession.Instance.CompleteLevel(levelIndex, deliveredIds.Count, enemiesDefeated);
            LevelCompleted?.Invoke();
        }
    }
}
