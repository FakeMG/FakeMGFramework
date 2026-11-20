using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FakeMG.InputHints.Editor
{
    /// <remarks>A new pack only needs its own <see cref="InputGlyphPack"/> table.</remarks>
    internal static class InputGlyphSetBuilder
    {
        private const string OUTPUT_FOLDER_NAME = "ScriptableObjects";
        private const string ATLAS_FOLDER_NAME = "SpriteAtlases";
        private const string ATLAS_EXTENSION = ".spriteatlasv2";

        private static string SelectedFolder => AssetDatabase.GetAssetPath(Selection.activeObject);

        #region Public Methods

        /// <param name="texturesFolder">Project folder holding the pack's family folders.</param>
        /// <exception cref="InvalidOperationException">
        /// A family folder, glyph or diagram is missing. Only the pack's prepared art, if any, was written.
        /// </exception>
        public static InputGlyphLibrarySO BuildAll(string texturesFolder, InputGlyphPack pack)
        {
            pack.PrepareTexturesFolder?.Invoke(texturesFolder);
            ThrowIfPackIsIncomplete(texturesFolder, pack);
            string outputFolder = EnsureSiblingFolder(texturesFolder, OUTPUT_FOLDER_NAME);
            var glyphSetSOs = new List<InputGlyphSetSO>();
            var usedSpritesByFamilyFolder = new Dictionary<string, HashSet<Sprite>>();
            foreach (InputGlyphFamily family in pack.Families)
            {
                string familyFolder = $"{texturesFolder}/{family.TextureFolderName}";
                ImportAsSprites(familyFolder);
                InputGlyphSetSO glyphSetSO = LoadOrCreate<InputGlyphSetSO>($"{outputFolder}/{family.GlyphSetAssetName}.asset");
                Sprite diagram = LoadDiagram(texturesFolder, family);
                FillGlyphSet(glyphSetSO, family, familyFolder, diagram, GetOrAddUsedSprites(usedSpritesByFamilyFolder, family.TextureFolderName));
                glyphSetSOs.Add(glyphSetSO);
            }

            PackAtlases(texturesFolder, usedSpritesByFamilyFolder);

            InputGlyphLibrarySO librarySO = LoadOrCreate<InputGlyphLibrarySO>($"{outputFolder}/{pack.LibraryAssetName}.asset");
            FillLibrary(librarySO, glyphSetSOs);
            AssetDatabase.SaveAssets();
            return librarySO;
        }

        public static void BuildFromSelectedFolder(InputGlyphPack pack)
        {
            InputGlyphLibrarySO librarySO = BuildAll(SelectedFolder, pack);
            EditorGUIUtility.PingObject(librarySO);
        }

        public static bool IsFolderSelected()
        {
            return Selection.activeObject && AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject));
        }

        #endregion

        #region Private Methods

        private static void ThrowIfPackIsIncomplete(string texturesFolder, InputGlyphPack pack)
        {
            var problems = new List<string>();
            foreach (InputGlyphFamily family in pack.Families)
            {
                string familyFolder = $"{texturesFolder}/{family.TextureFolderName}";
                if (!AssetDatabase.IsValidFolder(familyFolder))
                {
                    problems.Add($"Folder '{familyFolder}' for '{family.GlyphSetAssetName}' does not exist.");
                    continue;
                }

                string diagramFolder = family.HasDiagram ? DiagramFolderOf(texturesFolder, family) : null;
                if (family.HasDiagram && (!AssetDatabase.IsValidFolder(diagramFolder) ||
                    !LoadTextureNames(diagramFolder).Contains(Path.GetFileNameWithoutExtension(family.DiagramFilePath))))
                {
                    problems.Add($"Diagram '{family.DiagramFilePath}' for '{family.GlyphSetAssetName}' is missing from '{texturesFolder}'.");
                }

                HashSet<string> textureNames = LoadTextureNames(familyFolder);
                foreach ((string controlPath, string glyphFileName) in family.GlyphFileNamesByControlPath)
                {
                    if (!textureNames.Contains(glyphFileName))
                    {
                        problems.Add($"Glyph '{glyphFileName}' for '{controlPath}' is missing from '{familyFolder}'.");
                    }
                }
            }

            if (problems.Count > 0)
            {
                throw new InvalidOperationException(
                    $"{pack.DisplayName} glyph sets were not built, as the pack is incomplete:\n{string.Join("\n", problems)}");
            }
        }

        /// <returns>Null when the family has no diagram.</returns>
        private static Sprite LoadDiagram(string texturesFolder, InputGlyphFamily family)
        {
            if (!family.HasDiagram)
            {
                return null;
            }

            string diagramFolder = DiagramFolderOf(texturesFolder, family);
            ImportAsSprites(diagramFolder);
            return LoadSpritesByName(diagramFolder)[Path.GetFileNameWithoutExtension(family.DiagramFilePath)];
        }

        private static string DiagramFolderOf(string texturesFolder, InputGlyphFamily family)
        {
            string relativeFolder = Path.GetDirectoryName(family.DiagramFilePath)!.Replace('\\', '/');
            return $"{texturesFolder}/{relativeFolder}";
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

        /// <param name="usedSprites">Receives every glyph the set draws, for packing. The diagram is left out: it is large
        /// and shown one at a time, so it would only bloat the atlas.</param>
        private static void FillGlyphSet(
            InputGlyphSetSO glyphSetSO,
            InputGlyphFamily family,
            string familyFolder,
            Sprite diagram,
            HashSet<Sprite> usedSprites)
        {
            Dictionary<string, Sprite> spritesByName = LoadSpritesByName(familyFolder);
            var serializedSet = new SerializedObject(glyphSetSO);

            SerializedProperty layouts = FindPropertyOrThrow(serializedSet, "_deviceLayouts");
            layouts.arraySize = family.DeviceLayouts.Length;
            for (int i = 0; i < family.DeviceLayouts.Length; i++)
            {
                layouts.GetArrayElementAtIndex(i).stringValue = family.DeviceLayouts[i];
            }

            FillDiagram(serializedSet, diagram, family.DiagramHotspots);

            SerializedProperty glyphs = FindPropertyOrThrow(serializedSet, "_glyphs");
            glyphs.arraySize = 0;
            foreach ((string controlPath, string glyphFileName) in family.GlyphFileNamesByControlPath)
            {
                Sprite sprite = spritesByName[glyphFileName];
                glyphs.InsertArrayElementAtIndex(glyphs.arraySize);
                SerializedProperty glyph = glyphs.GetArrayElementAtIndex(glyphs.arraySize - 1);
                glyph.FindPropertyRelative("_controlPath").stringValue = controlPath;
                glyph.FindPropertyRelative("_sprite").objectReferenceValue = sprite;
                usedSprites.Add(sprite);
            }

            serializedSet.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void FillDiagram(SerializedObject serializedSet, Sprite diagram, IReadOnlyList<DiagramHotspotPixels> hotspotPixels)
        {
            FindPropertyOrThrow(serializedSet, "_deviceDiagram").objectReferenceValue = diagram;
            InputGlyphDiagramMasks masks = diagram
                ? InputGlyphDiagramMaskBuilder.BuildAndSaveMasks(AssetDatabase.GetAssetPath(diagram), hotspotPixels)
                : new InputGlyphDiagramMasks(null, default, Array.Empty<InputGlyphDiagramHotspot>());
            FindPropertyOrThrow(serializedSet, "_diagramMasks").objectReferenceValue = masks.Masks;
            FindPropertyOrThrow(serializedSet, "_diagramFillUvRect").rectValue = masks.FillUvRect;

            SerializedProperty hotspots = FindPropertyOrThrow(serializedSet, "_diagramHotspots");
            hotspots.arraySize = masks.Hotspots.Count;
            for (int i = 0; i < masks.Hotspots.Count; i++)
            {
                InputGlyphDiagramHotspot hotspot = masks.Hotspots[i];
                SerializedProperty hotspotProperty = hotspots.GetArrayElementAtIndex(i);
                hotspotProperty.FindPropertyRelative("_controlPath").stringValue = hotspot.ControlPath;
                hotspotProperty.FindPropertyRelative("_center01").vector2Value = (hotspot.AnchorMin01 + hotspot.AnchorMax01) / 2f;
                hotspotProperty.FindPropertyRelative("_size01").vector2Value = hotspot.AnchorMax01 - hotspot.AnchorMin01;
                hotspotProperty.FindPropertyRelative("_maskUvRect").rectValue = hotspot.MaskUvRect;
            }
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
