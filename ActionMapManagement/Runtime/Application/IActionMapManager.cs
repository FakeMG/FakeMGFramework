using System;
using System.Collections.Generic;

namespace FakeMG.ActionMapManagement
{
    public interface IActionMapManager
    {
        bool IsActionMapActive(string mapName);
        void EnableActionMap(string mapName);
        void DisableActionMap(string mapName);

        /// <summary>Holds every map off until the returned suspension is disposed, e.g. while a popup loads.</summary>
        IDisposable SuspendAllActionMaps(string reason);

        /// <summary>Holds the named maps off until the returned suspension is disposed, whatever enables them meanwhile.</summary>
        IDisposable SuspendActionMaps(string reason, IReadOnlyCollection<string> mapNames);
    }
}
