#if UNITY_EDITOR
using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FakeMG.Framework.SaveLoad.Examples
{
    /// <summary>
    /// Example of how a system would request and apply data using the new workflow.
    /// This system registers with DataApplicationManager and applies data when requested.
    /// </summary>
    public class ExampleUISystem : DataRequester
    {
        [SerializeField] private ExamplePlayerDataSaveableReference playerSaveableRef;

        private string SystemIdentifier => $"{GetType().Name}({name})";

        public override async UniTask ApplyDataAsync()
        {
            Debug.Log($"[{SystemIdentifier}] Starting data application...");

            await UpdateUIWithPlayerData();

            Debug.Log($"[{SystemIdentifier}] Data application completed");
        }

        private async UniTask UpdateUIWithPlayerData()
        {
            // Simulate async UI updates (loading textures, animating, etc.)
            await UniTask.Delay(100); // Replace this with actual UI update logic
        }
    }

    [Serializable]
    public class PlayerSaveData
    {
        public int Level;
        public float Health;
        public string Name;
    }
}
#endif