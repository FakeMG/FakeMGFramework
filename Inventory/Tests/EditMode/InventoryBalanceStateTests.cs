using System.Collections.Generic;
using FakeMG.Framework;
using FakeMG.Inventory;
using FakeMG.Numbers;
using NUnit.Framework;
using UnityEditor;

namespace Inventory.EditMode
{
    public class InventoryBalanceStateTests
    {
        private const string FIXTURE_DIR =
            "Assets/Thirdparty/FakeMGFramework/Inventory/Tests/EditMode/Fixtures/";

        private InventoryBalanceState _state;
        private IdentitySO _coin;
        private IdentitySO _gem;

        private InventoryChange _lastChange;
        private int _balanceChangedCount;
        private int _reloadedCount;

        #region Setup

        [SetUp]
        public void SetUp()
        {
            _coin = LoadFixture("TestCoin");
            _gem = LoadFixture("TestGem");

            _state = new InventoryBalanceState();
            _lastChange = default;
            _balanceChangedCount = 0;
            _reloadedCount = 0;
        }

        [TearDown]
        public void TearDown()
        {
            _state.OnBalanceChanged -= RecordBalanceChange;
            _state.BalancesReloaded -= RecordBalancesReloaded;
        }

        private static IdentitySO LoadFixture(string assetName)
        {
            IdentitySO fixture = AssetDatabase.LoadAssetAtPath<IdentitySO>($"{FIXTURE_DIR}{assetName}.asset");
            Assert.IsNotNull(fixture,
                $"Missing test fixture '{assetName}'. Create an IdentitySO at '{FIXTURE_DIR}{assetName}.asset' with its Id set.");
            return fixture;
        }

        #endregion

        #region GetBalance

        [Test]
        public void GetBalance_UnknownItem_ReturnsZero()
        {
            Assert.AreEqual(0, (int)_state.GetBalance(_coin));
        }

        [Test]
        public void GetBalance_NullItem_ReturnsZero()
        {
            Assert.AreEqual(0, (int)_state.GetBalance(null));
        }

        #endregion

        #region TrySpend (single)

        [Test]
        public void TrySpend_ZeroAmount_ReturnsFalse()
        {
            _state.Add(_coin, (GameNumber)10);

            Assert.IsFalse(_state.TrySpend(_coin, GameNumber.Zero));
            Assert.AreEqual(10, (int)_state.GetBalance(_coin));
        }

        [Test]
        public void TrySpend_NegativeAmount_ReturnsFalse()
        {
            _state.Add(_coin, (GameNumber)10);

            Assert.IsFalse(_state.TrySpend(_coin, (GameNumber)(-5)));
            Assert.AreEqual(10, (int)_state.GetBalance(_coin));
        }

        [Test]
        public void TrySpend_InsufficientBalance_ReturnsFalse_AndDoesNotMutate()
        {
            _state.Add(_coin, (GameNumber)5);

            Assert.IsFalse(_state.TrySpend(_coin, (GameNumber)10));
            Assert.AreEqual(5, (int)_state.GetBalance(_coin));
        }

        [Test]
        public void TrySpend_SufficientBalance_DecrementsAndReturnsTrue()
        {
            _state.Add(_coin, (GameNumber)10);

            Assert.IsTrue(_state.TrySpend(_coin, (GameNumber)4));
            Assert.AreEqual(6, (int)_state.GetBalance(_coin));
        }

        [Test]
        public void TrySpend_ExactBalance_LeavesZero()
        {
            _state.Add(_coin, (GameNumber)10);

            Assert.IsTrue(_state.TrySpend(_coin, (GameNumber)10));
            Assert.AreEqual(0, (int)_state.GetBalance(_coin));
        }

        [Test]
        public void TrySpend_FiresBalanceChangedWithOldAndNewAmounts()
        {
            _state.Add(_coin, (GameNumber)10);
            _state.OnBalanceChanged += RecordBalanceChange;

            _state.TrySpend(_coin, (GameNumber)3);

            Assert.AreEqual(1, _balanceChangedCount);
            Assert.AreEqual(10, (int)_lastChange.OldCount);
            Assert.AreEqual(7, (int)_lastChange.NewCount);
        }

        #endregion

        #region TrySpend (batch)

