using System;
using System.Collections.Generic;
using FakeMG.SaveLoad;
using UnityEngine;

namespace FakeMG.Shop.Services.Catalog
{
    [Serializable]
    public class ShopOwnershipStateData
    {
        public List<string> OwnedListingIds = new();
    }

    public class ShopOwnershipStateRepository : Saveable
    {
        [SerializeField] private List<string> _defaultOwnedListingIds = new();

        private readonly HashSet<string> _ownedListingIds = new();

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
        }

        public override object CaptureState()
        {
            return new ShopOwnershipStateData
            {
                OwnedListingIds = new List<string>(_ownedListingIds),
            };
        }

        public override void RestoreState(object data)
        {
            if (!StateRestoreUtility.TryRestore(data, out ShopOwnershipStateData shopOwnershipStateData)
                || shopOwnershipStateData.OwnedListingIds == null)
            {
                RestoreDefaultState();
                return;
            }

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

        public override void RestoreDefaultState()
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
