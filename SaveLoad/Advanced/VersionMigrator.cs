using System;
using UnityEngine;

namespace FakeMG.SaveLoad.Advanced {
    public static class VersionMigrator {
        public static void MigrateSaveData(string slotKey, string saveVersion) {
            Debug.Log($"Migrating save data from version {saveVersion} to {Application.version}");

            try {
                if (saveVersion == "1.0.0" && Application.version == "1.1.0") {
                    if (ES3.KeyExists("PlayerStats", slotKey)) {
                        var data = ES3.Load("PlayerStats", slotKey);
                        if (data is PlayerData oldData) {
                            var newData = new PlayerData {
                                health = oldData.health,
                                position = oldData.position
                            };
                            ES3.Save("PlayerStats", newData, slotKey);
                            Debug.Log("Migrated PlayerStats to version 1.1.0");
                        }
                    }
                } else {
                    Debug.LogWarning($"No migration path defined for {saveVersion} to {Application.version}");
                }

                SaveMetadata metadata = ES3.Load(SaveLoadSystem.METADATA_KEY, slotKey, new SaveMetadata());
                metadata.gameVersion = Application.version;
                ES3.Save(SaveLoadSystem.METADATA_KEY, metadata, slotKey);
            } catch (Exception e) {
                Debug.LogError($"Migration failed for {slotKey}: {e.Message}");
            }
        }
    }
}