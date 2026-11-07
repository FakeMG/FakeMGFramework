using System;
using System.Collections.Generic;
using System.IO;
using FakeMG.Framework;
using UnityEditor;
using UnityEngine;

namespace FakeMG.InputHints.Editor
{
    /// <summary>
    /// Builds glyph sets and a glyph library from a copy of the Kenney Input Prompts pack. Select the folder holding the
    /// pack's family folders ("Xbox Series", "Keyboard &amp; Mouse"...) and run the menu item. The assets go to a
    /// ScriptableObjects folder beside it, with one sprite atlas per family folder in a SpriteAtlases folder. Running it
    /// again updates them in place, keeping references intact. Nothing is written unless every family folder and glyph the
    /// tables name is found, so a rebuild never drops glyphs from the library silently.
    /// </summary>
    public static class KenneyInputGlyphSetBuilder
    {
        private const string LIBRARY_ASSET_NAME = "Kenney Input Glyph Library SO";
        private const string OUTPUT_FOLDER_NAME = "ScriptableObjects";
        private const string ATLAS_FOLDER_NAME = "SpriteAtlases";
        private const string ATLAS_EXTENSION = ".spriteatlasv2";

        #region Public Methods

        /// <param name="texturesFolder">Project folder holding the pack's family folders.</param>
        /// <exception cref="InvalidOperationException">A family folder or glyph is missing; nothing was written.</exception>
        public static InputGlyphLibrarySO BuildAll(string texturesFolder)
        {
            ThrowIfPackIsIncomplete(texturesFolder);
            string outputFolder = EnsureSiblingFolder(texturesFolder, OUTPUT_FOLDER_NAME);
            var glyphSetSOs = new List<InputGlyphSetSO>();
            var usedSpritesByFamilyFolder = new Dictionary<string, HashSet<Sprite>>();
            foreach (KenneyGlyphFamily family in KenneyGlyphFamilies.LIBRARY_ORDER)
            {
                string familyFolder = $"{texturesFolder}/{family.TextureFolderName}";
                ImportAsSprites(familyFolder);
                InputGlyphSetSO glyphSetSO = LoadOrCreate<InputGlyphSetSO>($"{outputFolder}/{family.GlyphSetAssetName}.asset");
                FillGlyphSet(glyphSetSO, family, familyFolder, GetOrAddUsedSprites(usedSpritesByFamilyFolder, family.TextureFolderName));
                glyphSetSOs.Add(glyphSetSO);
            }

            PackAtlases(texturesFolder, usedSpritesByFamilyFolder);

            InputGlyphLibrarySO librarySO = LoadOrCreate<InputGlyphLibrarySO>($"{outputFolder}/{LIBRARY_ASSET_NAME}.asset");
            FillLibrary(librarySO, glyphSetSOs);
            AssetDatabase.SaveAssets();
            return librarySO;
        }

        #endregion

        #region Private Methods

        [MenuItem(FakeMGEditorMenus.INPUT_HINTS_BUILD_KENNEY_GLYPH_SETS)]
        private static void BuildFromSelectedFolder()
        {
            InputGlyphLibrarySO librarySO = BuildAll(AssetDatabase.GetAssetPath(Selection.activeObject));
            EditorGUIUtility.PingObject(librarySO);
        }

