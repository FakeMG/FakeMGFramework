using System;
using System.Collections.Generic;

namespace FakeMG.SceneLoading
{
    public interface ISceneDataApplicationExecutionPlanBuilder
    {
        SceneDataApplicationExecutionPlan Build(IReadOnlyList<ILoadedSceneDataApplier> dataAppliers);
    }

    public sealed class SceneDataApplicationExecutionPlanBuilder : ISceneDataApplicationExecutionPlanBuilder
    {
        #region Public Methods

        public SceneDataApplicationExecutionPlan Build(IReadOnlyList<ILoadedSceneDataApplier> dataAppliers)
        {
            if (dataAppliers == null)
            {
                return SceneDataApplicationExecutionPlan.Failure(
                    "Loaded scene data appliers are required.",
                    Array.Empty<string>());
            }

            Dictionary<string, DataApplierGraphNode> nodesById = BuildNodes(
                dataAppliers,
                out string failedDataApplierId,
                out string failureReason);
            if (nodesById == null)
            {
                return SceneDataApplicationExecutionPlan.Failure(
                    failureReason,
                    string.IsNullOrEmpty(failedDataApplierId)
                        ? Array.Empty<string>()
                        : new[] { failedDataApplierId });
            }

            if (!TryConnectDependencies(nodesById, dataAppliers, out failedDataApplierId, out failureReason))
            {
                return SceneDataApplicationExecutionPlan.Failure(failureReason, new[] { failedDataApplierId });
            }

            List<DataApplierGraphNode> readyNodes = FindReadyNodes(nodesById.Values);
            List<IReadOnlyList<ILoadedSceneDataApplier>> executionWaves = new();
            int processedDataApplierCount = 0;
            while (readyNodes.Count > 0)
            {
                List<ILoadedSceneDataApplier> executionWave = new(readyNodes.Count);
                foreach (DataApplierGraphNode readyNode in readyNodes)
                {
                    executionWave.Add(readyNode.DataApplier);
                }

                executionWaves.Add(executionWave.AsReadOnly());
                processedDataApplierCount += readyNodes.Count;
                readyNodes = AdvanceToNextWave(readyNodes);
            }

            if (processedDataApplierCount != nodesById.Count)
            {
                IReadOnlyList<string> cyclePath = FindCyclePath(nodesById.Values);
                return SceneDataApplicationExecutionPlan.Failure(
                    $"Loaded scene data applier dependency cycle detected: {string.Join(" -> ", cyclePath)}.",
                    GetUniqueCycleIds(cyclePath));
            }

            return SceneDataApplicationExecutionPlan.Success(executionWaves);
        }

        #endregion

        #region Private Methods

        private static Dictionary<string, DataApplierGraphNode> BuildNodes(
            IReadOnlyList<ILoadedSceneDataApplier> dataAppliers,
            out string failedDataApplierId,
            out string failureReason)
        {
            Dictionary<string, DataApplierGraphNode> nodesById = new(StringComparer.Ordinal);
            for (int index = 0; index < dataAppliers.Count; index++)
            {
                ILoadedSceneDataApplier dataApplier = dataAppliers[index];
                if (dataApplier == null || string.IsNullOrWhiteSpace(dataApplier.DataApplierId))
                {
                    failedDataApplierId = string.Empty;
                    failureReason = "Every loaded scene data applier requires a stable ID.";
                    return null;
                }

                if (!nodesById.TryAdd(
                        dataApplier.DataApplierId,
                        new DataApplierGraphNode(dataApplier, index)))
                {
                    failedDataApplierId = dataApplier.DataApplierId;
                    failureReason = $"Duplicate scene data applier ID '{dataApplier.DataApplierId}'.";
                    return null;
                }
            }

            failedDataApplierId = string.Empty;
            failureReason = string.Empty;
            return nodesById;
        }

