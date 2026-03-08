using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ES3Internal;
using ES3Types;
using FakeMG.SaveLoad.Advanced;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FakeMG.SaveLoad.Editor
{
    internal static class SaveFileViewerRawJsonPolicy
    {
        private static readonly HashSet<string> MetadataFieldNames = typeof(SaveMetadata)
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Select(field => field.Name)
            .ToHashSet(StringComparer.Ordinal);

        private static readonly HashSet<string> ReservedMetadataFieldNames = new(StringComparer.Ordinal)
        {
            ES3Type.typeFieldName,
            ES3ReferenceMgrBase.referencePropertyName
        };

        private const string VALUE_PROPERTY_NAME = "value";

        public static bool TryParseFullFileJson(string rawJson, out JObject rootObject, out string errorMessage)
        {
            rootObject = null;

            if (!TryParseJsonToken(rawJson, out JToken rootToken, out errorMessage))
            {
                return false;
            }

            rootObject = rootToken as JObject;
            if (rootObject != null)
            {
                return true;
            }

            errorMessage = "The save file must contain a JSON object at the root.";
            return false;
        }

        public static bool TryParseSingleKeyRawJson(
            string rawJson,
            out string keyName,
            out JToken valueToken,
            out string errorMessage)
        {
            keyName = null;
            valueToken = null;

            if (!TryParseFullFileJson(rawJson, out JObject keyObject, out errorMessage))
            {
                return false;
            }

            List<JProperty> properties = keyObject.Properties().ToList();
            if (properties.Count != 1)
            {
                errorMessage = "Key Raw must contain exactly one JSON property.";
                return false;
            }

            JProperty property = properties[0];
            keyName = property.Name;
            valueToken = property.Value?.DeepClone() ?? JValue.CreateNull();
            errorMessage = null;
            return true;
        }

        public static string CreateSingleKeyRawJson(JProperty property)
        {
            return CreateSingleKeyRawJson(property.Name, property.Value);
        }

        public static string CreateSingleKeyRawJson(string keyName, JToken valueToken)
        {
            JObject keyObject = new(new JProperty(keyName, valueToken?.DeepClone() ?? JValue.CreateNull()));
            return keyObject.ToString(Formatting.Indented);
        }

        public static bool TryUpdateKeyPropertyPreservingOrder(
            JObject rootObject,
            string currentKeyName,
            string updatedKeyName,
            JToken rawToken,
            out string errorMessage)
        {
            JProperty existingProperty = rootObject.Property(currentKeyName);
            if (existingProperty == null)
            {
                errorMessage = $"Could not locate key '{currentKeyName}' in the save file.";
                return false;
            }

            JProperty replacementProperty = new(
                updatedKeyName,
                rawToken?.DeepClone() ?? JValue.CreateNull());

            existingProperty.Replace(replacementProperty);
            errorMessage = null;
            return true;
        }

        public static bool TryValidateMetadataInRoot(JObject rootObject, out string errorMessage)
        {
            List<JProperty> metadataProperties = rootObject
                .Properties()
                .Where(property => string.Equals(property.Name, SaveFileCatalog.METADATA_KEY, StringComparison.Ordinal))
                .ToList();

            if (metadataProperties.Count == 0)
            {
                errorMessage = $"'{SaveFileCatalog.METADATA_KEY}' is required and cannot be deleted.";
                return false;
            }

            if (metadataProperties.Count > 1)
            {
                errorMessage = $"'{SaveFileCatalog.METADATA_KEY}' must appear exactly once in the save file.";
                return false;
            }

            return TryValidateMetadataToken(metadataProperties[0].Value, out errorMessage);
        }

        public static bool TryValidateMetadataToken(JToken metadataToken, out string errorMessage)
        {
            if (!TryGetMetadataFieldsObject(metadataToken, out JObject metadataFieldsObject, out errorMessage))
            {
                return false;
            }

            HashSet<string> encounteredFieldNames = new(StringComparer.Ordinal);
            List<JProperty> metadataProperties = metadataFieldsObject.Properties().ToList();

            for (int i = 0; i < metadataProperties.Count; i++)
            {
                string fieldName = metadataProperties[i].Name;

                if (ReservedMetadataFieldNames.Contains(fieldName))
                {
                    continue;
                }

                if (!encounteredFieldNames.Add(fieldName))
                {
                    errorMessage = $"Metadata field '{fieldName}' is duplicated. Metadata field names cannot be renamed, duplicated, added, or removed.";
                    return false;
                }

                if (!MetadataFieldNames.Contains(fieldName))
                {
                    errorMessage = $"Metadata field '{fieldName}' is not supported. Allowed fields: {string.Join(", ", MetadataFieldNames)}.";
                    return false;
                }
            }

            if (encounteredFieldNames.Count != MetadataFieldNames.Count)
            {
                List<string> missingFields = MetadataFieldNames
                    .Where(fieldName => !encounteredFieldNames.Contains(fieldName))
                    .ToList();
                errorMessage = $"Metadata is missing required fields: {string.Join(", ", missingFields)}.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private static bool TryGetMetadataFieldsObject(JToken metadataToken, out JObject metadataFieldsObject, out string errorMessage)
        {
            metadataFieldsObject = null;

            if (metadataToken is not JObject metadataObject)
            {
                errorMessage = $"'{SaveFileCatalog.METADATA_KEY}' must remain a JSON object so its field names stay intact.";
                return false;
            }

            JProperty valueProperty = metadataObject.Property(VALUE_PROPERTY_NAME, StringComparison.Ordinal);
            if (valueProperty == null)
            {
                metadataFieldsObject = metadataObject;
                errorMessage = null;
                return true;
            }

            List<string> unsupportedWrapperFields = metadataObject
                .Properties()
                .Select(property => property.Name)
                .Where(fieldName => !ReservedMetadataFieldNames.Contains(fieldName) && !string.Equals(fieldName, VALUE_PROPERTY_NAME, StringComparison.Ordinal))
                .ToList();

            if (unsupportedWrapperFields.Count > 0)
            {
                errorMessage = $"Metadata field '{unsupportedWrapperFields[0]}' is not supported. Allowed fields: {string.Join(", ", MetadataFieldNames)}.";
                return false;
            }

            if (valueProperty.Value is not JObject wrappedMetadataObject)
            {
                errorMessage = $"'{SaveFileCatalog.METADATA_KEY}.{VALUE_PROPERTY_NAME}' must remain a JSON object so its field names stay intact.";
                return false;
            }

            metadataFieldsObject = wrappedMetadataObject;
            errorMessage = null;
            return true;
        }

        private static bool TryParseJsonToken(string rawJson, out JToken parsedToken, out string errorMessage)
        {
            parsedToken = null;

            if (string.IsNullOrWhiteSpace(rawJson))
            {
                errorMessage = "Raw JSON cannot be empty.";
                return false;
            }

            try
            {
                parsedToken = JToken.Parse(rawJson);
                errorMessage = null;
                return true;
            }
            catch (JsonReaderException exception)
            {
                errorMessage = $"Invalid JSON: {exception.Message}";
                return false;
            }
        }
    }
}