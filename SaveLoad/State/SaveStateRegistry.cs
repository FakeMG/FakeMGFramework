using System.Collections.Generic;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.SaveLoad
{
    /// <summary>
    /// Owns discovery, identity validation, capture, and ordering of synchronous saveables and
    /// asynchronous participants. Restoration belongs to the load executor that owns load policy.
    /// </summary>
    public sealed class SaveStateRegistry
    {
        private readonly Dictionary<string, ISaveable> _saveables = new();
        private readonly List<IAsyncSaveParticipant> _asyncParticipants = new();
        private readonly List<IAsyncSaveParticipant> _hierarchyParticipants = new();

        public IReadOnlyDictionary<string, ISaveable> Saveables => _saveables;
        public IReadOnlyList<IAsyncSaveParticipant> AsyncParticipants => _asyncParticipants;

        #region Public Methods

        public void Refresh(Transform collectionRoot)
        {
            _saveables.Clear();
            foreach (IAsyncSaveParticipant hierarchyParticipant in _hierarchyParticipants)
            {
                _asyncParticipants.Remove(hierarchyParticipant);
            }

            _hierarchyParticipants.Clear();
            foreach (MonoBehaviour behaviour in collectionRoot.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour is ISaveable saveable)
                {
                    RegisterSaveable(saveable);
                }

                if (behaviour is IAsyncSaveParticipant participant && !_asyncParticipants.Contains(participant))
                {
                    _asyncParticipants.Add(participant);
                    _hierarchyParticipants.Add(participant);
                }
            }

            _asyncParticipants.Sort(CompareParticipantSaveOrder);
        }

        public bool RegisterAsyncParticipant(IAsyncSaveParticipant participant)
        {
            if (participant == null)
            {
                Echo.Error("Cannot register a missing asynchronous save participant.");
                return false;
            }

            if (_asyncParticipants.Contains(participant))
            {
                Echo.Warning($"Save participant {participant.GetType().Name} is already registered.");
                return false;
            }

            _asyncParticipants.Add(participant);
            _asyncParticipants.Sort(CompareParticipantSaveOrder);
            return true;
        }

        public void UnregisterAsyncParticipant(IAsyncSaveParticipant participant)
        {
            if (!_asyncParticipants.Remove(participant))
            {
                Echo.Warning("Ignored asynchronous participant unregistration because it was not registered.");
            }

            _hierarchyParticipants.Remove(participant);
        }

        public IReadOnlyDictionary<string, object> CaptureStates()
        {
            var capturedStates = new Dictionary<string, object>(_saveables.Count);
            foreach (KeyValuePair<string, ISaveable> saveable in _saveables)
            {
                capturedStates.Add(saveable.Key, saveable.Value.CaptureState());
            }

            return capturedStates;
        }

        #endregion

        #region Private Methods

        private void RegisterSaveable(ISaveable saveable)
        {
            string uniqueId = saveable.GetUniqueId();
            if (string.IsNullOrEmpty(uniqueId))
            {
                Echo.Error($"Saveable {GetSaveableDisplayName(saveable)} has invalid ID.");
                return;
            }

            if (_saveables.ContainsKey(uniqueId))
            {
                Echo.Warning(
                    $"Duplicate Saveable ID {uniqueId} found on " +
                    $"{GetSaveableDisplayName(saveable)}. Overwriting.");
            }

            _saveables[uniqueId] = saveable;
        }

        private static int CompareParticipantSaveOrder(IAsyncSaveParticipant left, IAsyncSaveParticipant right)
        {
            return left.SaveOrder.CompareTo(right.SaveOrder);
        }

        private static string GetSaveableDisplayName(ISaveable saveable)
        {
            return saveable is MonoBehaviour behaviour
                ? behaviour.name
                : saveable.GetType().Name;
        }

        #endregion
    }
}
