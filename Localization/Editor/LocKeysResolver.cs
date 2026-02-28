using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FakeMG.Localization.Editor
{
    /// <summary>
    /// Resolves the generated Loc nested Keys types via reflection,
    /// allowing editor tools in this asmdef to work without
    /// a compile-time reference to Assembly-CSharp.
    /// </summary>
    public static class LocKeysResolver
    {
        private const string LOC_TYPE_NAME = "FakeMG.Localization.Loc";

        private static Type s_cachedLocType;
        private static Type[] s_cachedKeysTypes;
        private static bool s_hasSearched;

        public static Type FindLocType()
        {
            if (s_hasSearched)
                return s_cachedLocType;

            s_hasSearched = true;
            s_cachedLocType = AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetType(LOC_TYPE_NAME))
                .FirstOrDefault(type => type != null);

            return s_cachedLocType;
        }

        /// <summary>
        /// Returns all nested "Keys" types (one per table) inside the Loc class.
        /// e.g. Loc+UI+Keys, Loc+Gameplay+Keys
        /// </summary>
        public static Type[] FindAllKeysTypes()
        {
            if (s_cachedKeysTypes != null)
                return s_cachedKeysTypes;

            Type locType = FindLocType();
            if (locType == null)
            {
                s_cachedKeysTypes = Array.Empty<Type>();
                return s_cachedKeysTypes;
            }

            var keysTypes = new List<Type>();

            foreach (Type tableType in locType.GetNestedTypes(BindingFlags.Public | BindingFlags.Static))
            {
                Type keysType = tableType.GetNestedType("Keys", BindingFlags.Public | BindingFlags.Static);
                if (keysType != null)
                    keysTypes.Add(keysType);
            }

            s_cachedKeysTypes = keysTypes.ToArray();
            return s_cachedKeysTypes;
        }

        /// <summary>
        /// Backward-compatible: returns the first Keys type found (or null).
        /// Prefer <see cref="FindAllKeysTypes"/> for multi-table scenarios.
        /// </summary>
        public static Type FindKeysType()
        {
            var types = FindAllKeysTypes();
            return types.Length > 0 ? types[0] : null;
        }

        /// <summary>
        /// Returns all key fields across every table's Keys class.
        /// </summary>
        public static FieldInfo[] GetKeyFields()
        {
            var allKeysTypes = FindAllKeysTypes();
            if (allKeysTypes.Length == 0)
                return Array.Empty<FieldInfo>();

            return allKeysTypes
                .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
                .ToArray();
        }

        /// <summary>
        /// Returns key fields grouped by table class name for use in grouped dropdowns.
        /// Key = table class name (e.g. "UI"), Value = fields for that table's Keys class.
        /// </summary>
        public static Dictionary<string, FieldInfo[]> GetKeyFieldsGrouped()
        {
            var allKeysTypes = FindAllKeysTypes();
            var grouped = new Dictionary<string, FieldInfo[]>();

            foreach (Type keysType in allKeysTypes)
            {
                // keysType.DeclaringType is the table class (e.g. Loc+UI)
                string tableName = keysType.DeclaringType?.Name ?? "Unknown";
                var fields = keysType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                grouped[tableName] = fields;
            }

            return grouped;
        }
    }
}
