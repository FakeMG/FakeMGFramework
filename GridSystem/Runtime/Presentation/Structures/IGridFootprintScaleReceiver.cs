using UnityEngine;

namespace FakeMG.GridSystem
{
    public interface IGridFootprintScaleReceiver
    {
        void SetFootprintScaleMeters(Vector3 footprintScaleMeters);

        void SetFootprintHorizontalCenterOffsetMeters(Vector3 horizontalCenterOffsetMeters);
    }
}
