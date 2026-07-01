using System;
using System.Collections.Generic;
using System.Numerics;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;

namespace FakeMG.Inventory.Hud
{
    public sealed class CounterUpdateInformer
    {
        private readonly ItemCounterRegistry _counterRegistry;
        private readonly Dictionary<CounterUpdateDelayGroupSO, int> _activeDelayScopeCountByGroup = new();

        public CounterUpdateInformer(ItemCounterRegistry counterRegistry)
        {
            _counterRegistry = counterRegistry;
        }

        #region Public Methods

        public void Inform(IdentitySO itemSo, BigInteger newBalance)
        {
            if (!_counterRegistry.TryGetCounters(itemSo, out IReadOnlyList<ItemCounterView> counters))
            {
                return;
            }

            for (int counterIndex = 0; counterIndex < counters.Count; counterIndex++)
            {
                ItemCounterView counter = counters[counterIndex];
                if (IsDelayed(counter.DelayGroupSO))
                {
                    continue;
                }

                counter.AnimateDisplayedCountToAsync(newBalance).Forget();
            }
        }

        // Suppresses automatic updates for counters authored with delayGroupSO for the scope's lifetime, so a
        // presenter (e.g. a fly-to-counter visual) can apply the balance itself once its animation finishes.
        // Reference-counted so overlapping grants sharing the same group (e.g. two enemies dying at once)
        // don't let one finishing early re-enable updates for the other still in flight.
        public IDisposable BeginDelay(CounterUpdateDelayGroupSO delayGroupSO)
        {
            if (!delayGroupSO)
            {
                return NoOpScope.Instance;
            }

            _activeDelayScopeCountByGroup.TryGetValue(delayGroupSO, out int activeCount);
            _activeDelayScopeCountByGroup[delayGroupSO] = activeCount + 1;
            return new DelayScope(this, delayGroupSO);
        }

        #endregion

        #region Private Methods

        private bool IsDelayed(CounterUpdateDelayGroupSO delayGroupSO)
        {
            return delayGroupSO && _activeDelayScopeCountByGroup.TryGetValue(delayGroupSO, out int activeCount) && activeCount > 0;
        }

        private void EndDelay(CounterUpdateDelayGroupSO delayGroupSO)
        {
            if (!_activeDelayScopeCountByGroup.TryGetValue(delayGroupSO, out int activeCount))
            {
                return;
            }

            if (activeCount <= 1)
            {
                _activeDelayScopeCountByGroup.Remove(delayGroupSO);
            }
            else
            {
                _activeDelayScopeCountByGroup[delayGroupSO] = activeCount - 1;
            }
        }

        #endregion

        private sealed class DelayScope : IDisposable
        {
            private readonly CounterUpdateInformer _informer;
            private readonly CounterUpdateDelayGroupSO _delayGroupSO;
            private bool _isDisposed;

            public DelayScope(CounterUpdateInformer informer, CounterUpdateDelayGroupSO delayGroupSO)
            {
                _informer = informer;
                _delayGroupSO = delayGroupSO;
            }

            public void Dispose()
            {
                if (_isDisposed)
                {
                    return;
                }

                _isDisposed = true;
                _informer.EndDelay(_delayGroupSO);
            }
        }

        private sealed class NoOpScope : IDisposable
        {
            public static readonly NoOpScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
