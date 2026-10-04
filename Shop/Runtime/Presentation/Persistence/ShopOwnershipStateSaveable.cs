using System;
using System.Collections.Generic;
using FakeMG.SaveLoad;
using UnityEngine;

namespace FakeMG.Shop
{
    [Serializable]
    public class ShopOwnershipStateData
    {
        public List<string> OwnedListingIds = new();
    }

    public sealed class ShopOwnershipStateSaveable : MonoBehaviour, ISaveable
    {
        public const string SAVE_ID = nameof(ShopOwnershipStateSaveable);

        [SerializeField] private List<string> _defaultOwnedListingIds = new();

        private readonly HashSet<string> _ownedListingIds = new();

        public string SaveId => SAVE_ID;
        public event Action<string> OnOwnershipChanged;

        #region Public Methods

        public bool IsOwned(string listingId)
        {
            if (string.IsNullOrWhiteSpace(listingId))
            {
                return false;
            }

            return _ownedListingIds.Contains(listingId);
        }

        public void MarkOwned(string listingId)
        {
            if (string.IsNullOrWhiteSpace(listingId))
            {
                return;
            }

            _ownedListingIds.Add(listingId);
            OnOwnershipChanged?.Invoke(listingId);
        }

        public object CaptureState()
        {
            return new ShopOwnershipStateData
            {
                OwnedListingIds = new List<string>(_ownedListingIds),
            };
        }

        public bool TryValidateState(object state, out string failureReason)
        {
            if (state is ShopOwnershipStateData ownershipState && ownershipState.OwnedListingIds != null)
            {
                failureReason = string.Empty;
                return true;
            }

            failureReason = "Shop ownership state is invalid.";
            return false;
        }

        public void RestoreState(object data)
        {
            var shopOwnershipStateData = (ShopOwnershipStateData)data;
            _ownedListingIds.Clear();
            for (int listingIndex = 0; listingIndex < shopOwnershipStateData.OwnedListingIds.Count; listingIndex++)
            {
                string listingId = shopOwnershipStateData.OwnedListingIds[listingIndex];
                if (string.IsNullOrWhiteSpace(listingId))
                {
                    continue;
                }

                _ownedListingIds.Add(listingId);
            }
        }

        public void RestoreDefaultState()
        {
            _ownedListingIds.Clear();
            for (int listingIndex = 0; listingIndex < _defaultOwnedListingIds.Count; listingIndex++)
            {
                string listingId = _defaultOwnedListingIds[listingIndex];
                if (string.IsNullOrWhiteSpace(listingId))
                {
                    continue;
                }

                _ownedListingIds.Add(listingId);
            }
        }

        #endregion
    }
}
