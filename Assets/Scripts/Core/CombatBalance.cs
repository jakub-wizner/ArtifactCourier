using UnityEngine;

namespace ArtifactCourier.Core
{
    public static class CombatBalance
    {
        public readonly struct EnemyTuning
        {
            public readonly int Health;
            public readonly float ContactDamage;
            public readonly float Speed;

            public EnemyTuning(int health, float contactDamage, float speed)
            {
                Health = health;
                ContactDamage = contactDamage;
                Speed = speed;
            }
        }

        private static readonly float[] PlayerHealthByLevel = { 100f, 112f, 128f, 146f, 168f };
        private static readonly int[] PulseDamageByLevel = { 2, 2, 3, 4, 5 };
        private static readonly float[] PulseRadiusByLevel = { 2.8f, 2.9f, 3.05f, 3.2f, 3.35f };
        private static readonly float[] PulseCooldownByLevel = { 1.10f, 1.05f, 0.98f, 0.90f, 0.82f };
        private static readonly int[] EmpDamageByLevel = { 0, 0, 5, 6, 8 };
        private static readonly float[] EmpRadiusByLevel = { 0f, 0f, 5.4f, 5.8f, 6.2f };
        private static readonly float[] EmpCooldownByLevel = { 0f, 0f, 5.3f, 4.8f, 4.4f };
        private static readonly int[] RangedDamageByLevel = { 0, 0, 0, 0, 12 };
        private static readonly float[] RangedCooldownByLevel = { 0f, 0f, 0f, 0f, 1.75f };

        public static int ClampLevel(int levelIndex) => Mathf.Clamp(levelIndex, 0, 4);
        public static float PlayerMaxHealth(int levelIndex) => PlayerHealthByLevel[ClampLevel(levelIndex)] + Mathf.Max(0,Mathf.Clamp(levelIndex,0,6)-4)*20f;
        public static int PulseDamage(int levelIndex) => PulseDamageByLevel[ClampLevel(levelIndex)];
        public static float PulseRadius(int levelIndex) => PulseRadiusByLevel[ClampLevel(levelIndex)];
        public static float PulseCooldown(int levelIndex) => PulseCooldownByLevel[ClampLevel(levelIndex)];
        public static int EmpDamage(int levelIndex) => EmpDamageByLevel[ClampLevel(levelIndex)];
        public static float EmpRadius(int levelIndex) => EmpRadiusByLevel[ClampLevel(levelIndex)];
        public static float EmpCooldown(int levelIndex) => EmpCooldownByLevel[ClampLevel(levelIndex)];
        public static int RangedDamage(int levelIndex) => RangedDamageByLevel[ClampLevel(levelIndex)];
        public static float RangedCooldown(int levelIndex) => RangedCooldownByLevel[ClampLevel(levelIndex)];

        public static EnemyTuning Enemy(string enemyKey, int levelIndex)
        {
            int level = ClampLevel(levelIndex);
            EnemyTuning tuning = enemyKey switch
            {
                "police" => new EnemyTuning(
                    new[] { 3, 4, 5, 6, 8 }[level],
                    new[] { 8f, 10f, 12f, 15f, 18f }[level],
                    new[] { 7.4f, 7.7f, 8.0f, 8.3f, 8.6f }[level]),

                "robber" => new EnemyTuning(
                    new[] { 2, 3, 4, 5, 6 }[level],
                    new[] { 5f, 6f, 8f, 10f, 12f }[level],
                    new[] { 7.6f, 7.9f, 8.1f, 8.35f, 8.6f }[level]),

                "jammer" => new EnemyTuning(
                    new[] { 4, 5, 6, 8, 10 }[level],
                    new[] { 4f, 5f, 6f, 8f, 10f }[level],
                    new[] { 6.0f, 6.2f, 6.4f, 6.6f, 6.8f }[level]),

                "racer" => new EnemyTuning(
                    new[] { 2, 3, 4, 5, 6 }[level],
                    new[] { 10f, 12f, 15f, 18f, 22f }[level],
                    new[] { 8.8f, 9.1f, 9.4f, 9.8f, 10.2f }[level]),

                "tow" => new EnemyTuning(
                    new[] { 5, 6, 7, 9, 11 }[level],
                    new[] { 7f, 9f, 11f, 14f, 17f }[level],
                    new[] { 5.8f, 6.0f, 6.15f, 6.3f, 6.45f }[level]),

                "oil" => new EnemyTuning(
                    new[] { 4, 5, 6, 7, 9 }[level],
                    new[] { 6f, 7f, 9f, 11f, 13f }[level],
                    new[] { 6.7f, 6.9f, 7.1f, 7.3f, 7.5f }[level]),

                "boss" => new EnemyTuning(
                    new[] { 16, 18, 20, 22, 26 }[level],
                    new[] { 18f, 20f, 22f, 24f, 28f }[level],
                    new[] { 6.2f, 6.3f, 6.4f, 6.5f, 6.7f }[level]),

                _ => new EnemyTuning(3 + level, 7f + level * 2f, 7f + level * 0.2f)
            };
            int late=Mathf.Max(0,Mathf.Clamp(levelIndex,0,6)-4);
            int health = enemyKey == "boss" ? 100 + late * 30 : Mathf.RoundToInt(tuning.Health*(1f+late*.23f));
            return new EnemyTuning(health,tuning.ContactDamage*(1f+late*.12f),tuning.Speed+late*.3f);
        }
    }
}