        private static bool TryConnectDependencies(
            IReadOnlyDictionary<string, DataApplierGraphNode> nodesById,
            IReadOnlyList<ILoadedSceneDataApplier> dataAppliers,
            out string failedDataApplierId,
            out string failureReason)
        {
            foreach (ILoadedSceneDataApplier dataApplier in dataAppliers)
            {
                if (dataApplier is not ILoadedSceneDataApplierDependencies dependencyDeclaration ||
                    dependencyDeclaration.PrerequisiteDataApplierIds == null)
                {
                    continue;
                }

                DataApplierGraphNode dependentNode = nodesById[dataApplier.DataApplierId];
                HashSet<string> declaredPrerequisiteIds = new(StringComparer.Ordinal);
                foreach (string prerequisiteId in dependencyDeclaration.PrerequisiteDataApplierIds)
                {
                    if (string.IsNullOrWhiteSpace(prerequisiteId))
                    {
                        failedDataApplierId = dependentNode.Id;
                        failureReason =
                            $"Loaded scene data applier '{dependentNode.Id}' declares an empty prerequisite ID.";
                        return false;
                    }

                    if (!declaredPrerequisiteIds.Add(prerequisiteId))
                    {
                        failedDataApplierId = dependentNode.Id;
                        failureReason =
                            $"Loaded scene data applier '{dependentNode.Id}' declares prerequisite '{prerequisiteId}' more than once.";
                        return false;
                    }

                    if (!nodesById.TryGetValue(prerequisiteId, out DataApplierGraphNode prerequisiteNode))
                    {
                        failedDataApplierId = dependentNode.Id;
                        failureReason =
                            $"Loaded scene data applier '{dependentNode.Id}' depends on missing prerequisite '{prerequisiteId}'.";
                        return false;
                    }

                    dependentNode.Prerequisites.Add(prerequisiteNode);
                    prerequisiteNode.Dependents.Add(dependentNode);
                }

                dependentNode.RemainingPrerequisiteCount = dependentNode.Prerequisites.Count;
            }

            failedDataApplierId = string.Empty;
            failureReason = string.Empty;
            return true;
        }

        private static List<DataApplierGraphNode> FindReadyNodes(IEnumerable<DataApplierGraphNode> nodes)
        {
            List<DataApplierGraphNode> readyNodes = new();
            foreach (DataApplierGraphNode node in nodes)
            {
                if (node.RemainingPrerequisiteCount == 0)
                {
                    readyNodes.Add(node);
                }
            }

            SortByRegistrationOrder(readyNodes);
            return readyNodes;
        }

        private static List<DataApplierGraphNode> AdvanceToNextWave(
            IReadOnlyList<DataApplierGraphNode> completedNodes)
        {
            List<DataApplierGraphNode> nextReadyNodes = new();
            foreach (DataApplierGraphNode completedNode in completedNodes)
            {
                foreach (DataApplierGraphNode dependentNode in completedNode.Dependents)
                {
                    dependentNode.RemainingPrerequisiteCount--;
                    if (dependentNode.RemainingPrerequisiteCount == 0)
                    {
                        nextReadyNodes.Add(dependentNode);
                    }
                }
            }

            SortByRegistrationOrder(nextReadyNodes);
            return nextReadyNodes;
        }

        private static void SortByRegistrationOrder(List<DataApplierGraphNode> nodes)
        {
            nodes.Sort(CompareRegistrationOrder);
        }

        private static int CompareRegistrationOrder(DataApplierGraphNode leftNode, DataApplierGraphNode rightNode)
        {
            return leftNode.RegistrationOrder.CompareTo(rightNode.RegistrationOrder);
        }

        private static IReadOnlyList<string> FindCyclePath(IEnumerable<DataApplierGraphNode> nodes)
        {
            Dictionary<DataApplierGraphNode, VisitState> visitStates = new();
            List<DataApplierGraphNode> currentPath = new();
            foreach (DataApplierGraphNode node in nodes)
            {
                if (visitStates.ContainsKey(node))
                {
                    continue;
                }

                if (TryFindCyclePath(node, visitStates, currentPath, out IReadOnlyList<string> cyclePath))
                {
                    return cyclePath;
                }
            }

            return Array.Empty<string>();
        }

