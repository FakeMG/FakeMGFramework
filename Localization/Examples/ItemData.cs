using UnityEngine;

namespace FakeMG.Localization.Examples
{
    // Example of a ScriptableObject that references localization keys
    // This allows designers to assign localization keys directly in the inspector
    [CreateAssetMenu]
    public class ItemData : ScriptableObject
    {
        public string ItemID;

        [Header("Localization Key")]
        // We use a custom attribute or just a string, but we fill it using the const
        // For stronger typing, use a Dropdown drawer (see below)
        [LocKey] public string DescriptionKey;
    }

    // In your editor code setup or manually:
    // itemData.descriptionKey = Loc.Keys.Inventory_Count;
}