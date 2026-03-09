using System;
using System.Collections.Generic;
using System.Reflection;

namespace FakeMG.Settings.Converters
{
    public static class SettingValueConverterRegistry
    {
        private const string ENUM_TYPE_ID_PREFIX = "enum:";

        private static readonly Dictionary<Type, ISettingValueConverter> _converters = new();
        private static readonly Dictionary<string, Type> _typeIds = new(StringComparer.Ordinal);
        private static readonly object _initializationLock = new();
        private static bool _isInitialized;

        static SettingValueConverterRegistry()
        {
            EnsureInitialized();
        }

        public static void Register<T>(SettingValueConverter<T> converter)
        {
            Register((ISettingValueConverter)converter);
        }

        public static void Register(ISettingValueConverter converter)
        {
            if (converter == null)
            {
                throw new ArgumentNullException(nameof(converter));
            }

            _converters[converter.ValueType] = converter;
            _typeIds[converter.TypeId] = converter.ValueType;
        }

        public static bool TrySerialize(Type valueType, object value, out string serializedValue)
        {
            EnsureInitialized();

            if (TryGetConverter(valueType, out ISettingValueConverter converter))
            {
                serializedValue = converter.Serialize(value);
                return true;
            }

            serializedValue = null;
            return false;
        }

        public static bool TryDeserialize<T>(string serializedValue, out T value)
        {
            EnsureInitialized();

            if (TryGetConverter(typeof(T), out ISettingValueConverter converter) &&
                converter.TryDeserialize(serializedValue, out object deserializedValue) &&
                deserializedValue is T typedValue)
            {
                value = typedValue;
                return true;
            }

            value = default;
            return false;
        }

        public static bool TryDeserialize(Type valueType, string serializedValue, out object value)
        {
            EnsureInitialized();

            if (TryGetConverter(valueType, out ISettingValueConverter converter))
            {
                return converter.TryDeserialize(serializedValue, out value);
            }

            value = null;
            return false;
        }

        public static bool TryGetTypeId(Type valueType, out string typeId)
        {
            EnsureInitialized();

            if (TryGetConverter(valueType, out ISettingValueConverter converter))
            {
                typeId = converter.TypeId;
                return true;
            }

            if (valueType.IsEnum)
            {
                typeId = BuildEnumTypeId(valueType);
                return true;
            }

            typeId = null;
            return false;
        }

        public static bool TryResolveType(string typeId, out Type valueType)
        {
            EnsureInitialized();

            if (_typeIds.TryGetValue(typeId, out valueType))
            {
                return true;
            }

            if (typeId.StartsWith(ENUM_TYPE_ID_PREFIX, StringComparison.Ordinal))
            {
                string enumTypeName = typeId.Substring(ENUM_TYPE_ID_PREFIX.Length);
                valueType = Type.GetType(enumTypeName);

                if (valueType != null)
                {
                    _typeIds[typeId] = valueType;
                    return true;
                }

                return false;
            }

            valueType = null;
            return false;
        }

        private static void EnsureInitialized()
        {
            if (_isInitialized)
            {
                return;
            }

            lock (_initializationLock)
            {
                if (_isInitialized)
                {
                    return;
                }

                DiscoverConverters();
                _isInitialized = true;
            }
        }

        private static void DiscoverConverters()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!ShouldScanAssembly(assembly))
                {
                    continue;
                }

                foreach (Type converterType in GetLoadableTypes(assembly))
                {
                    if (!CanInstantiateConverter(converterType))
                    {
                        continue;
                    }

                    if (Activator.CreateInstance(converterType, true) is ISettingValueConverter converter)
                    {
                        Register(converter);
                    }
                }
            }
        }

        private static bool ShouldScanAssembly(Assembly assembly)
        {
            if (assembly == null || assembly.IsDynamic)
            {
                return false;
            }

            string assemblyName = assembly.GetName().Name;

            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                return false;
            }

            return !assemblyName.StartsWith("System", StringComparison.Ordinal) &&
                   !assemblyName.StartsWith("mscorlib", StringComparison.Ordinal) &&
                   !assemblyName.StartsWith("netstandard", StringComparison.Ordinal) &&
                   !assemblyName.StartsWith("Mono.", StringComparison.Ordinal) &&
                   !assemblyName.StartsWith("Unity", StringComparison.Ordinal) &&
                   !assemblyName.StartsWith("Microsoft", StringComparison.Ordinal) &&
                   !assemblyName.StartsWith("nunit", StringComparison.OrdinalIgnoreCase);
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types;
            }
        }

        private static bool CanInstantiateConverter(Type converterType)
        {
            if (converterType == null ||
                converterType.IsAbstract ||
                converterType.IsInterface ||
                converterType.IsGenericTypeDefinition ||
                !typeof(ISettingValueConverter).IsAssignableFrom(converterType))
            {
                return false;
            }

            if (converterType == typeof(EnumSettingValueConverter))
            {
                return false;
            }

            ConstructorInfo constructor = converterType.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null);

            return constructor != null;
        }

        private static bool TryGetConverter(Type valueType, out ISettingValueConverter converter)
        {
            if (_converters.TryGetValue(valueType, out converter))
            {
                return true;
            }

            if (valueType.IsEnum)
            {
                converter = new EnumSettingValueConverter(valueType);
                _converters[valueType] = converter;
                _typeIds[converter.TypeId] = valueType;
                return true;
            }

            return false;
        }

        private static string BuildEnumTypeId(Type enumType)
        {
            return $"{ENUM_TYPE_ID_PREFIX}{enumType.AssemblyQualifiedName}";
        }

        private sealed class EnumSettingValueConverter : ISettingValueConverter
        {
            private readonly Type _enumType;

            public EnumSettingValueConverter(Type enumType)
            {
                _enumType = enumType;
            }

            public Type ValueType => _enumType;
            public string TypeId => BuildEnumTypeId(_enumType);

            public string Serialize(object value)
            {
                return value.ToString();
            }

            public bool TryDeserialize(string serializedValue, out object value)
            {
                if (Enum.IsDefined(_enumType, serializedValue))
                {
                    value = Enum.Parse(_enumType, serializedValue);
                    return true;
                }

                try
                {
                    value = Enum.Parse(_enumType, serializedValue, true);
                    return true;
                }
                catch (ArgumentException)
                {
                    value = null;
                    return false;
                }
            }
        }
    }
}