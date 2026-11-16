using System.Threading;
using Cysharp.Threading.Tasks;

namespace FakeMG.GridSystem
{
    public interface IGridOccupantReplacer
    {
        /// <returns>False when the replacement could not be created or placed; the current occupant is kept.</returns>
        UniTask<bool> ReplaceStructureAsync(string instanceId, StructureSO replacementStructureSO, CancellationToken cancellationToken);
    }
}
