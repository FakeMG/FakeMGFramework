using System;
using System.Collections.Generic;
using ES3Internal;
using FakeMG.Settings;
using FakeMG.Settings.Converters;
using UnityEngine.Scripting;

namespace ES3Types
{
    [Preserve]
    public class ES3Type_SettingDataSnapshot : ES3Type
    {
        private const string VALUE_TYPES_PROPERTY_NAME = "__types";

        public static ES3Type Instance = new ES3Type_SettingDataSnapshot();

        public ES3Type_SettingDataSnapshot() : base(typeof(SettingDataSnapshot))
        {
            Instance = this;
        }

        public override void Write(object obj, ES3Writer writer)
        {
            SettingDataSnapshot snapshot = (SettingDataSnapshot)obj;

            writer.WriteProperty(VALUE_TYPES_PROPERTY_NAME, snapshot.ValueTypes);

            foreach (KeyValuePair<string, string> entry in snapshot.Values)
            {
                if (!snapshot.ValueTypes.TryGetValue(entry.Key, out string typeId))
                {
                    continue;
                }

                if (!SettingValueConverterRegistry.TryResolveType(typeId, out Type valueType))
                {
                    continue;
                }

                if (!SettingValueConverterRegistry.TryDeserialize(valueType, entry.Value, out object value))
                {
                    continue;
                }

                writer.WriteProperty(entry.Key, value, ES3TypeMgr.GetOrCreateES3Type(valueType));
            }
        }

        public override object Read<T>(ES3Reader reader)
        {
            SettingDataSnapshot snapshot = new();

            foreach (string propertyName in reader.Properties)
            {
                if (propertyName == VALUE_TYPES_PROPERTY_NAME)
                {
                    snapshot.ValueTypes = reader.Read<Dictionary<string, string>>();
                    continue;
                }

                if (!TryReadSerializedValue(reader, snapshot, propertyName, out string serializedValue))
                {
                    reader.Skip();
                    continue;
                }

                snapshot.Values[propertyName] = serializedValue;
            }

            return snapshot;
        }

        private static bool TryReadSerializedValue(
            ES3Reader reader,
            SettingDataSnapshot snapshot,
            string propertyName,
            out string serializedValue)
        {
            serializedValue = null;

            if (snapshot.ValueTypes == null)
            {
                return false;
            }

            if (!snapshot.ValueTypes.TryGetValue(propertyName, out string typeId))
            {
                return false;
            }

            if (!SettingValueConverterRegistry.TryResolveType(typeId, out Type valueType))
            {
                return false;
            }

            object rawValue = reader.Read<object>(ES3TypeMgr.GetOrCreateES3Type(valueType));
            return SettingValueConverterRegistry.TrySerialize(valueType, rawValue, out serializedValue);
        }
    }
}