        [Test]
        public void TrySpend_NullBatch_ReturnsFalse()
        {
            Assert.IsFalse(_state.TrySpend((IReadOnlyList<ItemAmountEntry>)null));
        }

        [Test]
        public void TrySpend_BatchDuplicateItems_AggregatesAndSpendsSum()
        {
            _state.Add(_coin, (GameNumber)10);

            bool spent = _state.TrySpend(new List<ItemAmountEntry>
            {
                new(_coin, (GameNumber)4),
                new(_coin, (GameNumber)4),
            });

            Assert.IsTrue(spent);
            Assert.AreEqual(2, (int)_state.GetBalance(_coin));
        }

        [Test]
        public void TrySpend_BatchDuplicateItemsExceedBalance_ReturnsFalse_AndDoesNotMutate()
        {
            _state.Add(_coin, (GameNumber)5);

            bool spent = _state.TrySpend(new List<ItemAmountEntry>
            {
                new(_coin, (GameNumber)3),
                new(_coin, (GameNumber)3),
            });

            Assert.IsFalse(spent);
            Assert.AreEqual(5, (int)_state.GetBalance(_coin));
        }

        [Test]
        public void TrySpend_BatchOneItemInsufficient_DoesNotMutateAnyItem()
        {
            _state.Add(_coin, (GameNumber)10);
            _state.Add(_gem, (GameNumber)1);

            bool spent = _state.TrySpend(new List<ItemAmountEntry>
            {
                new(_coin, (GameNumber)5),
                new(_gem, (GameNumber)5),
            });

            Assert.IsFalse(spent);
            Assert.AreEqual(10, (int)_state.GetBalance(_coin));
            Assert.AreEqual(1, (int)_state.GetBalance(_gem));
        }

        [Test]
        public void TrySpend_BatchAllSufficient_SpendsAllAndReturnsTrue()
        {
            _state.Add(_coin, (GameNumber)10);
            _state.Add(_gem, (GameNumber)8);

            bool spent = _state.TrySpend(new List<ItemAmountEntry>
            {
                new(_coin, (GameNumber)3),
                new(_gem, (GameNumber)5),
            });

            Assert.IsTrue(spent);
            Assert.AreEqual(7, (int)_state.GetBalance(_coin));
            Assert.AreEqual(3, (int)_state.GetBalance(_gem));
        }

        [Test]
        public void TrySpend_BatchSkipsInvalidEntries_AndProcessesValidOnes()
        {
            _state.Add(_coin, (GameNumber)10);

            bool spent = _state.TrySpend(new List<ItemAmountEntry>
            {
                null,
                new(_coin, GameNumber.Zero),
                new(_coin, (GameNumber)4),
            });

            Assert.IsTrue(spent);
            Assert.AreEqual(6, (int)_state.GetBalance(_coin));
        }

        #endregion

        #region Add

        [Test]
        public void Add_ZeroAmount_DoesNotChangeBalance()
        {
            _state.Add(_coin, GameNumber.Zero);

            Assert.AreEqual(0, (int)_state.GetBalance(_coin));
        }

        [Test]
        public void Add_NegativeAmount_DoesNotChangeBalance()
        {
            _state.Add(_coin, (GameNumber)(-5));

            Assert.AreEqual(0, (int)_state.GetBalance(_coin));
        }

        [Test]
        public void Add_AccumulatesBalance()
        {
            _state.Add(_coin, (GameNumber)5);
            _state.Add(_coin, (GameNumber)3);

            Assert.AreEqual(8, (int)_state.GetBalance(_coin));
        }

        [Test]
        public void Add_Batch_AggregatesDuplicates_AndReturnsAcceptedEntries()
        {
            IReadOnlyList<ItemAmountEntry> accepted = _state.Add(new List<ItemAmountEntry>
            {
                new(_coin, (GameNumber)4),
                new(_coin, (GameNumber)6),
                new(_gem, (GameNumber)2),
            });

            Assert.AreEqual(3, accepted.Count);
            Assert.AreEqual(10, (int)_state.GetBalance(_coin));
            Assert.AreEqual(2, (int)_state.GetBalance(_gem));
        }

        [Test]
        public void Add_NullBatch_ReturnsEmptyList()
        {
            IReadOnlyList<ItemAmountEntry> accepted = _state.Add((IReadOnlyList<ItemAmountEntry>)null);

            Assert.AreEqual(0, accepted.Count);
        }

