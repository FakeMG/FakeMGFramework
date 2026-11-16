using UnityEngine;

namespace FakeMG.Localization.Examples
{
    public class ItemData : ScriptableObject
    {
        public string ItemID;

        [Header("Localization Key")]
        [LocKey] public string DescriptionKey;
    }

}