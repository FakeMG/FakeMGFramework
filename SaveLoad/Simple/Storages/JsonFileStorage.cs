using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace FakeMG.Framework.SaveLoad.Simple.Storages
{
    public class JsonFileStorage : ISaveStorage
    {
        private string GetPath(string saveId) =>
            Path.Combine(Application.persistentDataPath, $"{saveId}.json");

        public void Save(string saveId, SaveProfile profile)
        {
            var json = JsonConvert.SerializeObject(profile, Formatting.Indented,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
            File.WriteAllText(GetPath(saveId), json);
        }

        public SaveProfile Load(string saveId)
        {
            var json = File.ReadAllText(GetPath(saveId));
            return JsonConvert.DeserializeObject<SaveProfile>(json,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
        }

        public bool FileExists(string saveId)
        {
            return File.Exists(GetPath(saveId));
        }
    }
}