        [MenuItem(FakeMGEditorMenus.INPUT_HINTS_BUILD_KENNEY_GLYPH_SETS, true)]
        private static bool IsFolderSelected()
        {
            return Selection.activeObject && AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject));
        }

        private static void ThrowIfPackIsIncomplete(string texturesFolder)
        {
            var problems = new List<string>();
            foreach (KenneyGlyphFamily family in KenneyGlyphFamilies.LIBRARY_ORDER)
            {
                string familyFolder = $"{texturesFolder}/{family.TextureFolderName}";
                if (!AssetDatabase.IsValidFolder(familyFolder))
                {
                    problems.Add($"Folder '{familyFolder}' for '{family.GlyphSetAssetName}' does not exist.");
                    continue;
                }

                HashSet<string> textureNames = LoadTextureNames(familyFolder);
                foreach ((string controlPath, string iconName) in family.GlyphFileNamesByControlPath)
                {
                    if (!textureNames.Contains(iconName))
                    {
                        problems.Add($"Glyph '{iconName}' for '{controlPath}' is missing from '{familyFolder}'.");
                    }
                }
            }

            if (problems.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Kenney glyph sets were not built, as the pack is incomplete:\n{string.Join("\n", problems)}");
            }
        }

        private static HashSet<string> LoadTextureNames(string folder)
        {
            var textureNames = new HashSet<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                textureNames.Add(Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid)));
            }

            return textureNames;
        }

        private static string EnsureSiblingFolder(string texturesFolder, string folderName)
        {
            string parentFolder = Path.GetDirectoryName(texturesFolder)!.Replace('\\', '/');
            string siblingFolder = $"{parentFolder}/{folderName}";
            if (!AssetDatabase.IsValidFolder(siblingFolder))
            {
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }

            return siblingFolder;
        }

        /// <remarks>Families sharing a texture folder, such as PS4 and PS5, share one atlas: a sprite belongs to one atlas only.</remarks>
        private static HashSet<Sprite> GetOrAddUsedSprites(Dictionary<string, HashSet<Sprite>> usedSpritesByFamilyFolder, string textureFolderName)
        {
            if (!usedSpritesByFamilyFolder.TryGetValue(textureFolderName, out HashSet<Sprite> usedSprites))
            {
                usedSprites = new HashSet<Sprite>();
                usedSpritesByFamilyFolder.Add(textureFolderName, usedSprites);
            }

            return usedSprites;
        }

        private static void ImportAsSprites(string folder)
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
                {
                    var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                    if (importer.textureType == TextureImporterType.Sprite)
                    {
                        continue;
                    }

                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.mipmapEnabled = false;
                    importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        private static T LoadOrCreate<T>(string assetPath) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        /// <param name="usedSprites">Receives every sprite the set draws, for packing.</param>
        private static void FillGlyphSet(InputGlyphSetSO glyphSetSO, KenneyGlyphFamily family, string familyFolder, HashSet<Sprite> usedSprites)
        {
            Dictionary<string, Sprite> spritesByName = LoadSpritesByName(familyFolder);
            var serializedSet = new SerializedObject(glyphSetSO);

            SerializedProperty layouts = FindPropertyOrThrow(serializedSet, "_deviceLayouts");
            layouts.arraySize = family.DeviceLayouts.Length;
            for (int i = 0; i < family.DeviceLayouts.Length; i++)
            {
                layouts.GetArrayElementAtIndex(i).stringValue = family.DeviceLayouts[i];
            }

            SerializedProperty glyphs = FindPropertyOrThrow(serializedSet, "_glyphs");
            glyphs.arraySize = 0;
            foreach ((string controlPath, string iconName) in family.GlyphFileNamesByControlPath)
            {
                Sprite sprite = spritesByName[iconName];
                glyphs.InsertArrayElementAtIndex(glyphs.arraySize);
                SerializedProperty glyph = glyphs.GetArrayElementAtIndex(glyphs.arraySize - 1);
                glyph.FindPropertyRelative("_controlPath").stringValue = controlPath;
                glyph.FindPropertyRelative("_sprite").objectReferenceValue = sprite;
                usedSprites.Add(sprite);
            }

            serializedSet.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Dictionary<string, Sprite> LoadSpritesByName(string folder)
        {
            var spritesByName = new Dictionary<string, Sprite>();
            foreach (string guid in AssetDatabase.FindAssets("t:Sprite", new[] { folder }))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid));
                spritesByName[sprite.name] = sprite;
            }

            return spritesByName;
        }

        private static void FillLibrary(InputGlyphLibrarySO librarySO, List<InputGlyphSetSO> glyphSetSOs)
        {
            var serializedLibrary = new SerializedObject(librarySO);
            SerializedProperty sets = FindPropertyOrThrow(serializedLibrary, "_glyphSetSOs");
            sets.arraySize = glyphSetSOs.Count;
            for (int i = 0; i < glyphSetSOs.Count; i++)
            {
                sets.GetArrayElementAtIndex(i).objectReferenceValue = glyphSetSOs[i];
            }

            serializedLibrary.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <remarks>The field names are private to the runtime classes; renaming one there must fail here, not write nothing.</remarks>
        private static SerializedProperty FindPropertyOrThrow(SerializedObject serializedObject, string propertyPath)
        {
            return serializedObject.FindProperty(propertyPath)
                ?? throw new InvalidOperationException($"{serializedObject.targetObject.GetType().Name} has no field '{propertyPath}'.");
        }

        private static void PackAtlases(string texturesFolder, Dictionary<string, HashSet<Sprite>> usedSpritesByFamilyFolder)
        {
            string atlasFolder = EnsureSiblingFolder(texturesFolder, ATLAS_FOLDER_NAME);
            foreach ((string textureFolderName, HashSet<Sprite> usedSprites) in usedSpritesByFamilyFolder)
            {
                InputGlyphAtlasPacker.Pack($"{atlasFolder}/{textureFolderName} Glyphs{ATLAS_EXTENSION}", usedSprites);
            }
        }

        #endregion
    }
}
