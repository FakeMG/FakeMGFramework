using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace FakeMG.SaveLoad.Editor
{
    internal sealed class SaveFileViewerTypeCatalog
    {
        public static SaveFileViewerTypeCatalog Default { get; } = new();

        private readonly List<Type> _creatableTypes;

        private SaveFileViewerTypeCatalog()
        {
            _creatableTypes = BuildCreatableTypes();
        }

        public Type ResolveSupportedType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return null;
            }

            for (int i = 0; i < _creatableTypes.Count; i++)
            {
                Type candidate = _creatableTypes[i];
                if (string.Equals(candidate.FullName, typeName, StringComparison.Ordinal)
                    || string.Equals(candidate.AssemblyQualifiedName, typeName, StringComparison.Ordinal)
                    || string.Equals(candidate.Name, typeName, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            return null;
        }

        public List<Type> GetMatches(string search, int maxMatches)
        {
            IEnumerable<Type> matches = _creatableTypes;
            if (!string.IsNullOrWhiteSpace(search))
            {
                matches = matches.Where(type =>
                    type.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || type.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            return matches
                .Take(maxMatches)
                .ToList();
        }

        public string GetDisplayName(Type type)
        {
            return type == null ? string.Empty : $"{type.FullName} ({type.Assembly.GetName().Name})";
        }

        private static List<Type> BuildCreatableTypes()
        {
            List<Type> types = new();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Assembly assembly = assemblies[i];
                if (assembly.IsDynamic || IsEditorAssembly(assembly))
                {
                    continue;
                }

                Type[] assemblyTypes = GetLoadableTypes(assembly);
                for (int typeIndex = 0; typeIndex < assemblyTypes.Length; typeIndex++)
                {
                    Type type = assemblyTypes[typeIndex];
                    if (!IsSupportedNewKeyType(type))
                    {
                        continue;
                    }

                    types.Add(type);
                }
            }

            return types
                .Distinct()
                .OrderBy(type => type.FullName)
                .ToList();
        }

        private static Type[] GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(type => type != null).ToArray();
            }
        }

        private static bool IsEditorAssembly(Assembly assembly)
        {
            string assemblyName = assembly.GetName().Name;
            return assemblyName.Contains("Editor", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSupportedNewKeyType(Type type)
        {
            if (type == null)
            {
                return false;
            }

            if (type.IsAbstract || type.IsInterface)
            {
                return false;
            }

            if (type.ContainsGenericParameters)
            {
                return false;
            }

            if (type.IsPointer || type.IsByRef)
            {
                return false;
            }

            if (typeof(Delegate).IsAssignableFrom(type))
            {
                return false;
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                return false;
            }

            if (type.FullName == null)
            {
                return false;
            }

            if (type.FullName.StartsWith("<", StringComparison.Ordinal))
            {
                return false;
            }

            return CanCreateDefaultValue(type);
        }

        private static bool CanCreateDefaultValue(Type type)
        {
            if (type == typeof(string))
            {
                return true;
            }

            if (type.IsArray)
            {
                return type.GetElementType() != null;
            }

            if (type.IsValueType)
            {
                return true;
            }

            return type.GetConstructor(Type.EmptyTypes) != null;
        }
    }
}