        #endregion

        #region SetBalance

        [Test]
        public void SetBalance_NegativeAmount_ClampsToZero()
        {
            _state.SetBalance(_coin, (GameNumber)(-5));

            Assert.AreEqual(0, (int)_state.GetBalance(_coin));
        }

        [Test]
        public void SetBalance_OverwritesExistingBalance()
        {
            _state.Add(_coin, (GameNumber)10);

            _state.SetBalance(_coin, (GameNumber)3);

            Assert.AreEqual(3, (int)_state.GetBalance(_coin));
        }

        [Test]
        public void SetBalance_FiresBalanceChanged()
        {
            _state.OnBalanceChanged += RecordBalanceChange;

            _state.SetBalance(_coin, (GameNumber)7);

            Assert.AreEqual(1, _balanceChangedCount);
            Assert.AreEqual(7, (int)_lastChange.NewCount);
        }

        #endregion

        #region Save / Load

        [Test]
        public void CaptureState_ThenRestoreState_PreservesBalances()
        {
            _state.Add(_coin, (GameNumber)10);
            _state.Add(_gem, (GameNumber)25);
            InventoryData captured = _state.CaptureState();

            InventoryBalanceState restored = new();
            restored.RestoreState(captured);

            Assert.AreEqual(10, (int)restored.GetBalance(_coin));
            Assert.AreEqual(25, (int)restored.GetBalance(_gem));
        }

        [Test]
        public void RestoreState_NullData_RestoresEmpty_AndFiresReloaded()
        {
            _state.Add(_coin, (GameNumber)10);
            _state.BalancesReloaded += RecordBalancesReloaded;

            _state.RestoreState(null);

            Assert.AreEqual(1, _reloadedCount);
            Assert.AreEqual(0, (int)_state.GetBalance(_coin));
        }

        [Test]
        public void RestoreState_EmptyItemId_SkipsEntry_AndKeepsValidEntries()
        {
            InventoryData data = new()

            {
                AmountByItemId = new Dictionary<string, string>
                {
                    { "", "5" },
                    { _coin.Id, "7" },
                },
            };

            _state.RestoreState(data);

            Assert.AreEqual(7, (int)_state.GetBalance(_coin));
        }

        [Test]
        public void RestoreState_UnparsableAmount_SkipsEntry()
        {
            InventoryData data = new()

            {
                AmountByItemId = new Dictionary<string, string>
                {
                    { _coin.Id, "not_a_number" },
                    { _gem.Id, "5" },
                },
            };

            _state.RestoreState(data);

            Assert.AreEqual(0, (int)_state.GetBalance(_coin));
            Assert.AreEqual(5, (int)_state.GetBalance(_gem));
        }

        [Test]
        public void RestoreState_NegativeAmount_ResultsInZeroBalance()
        {
            InventoryData data = new()

            {
                AmountByItemId = new Dictionary<string, string>
                {
                    { _coin.Id, "-5" },
                },
            };

            _state.RestoreState(data);

            Assert.AreEqual(0, (int)_state.GetBalance(_coin));
        }

        [Test]
        public void RestoreDefaultState_NullBalances_RestoresEmpty_AndFiresReloaded()
        {
            _state.Add(_coin, (GameNumber)10);
            _state.BalancesReloaded += RecordBalancesReloaded;

            _state.RestoreDefaultState(null);

            Assert.AreEqual(1, _reloadedCount);
            Assert.AreEqual(0, (int)_state.GetBalance(_coin));
        }

        [Test]
        public void RestoreDefaultState_SetsInitialBalances()
        {
            _state.RestoreDefaultState(new List<ItemAmountEntry>
            {
                new(_coin, (GameNumber)10),
                new(_gem, (GameNumber)5),
            });

            Assert.AreEqual(10, (int)_state.GetBalance(_coin));
            Assert.AreEqual(5, (int)_state.GetBalance(_gem));
        }

        #endregion

        #region Event Handlers

        private void RecordBalanceChange(InventoryChange change)
        {
            _lastChange = change;
            _balanceChangedCount++;
        }

        private void RecordBalancesReloaded()
        {
            _reloadedCount++;
        }

        #endregion
    }
}
