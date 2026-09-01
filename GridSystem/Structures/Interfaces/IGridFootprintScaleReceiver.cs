using UnityEngine;

namespace FakeMG.GridSystem
{
    /// <summary>
    /// Receives runtime dimensions derived from a grid footprint before placement validation.
    /// </summary>
    public interface IGridFootprintScaleReceiver
    {
        void SetFootprintScaleMeters(Vector3 footprintScaleMeters);

        void SetFootprintHorizontalCenterOffsetMeters(Vector3 horizontalCenterOffsetMeters);
    }
}
