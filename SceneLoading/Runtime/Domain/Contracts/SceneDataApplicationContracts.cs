using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace FakeMG.SceneLoading
{
    public readonly struct LoadedSceneContext : IEquatable<LoadedSceneContext>
    {
        public int Id { get; }
        public string Name { get; }
        public bool IsValid => Id != 0;

        public LoadedSceneContext(int id, string name)
        {
            Id = id;
            Name = name ?? string.Empty;
        }

        public bool Equals(LoadedSceneContext other)
        {
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is LoadedSceneContext other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id;
        }
    }

    public interface ILoadedSceneDataApplier
    {
        string DataApplierId { get; }
        UniTask ApplyLoadedDataAsync(CancellationToken cancellationToken);
    }

    public interface ILoadedSceneDataApplierDependencies
    {
        IReadOnlyCollection<string> PrerequisiteDataApplierIds { get; }
    }

    public readonly struct SceneDataApplicationResult
    {
        public bool Succeeded { get; }
        public bool DidTimeOut { get; }
        public IReadOnlyList<string> FailedDataApplierIds { get; }
        public string FailureReason { get; }

        public SceneDataApplicationResult(
            bool succeeded,
            bool didTimeOut,
            IReadOnlyList<string> failedDataApplierIds,
            string failureReason)
        {
            Succeeded = succeeded;
            DidTimeOut = didTimeOut;
            FailedDataApplierIds = failedDataApplierIds == null
                ? Array.Empty<string>()
                : new List<string>(failedDataApplierIds).AsReadOnly();
            FailureReason = failureReason ?? string.Empty;
        }
    }

    public interface ISceneDataApplicationCoordinator
    {
        IDisposable Register(LoadedSceneContext sceneContext, ILoadedSceneDataApplier dataApplier);
        UniTask<SceneDataApplicationResult> ApplyLoadedDataAsync(
            LoadedSceneContext sceneContext,
            CancellationToken cancellationToken);
    }
}
