using System;
using System.Collections.Generic;

namespace FakeMG.ActionMapManagement
{
    /// <summary>
    /// Holds action maps off while any suspension of them is held, e.g. while a popup loads or after the colony falls.
    /// Suspensions stack: a map comes back only once every suspension covering it is released. A suspension does not
    /// change which maps are enabled, so maps enabled or disabled meanwhile take that state once it ends.
    /// </summary>
    public sealed class ActionMapSuspensions
    {
        private readonly Dictionary<string, int> _suspensionCountsByMapName = new();
        private int _allMapsSuspensionCount;

        /// <summary>Raised whenever a suspension starts or ends, so the owner can re-apply which maps are on.</summary>
        public event Action OnChanged;

        #region Public Methods

        public bool IsSuspended(string mapName)
        {
            return _allMapsSuspensionCount > 0
                || (_suspensionCountsByMapName.TryGetValue(mapName, out int suspensionCount) && suspensionCount > 0);
        }

        /// <summary>Holds every map off until the returned suspension is disposed.</summary>
        public IDisposable SuspendAll(string reason)
        {
            _allMapsSuspensionCount++;
            OnChanged?.Invoke();
            return new Suspension(this, reason, null);
        }

        /// <summary>Holds the named maps off until the returned suspension is disposed.</summary>
        public IDisposable Suspend(string reason, IReadOnlyCollection<string> mapNames)
        {
            var suspendedMapNames = new List<string>(mapNames);
            foreach (string mapName in suspendedMapNames)
            {
                _suspensionCountsByMapName.TryGetValue(mapName, out int suspensionCount);
                _suspensionCountsByMapName[mapName] = suspensionCount + 1;
            }

            OnChanged?.Invoke();
            return new Suspension(this, reason, suspendedMapNames);
        }

        #endregion

        #region Private Methods

        private void Release(IReadOnlyList<string> mapNames)
        {
            if (mapNames == null)
            {
                _allMapsSuspensionCount--;
            }
            else
            {
                foreach (string mapName in mapNames)
                {
                    int suspensionCount = _suspensionCountsByMapName[mapName] - 1;
                    if (suspensionCount == 0)
                    {
                        _suspensionCountsByMapName.Remove(mapName);
                    }
                    else
                    {
                        _suspensionCountsByMapName[mapName] = suspensionCount;
                    }
                }
            }

            OnChanged?.Invoke();
        }

        #endregion

        /// <remarks>Disposing twice releases once, so a caller may both dispose early and dispose on teardown.</remarks>
        private sealed class Suspension : IDisposable
        {
            private readonly ActionMapSuspensions _owner;
            private readonly IReadOnlyList<string> _mapNames;
            private bool _isReleased;

            /// <summary>Why the maps are held off; read when debugging stuck input.</summary>
            public string Reason { get; }

            public Suspension(ActionMapSuspensions owner, string reason, IReadOnlyList<string> mapNames)
            {
                _owner = owner;
                _mapNames = mapNames;
                Reason = reason;
            }

            public void Dispose()
            {
                if (_isReleased)
                {
                    return;
                }

                _isReleased = true;
                _owner.Release(_mapNames);
            }
        }
    }
}
