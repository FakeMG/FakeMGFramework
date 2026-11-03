using UnityEngine;

namespace FakeMG.Outline
{
    public interface IOutlineSelectionBounds
    {
        bool TryGetWorldBounds(out Bounds boundsMeters);
    }
}
