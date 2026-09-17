using System;

namespace ArtifactCourier.Core
{
    [Serializable]
    public sealed class SaveData
    {
        public int[] cityScores = new int[7];
        public int currentLevelIndex;
        public int highestUnlockedLevelIndex;
        public int totalDeliveries;
        public int totalEnemiesDefeated;
        public string savedUtc;
        public int vehicleClass; // 0 bike, 1 pickup, 2 standard; migration default handled by version.
        public int progressionVersion;
        public int skillPoints;
        public int completedCityMask;
        public int engineRank;
        public int combatRank;
        public int utilityRank;
        public int coins;
        public int ownedCosmetics;
        public int[] equippedCosmetics = new int[3];
        public string[] claimedCoinIds = new string[0];

        public static SaveData NewGame() => new()
        {
            vehicleClass = 2, progressionVersion = 1,
            currentLevelIndex = 0,
            highestUnlockedLevelIndex = 0,
            totalDeliveries = 0,
            totalEnemiesDefeated = 0,
            savedUtc = DateTime.UtcNow.ToString("O")
        };
    }
}
