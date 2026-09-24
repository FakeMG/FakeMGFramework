using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace FakeMG.SceneLoading.Tests.EditMode
{
    public sealed class SceneDataApplicationExecutionPlanBuilderTests
    {
        private const string ROOT_ID = "root";
        private const string LEFT_ID = "left";
        private const string RIGHT_ID = "right";
        private const string JOIN_ID = "join";
        private const string MIDDLE_ID = "middle";
        private const string LEAF_ID = "leaf";
        private const string MISSING_ID = "missing";
        private const string CYCLE_FIRST_ID = "cycle-first";
        private const string CYCLE_SECOND_ID = "cycle-second";

        #region Public Methods

        [Test]
        public void Build_EmptyApplierList_ReturnsEmptySuccessfulPlan()
        {
            SceneDataApplicationExecutionPlan plan = new SceneDataApplicationExecutionPlanBuilder()
                .Build(new List<ILoadedSceneDataApplier>());

            Assert.That(plan.Succeeded, Is.True);
            Assert.That(plan.ExecutionWaves, Is.Empty);
            Assert.That(plan.FailedDataApplierIds, Is.Empty);
        }

        [Test]
        public void Build_AppliersWithoutDependencies_ReturnsOneParallelWave()
        {
            SceneDataApplicationExecutionPlan plan = BuildPlan(
                new TestDataApplier(ROOT_ID),
                new TestDataApplier(LEFT_ID),
                new TestDataApplier(RIGHT_ID));

            Assert.That(plan.Succeeded, Is.True);
            AssertWaveIds(plan, 0, ROOT_ID, LEFT_ID, RIGHT_ID);
        }

        [Test]
        public void Build_LinearDependencyChain_ReturnsOneWavePerDependencyLevel()
        {
            SceneDataApplicationExecutionPlan plan = BuildPlan(
                new DependentTestDataApplier(LEAF_ID, MIDDLE_ID),
                new DependentTestDataApplier(MIDDLE_ID, ROOT_ID),
                new TestDataApplier(ROOT_ID));

            Assert.That(plan.Succeeded, Is.True);
            AssertWaveIds(plan, 0, ROOT_ID);
            AssertWaveIds(plan, 1, MIDDLE_ID);
            AssertWaveIds(plan, 2, LEAF_ID);
        }

        [Test]
        public void Build_FanInAndFanOut_ReturnsDependencyWaves()
        {
            SceneDataApplicationExecutionPlan plan = BuildPlan(
                new DependentTestDataApplier(JOIN_ID, LEFT_ID, RIGHT_ID),
                new DependentTestDataApplier(LEFT_ID, ROOT_ID),
                new DependentTestDataApplier(RIGHT_ID, ROOT_ID),
                new TestDataApplier(ROOT_ID));

            Assert.That(plan.Succeeded, Is.True);
            AssertWaveIds(plan, 0, ROOT_ID);
            AssertWaveIds(plan, 1, LEFT_ID, RIGHT_ID);
            AssertWaveIds(plan, 2, JOIN_ID);
        }

        [Test]
        public void Build_MissingDependency_ReturnsClearFailure()
        {
            SceneDataApplicationExecutionPlan plan = BuildPlan(
                new DependentTestDataApplier(ROOT_ID, MISSING_ID));

            Assert.That(plan.Succeeded, Is.False);
            Assert.That(plan.ExecutionWaves, Is.Empty);
            Assert.That(plan.FailedDataApplierIds, Is.EquivalentTo(new[] { ROOT_ID }));
            Assert.That(plan.FailureReason, Does.Contain("missing prerequisite"));
            Assert.That(plan.FailureReason, Does.Contain(MISSING_ID));
        }

        [Test]
        public void Build_CyclicDependency_ReturnsClearFailure()
        {
            SceneDataApplicationExecutionPlan plan = BuildPlan(
                new DependentTestDataApplier(CYCLE_FIRST_ID, CYCLE_SECOND_ID),
                new DependentTestDataApplier(CYCLE_SECOND_ID, CYCLE_FIRST_ID));

            Assert.That(plan.Succeeded, Is.False);
            Assert.That(plan.ExecutionWaves, Is.Empty);
            Assert.That(plan.FailedDataApplierIds, Is.EquivalentTo(new[] { CYCLE_FIRST_ID, CYCLE_SECOND_ID }));
            Assert.That(plan.FailureReason, Does.Contain("cycle"));
        }

        [Test]
        public void Build_DuplicateIds_PreservesDuplicateValidation()
        {
            SceneDataApplicationExecutionPlan plan = BuildPlan(
                new TestDataApplier(ROOT_ID),
                new TestDataApplier(ROOT_ID));

            Assert.That(plan.Succeeded, Is.False);
            Assert.That(plan.FailureReason, Does.Contain("Duplicate scene data applier ID"));
            Assert.That(plan.FailedDataApplierIds, Is.EquivalentTo(new[] { ROOT_ID }));
        }

        #endregion

        #region Private Methods

        private static SceneDataApplicationExecutionPlan BuildPlan(params ILoadedSceneDataApplier[] dataAppliers)
        {
            return new SceneDataApplicationExecutionPlanBuilder().Build(dataAppliers);
        }

        private static void AssertWaveIds(
            SceneDataApplicationExecutionPlan plan,
            int waveIndex,
            params string[] expectedIds)
        {
            Assert.That(plan.ExecutionWaves, Has.Count.GreaterThan(waveIndex));
            string[] actualIds = plan.ExecutionWaves[waveIndex]
                .Select(dataApplier => dataApplier.DataApplierId)
                .ToArray();
            Assert.That(actualIds, Is.EqualTo(expectedIds));
        }

        #endregion

        private class TestDataApplier : ILoadedSceneDataApplier
        {
            public TestDataApplier(string dataApplierId)
            {
                DataApplierId = dataApplierId;
            }

            public string DataApplierId { get; }

            public UniTask ApplyLoadedDataAsync(CancellationToken cancellationToken)
            {
                return UniTask.CompletedTask;
            }
        }

        private sealed class DependentTestDataApplier : TestDataApplier, ILoadedSceneDataApplierDependencies
        {
            public DependentTestDataApplier(string dataApplierId, params string[] prerequisiteDataApplierIds)
                : base(dataApplierId)
            {
                PrerequisiteDataApplierIds = prerequisiteDataApplierIds;
            }

            public IReadOnlyCollection<string> PrerequisiteDataApplierIds { get; }
        }
    }
}
