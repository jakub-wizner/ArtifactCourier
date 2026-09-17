using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArtifactCourier.Core
{
    public sealed class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        public SaveData Data { get; private set; }
        public SaveService Saves { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureExists()
        {
            if (Instance != null) return;
            var go = new GameObject("GameSession");
            DontDestroyOnLoad(go);
            go.AddComponent<GameSession>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Saves = new SaveService();
            Data = Saves.LoadOrNew();
        }

        public static int SelectedVehicle = 2;
        public bool BuyUpgrade(int branch)
        {
            if (Data.skillPoints <= 0 || branch < 0 || branch > 2) return false;
            int rank = branch==0?Data.engineRank:branch==1?Data.combatRank:Data.utilityRank;
            if (rank >= 2 || (rank == 1 && Data.highestUnlockedLevelIndex < 3)) return false;
            if (branch==0) Data.engineRank++; else if(branch==1) Data.combatRank++; else Data.utilityRank++;
            Data.skillPoints--; Saves.Save(Data); return true;
        }

        public void EarnCoins(int amount){if(amount<=0)return;Data.coins+=amount;Saves.Save(Data);}
        public bool ClaimCoin(string id)
        {
            var claimed=new System.Collections.Generic.List<string>(Data.claimedCoinIds ?? new string[0]);
            if(claimed.Contains(id))return false;
            claimed.Add(id);Data.claimedCoinIds=claimed.ToArray();Data.coins+=10;Saves.Save(Data);return true;
        }
        public bool PurchaseCosmetic(int vehicle,int tier)
        {
            if(vehicle<0||vehicle>2||tier<0||tier>2||vehicle!=Data.vehicleClass)return false;
            int bit=1<<(tier*3+vehicle);int price=Player.CosmeticCatalog.Price(tier);
            if(Data.highestUnlockedLevelIndex+1<Player.CosmeticCatalog.UnlockLevel(tier))return false;
            if((Data.ownedCosmetics&bit)==0){if(Data.coins<price)return false;Data.coins-=price;Data.ownedCosmetics|=bit;}
            if(Data.equippedCosmetics==null||Data.equippedCosmetics.Length!=3)Data.equippedCosmetics=new int[3];
            Data.equippedCosmetics[vehicle]=tier+1;Saves.Save(Data);return true;
        }
        public void EquipOriginal(int vehicle)
        {
            if(vehicle<0||vehicle>2||vehicle!=Data.vehicleClass)return;
            if(Data.equippedCosmetics==null||Data.equippedCosmetics.Length!=3)Data.equippedCosmetics=new int[3];
            Data.equippedCosmetics[vehicle]=0;Saves.Save(Data);
        }

        public void StartNewGame()
        {
            Saves.Delete();
            Data = SaveData.NewGame();
            Data.vehicleClass = Mathf.Clamp(SelectedVehicle,0,2);
            Saves.Save(Data);
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameScenes.Levels[0]);
        }

        public void LoadGame()
        {
            Data = Saves.LoadOrNew();
            int level = Mathf.Clamp(Data.currentLevelIndex, 0, GameScenes.Levels.Length - 1);
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameScenes.Levels[level]);
        }

        public void EnterLevel(int levelIndex)
        {
            Data.currentLevelIndex = Mathf.Clamp(levelIndex, 0, GameScenes.Levels.Length - 1);
            Data.highestUnlockedLevelIndex = Mathf.Max(Data.highestUnlockedLevelIndex, Data.currentLevelIndex);
            Saves.Save(Data);
        }

        public void CompleteLevel(int levelIndex, int deliveries, int enemiesDefeated)
        {
            if (Data.cityScores == null || Data.cityScores.Length != GameScenes.Levels.Length)
                System.Array.Resize(ref Data.cityScores, GameScenes.Levels.Length);
            var style = FindFirstObjectByType<ArtifactCourier.Driving.DrivingStyle>();
            Data.cityScores[levelIndex] = Mathf.Max(Data.cityScores[levelIndex], style == null ? 0 : style.Score);
            if ((Data.completedCityMask & (1 << levelIndex)) == 0)
            {
                Data.completedCityMask |= 1 << levelIndex;
                Data.skillPoints++;
                Data.coins+=100;
            }
            Data.totalDeliveries += Mathf.Max(0, deliveries);
            Data.totalEnemiesDefeated += Mathf.Max(0, enemiesDefeated);
            Data.highestUnlockedLevelIndex = Mathf.Max(Data.highestUnlockedLevelIndex, Mathf.Min(levelIndex + 1, GameScenes.Levels.Length - 1));
            Data.currentLevelIndex = Mathf.Min(levelIndex + 1, GameScenes.Levels.Length - 1);
            Saves.Save(Data);
        }

        public void LoadNextLevelOrMenu(int currentLevelIndex)
        {
            Time.timeScale = 1f;
            int next = currentLevelIndex + 1;
            SceneManager.LoadScene(GameScenes.IsValidLevelIndex(next) ? GameScenes.Levels[next] : GameScenes.MainMenu);
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameScenes.MainMenu);
        }
    }
}
