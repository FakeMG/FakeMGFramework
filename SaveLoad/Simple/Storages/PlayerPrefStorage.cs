using Newtonsoft.Json;
using UnityEngine;

namespace FakeMG.FakeMGFramework.SaveLoad.Simple.Storages
{
    public class PlayerPrefsStorage : ISaveStorage
    {
        private readonly JsonSerializerSettings _jsonSettings = new()
        {
            TypeNameHandling = TypeNameHandling.All
        };

        public void Save(string saveId, SaveProfile profile)
        {
            string json = JsonConvert.SerializeObject(profile, Formatting.None, _jsonSettings);
            PlayerPrefs.SetString(saveId, json);
            PlayerPrefs.Save();
        }

        public SaveProfile Load(string saveId)
        {
            string key = saveId;
            if (!PlayerPrefs.HasKey(key))
            {
                Debug.LogWarning($"No save data found for ID: {saveId}");
                return new SaveProfile(); // Or return null based on preference
            }

            string json = PlayerPrefs.GetString(key);
            return JsonConvert.DeserializeObject<SaveProfile>(json, _jsonSettings);
        }

        public void Delete(string saveId)
        {
            PlayerPrefs.DeleteKey(saveId);
            PlayerPrefs.Save();
        }

        public bool FileExists(string saveId)
        {
            return PlayerPrefs.HasKey(saveId);
        }
    }
}