        private static bool TryFindCyclePath(
            DataApplierGraphNode node,
            IDictionary<DataApplierGraphNode, VisitState> visitStates,
            IList<DataApplierGraphNode> currentPath,
            out IReadOnlyList<string> cyclePath)
        {
            if (visitStates.TryGetValue(node, out VisitState visitState))
            {
                if (visitState != VisitState.Visiting)
                {
                    cyclePath = Array.Empty<string>();
                    return false;
                }

                int cycleStartIndex = currentPath.IndexOf(node);
                List<string> detectedCyclePath = new();
                for (int index = cycleStartIndex; index < currentPath.Count; index++)
                {
                    detectedCyclePath.Add(currentPath[index].Id);
                }

                detectedCyclePath.Add(node.Id);
                cyclePath = detectedCyclePath.AsReadOnly();
                return true;
            }

            visitStates[node] = VisitState.Visiting;
            currentPath.Add(node);
            foreach (DataApplierGraphNode prerequisiteNode in node.Prerequisites)
            {
                if (TryFindCyclePath(prerequisiteNode, visitStates, currentPath, out cyclePath))
                {
                    return true;
                }
            }

            currentPath.RemoveAt(currentPath.Count - 1);
            visitStates[node] = VisitState.Visited;
            cyclePath = Array.Empty<string>();
            return false;
        }

        private static IReadOnlyList<string> GetUniqueCycleIds(IReadOnlyList<string> cyclePath)
        {
            HashSet<string> uniqueIds = new(StringComparer.Ordinal);
            List<string> result = new();
            foreach (string dataApplierId in cyclePath)
            {
                if (uniqueIds.Add(dataApplierId))
                {
                    result.Add(dataApplierId);
                }
            }

            return result.AsReadOnly();
        }

        private sealed class DataApplierGraphNode
        {
            public string Id => DataApplier.DataApplierId;
            public ILoadedSceneDataApplier DataApplier { get; }
            public int RegistrationOrder { get; }
            public List<DataApplierGraphNode> Prerequisites { get; } = new();
            public List<DataApplierGraphNode> Dependents { get; } = new();
            public int RemainingPrerequisiteCount { get; set; }

            public DataApplierGraphNode(ILoadedSceneDataApplier dataApplier, int registrationOrder)
            {
                DataApplier = dataApplier;
                RegistrationOrder = registrationOrder;
            }
        }

        private enum VisitState
        {
            Visiting,
            Visited
        }

        #endregion
    }

    public sealed class SceneDataApplicationExecutionPlan
    {
        private SceneDataApplicationExecutionPlan(
            bool succeeded,
            IReadOnlyList<IReadOnlyList<ILoadedSceneDataApplier>> executionWaves,
            IReadOnlyList<string> failedDataApplierIds,
            string failureReason)
        {
            Succeeded = succeeded;
            ExecutionWaves = CopyWaves(executionWaves);
            FailedDataApplierIds = failedDataApplierIds == null
                ? Array.Empty<string>()
                : new List<string>(failedDataApplierIds).AsReadOnly();
            FailureReason = failureReason ?? string.Empty;
        }

        #region Public Properties

        public bool Succeeded { get; }
        public IReadOnlyList<IReadOnlyList<ILoadedSceneDataApplier>> ExecutionWaves { get; }
        public IReadOnlyList<string> FailedDataApplierIds { get; }
        public string FailureReason { get; }

        #endregion

        #region Public Methods

        public static SceneDataApplicationExecutionPlan Success(
            IReadOnlyList<IReadOnlyList<ILoadedSceneDataApplier>> executionWaves)
        {
            return new SceneDataApplicationExecutionPlan(
                true,
                executionWaves,
                Array.Empty<string>(),
                string.Empty);
        }

        public static SceneDataApplicationExecutionPlan Failure(
            string failureReason,
            IReadOnlyList<string> failedDataApplierIds)
        {
            return new SceneDataApplicationExecutionPlan(
                false,
                Array.Empty<IReadOnlyList<ILoadedSceneDataApplier>>(),
                failedDataApplierIds,
                failureReason);
        }

        #endregion

        #region Private Methods

        private static IReadOnlyList<IReadOnlyList<ILoadedSceneDataApplier>> CopyWaves(
            IReadOnlyList<IReadOnlyList<ILoadedSceneDataApplier>> executionWaves)
        {
            if (executionWaves == null)
            {
                return Array.Empty<IReadOnlyList<ILoadedSceneDataApplier>>();
            }

            List<IReadOnlyList<ILoadedSceneDataApplier>> copiedWaves = new(executionWaves.Count);
            foreach (IReadOnlyList<ILoadedSceneDataApplier> executionWave in executionWaves)
            {
                copiedWaves.Add(new List<ILoadedSceneDataApplier>(executionWave).AsReadOnly());
            }

            return copiedWaves.AsReadOnly();
        }

        #endregion
    }
}
