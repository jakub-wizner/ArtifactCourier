using System.IO;
using UnityEngine;

namespace ArtifactCourier.Core
{
    public sealed class SaveService
    {
        private const string FileName = "artifact_courier_save.json";
        private string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        public bool HasSave => File.Exists(SavePath);

        public void Save(SaveData data)
        {
            data.savedUtc = System.DateTime.UtcNow.ToString("O");
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
        }

        public SaveData LoadOrNew()
        {
            if (!HasSave) return SaveData.NewGame();

            try
            {
                string json = File.ReadAllText(SavePath);
                SaveData loadedData = JsonUtility.FromJson<SaveData>(json);
                if(loadedData==null)return SaveData.NewGame();
                if(loadedData.progressionVersion==0)
                {
                    loadedData.vehicleClass=2;loadedData.progressionVersion=1;
                    int cleared=Mathf.Clamp(loadedData.highestUnlockedLevelIndex,0,6);
                    loadedData.completedCityMask=(1<<cleared)-1;
                    loadedData.skillPoints=cleared;
                }
                return loadedData;
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"Save file could not be read. Starting fresh. {exception.Message}");
                return SaveData.NewGame();
            }
        }

        public void Delete()
        {
            if (HasSave) File.Delete(SavePath);
        }
    }
}
