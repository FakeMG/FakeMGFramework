using System.Collections.Generic;
using System.IO;
using FakeMG.Framework;
using UnityEditor;
using UnityEngine;

namespace FakeMG.Audio.Editor
{
    public static class AudioCueSOAssetCreator
    {
        private const int CREATE_AUDIO_CUE_MENU_PRIORITY = 210;

        private const string AUDIO_CLIP_GROUPS_PROPERTY_NAME = "_audioClipGroups";
        private const string AUDIO_CLIPS_PROPERTY_NAME = "AudioClips";
        private const string SEQUENCE_MODE_PROPERTY_NAME = "_sequenceMode";

        private const string DEFAULT_ASSET_DIRECTORY = "Assets";
        private const string AUDIO_CUE_ASSET_SUFFIX = " AudioCueSO";
        private const string ASSET_EXTENSION = ".asset";
        private const string WINDOWS_DIRECTORY_SEPARATOR = "\\";
        private const string UNITY_DIRECTORY_SEPARATOR = "/";

        [MenuItem(FakeMGEditorMenus.AUDIO_CREATE_AUDIO_CUE_FROM_SELECTED_CLIPS, false, CREATE_AUDIO_CUE_MENU_PRIORITY)]
        private static void CreateAudioCueFromSelectedClips()
        {
            List<AudioClip> selectedClips = GetSelectedAudioClips();
            if (selectedClips.Count == 0)
            {
                return;
            }

            List<AudioClip> orderedClips = OrderClipsByPath(selectedClips);
            AudioCueSO createdAudioCue = ScriptableObject.CreateInstance<AudioCueSO>();

            ConfigureAudioCueWithSingleGroup(createdAudioCue, orderedClips);
            string createdAssetPath = CreateAudioCueAsset(createdAudioCue, orderedClips[0]);

            SelectCreatedAsset(createdAudioCue, createdAssetPath);
        }

        [MenuItem(FakeMGEditorMenus.AUDIO_CREATE_AUDIO_CUE_FROM_SELECTED_CLIPS, true)]
        private static bool ValidateCreateAudioCueFromSelectedClips()
        {
            if (Selection.objects.Length == 0)
            {
                return false;
            }

            List<AudioClip> selectedClips = GetSelectedAudioClips();
            bool allSelectedAssetsAreAudioClips = selectedClips.Count == Selection.objects.Length;

            return allSelectedAssetsAreAudioClips;
        }

        private static List<AudioClip> GetSelectedAudioClips()
        {
            Object[] selectedAssets = Selection.GetFiltered(typeof(AudioClip), SelectionMode.Assets);
            List<AudioClip> selectedClips = new List<AudioClip>(selectedAssets.Length);

            for (int i = 0; i < selectedAssets.Length; i++)
            {
                if (selectedAssets[i] is AudioClip clip)
                {
                    selectedClips.Add(clip);
                }
            }

            return selectedClips;
        }

        private static List<AudioClip> OrderClipsByPath(List<AudioClip> selectedClips)
        {
            List<AudioClip> orderedClips = new List<AudioClip>(selectedClips);
            orderedClips.Sort(CompareAudioClipPath);

            return orderedClips;
        }

        private static int CompareAudioClipPath(AudioClip leftClip, AudioClip rightClip)
        {
            string leftPath = AssetDatabase.GetAssetPath(leftClip);
            string rightPath = AssetDatabase.GetAssetPath(rightClip);

            return string.CompareOrdinal(leftPath, rightPath);
        }

        private static void ConfigureAudioCueWithSingleGroup(AudioCueSO audioCue, List<AudioClip> orderedClips)
        {
            SerializedObject serializedAudioCue = new SerializedObject(audioCue);
            SerializedProperty audioClipGroupsProperty = serializedAudioCue.FindProperty(AUDIO_CLIP_GROUPS_PROPERTY_NAME);

            audioClipGroupsProperty.arraySize = 1;
            SerializedProperty firstGroupProperty = audioClipGroupsProperty.GetArrayElementAtIndex(0);

            ApplyDefaultSequenceMode(firstGroupProperty);
            FillGroupWithSelectedClips(firstGroupProperty, orderedClips);

            serializedAudioCue.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ApplyDefaultSequenceMode(SerializedProperty groupProperty)
        {
            SerializedProperty sequenceModeProperty = groupProperty.FindPropertyRelative(SEQUENCE_MODE_PROPERTY_NAME);
            int defaultSequenceMode = (int)AudioClipsGroup.SequenceMode.RandomNoImmediateRepeat;

            sequenceModeProperty.intValue = defaultSequenceMode;
        }

        private static void FillGroupWithSelectedClips(SerializedProperty groupProperty, List<AudioClip> orderedClips)
        {
            SerializedProperty audioClipsProperty = groupProperty.FindPropertyRelative(AUDIO_CLIPS_PROPERTY_NAME);
            audioClipsProperty.arraySize = orderedClips.Count;

            for (int i = 0; i < orderedClips.Count; i++)
            {
                SerializedProperty clipProperty = audioClipsProperty.GetArrayElementAtIndex(i);
                clipProperty.objectReferenceValue = orderedClips[i];
            }
        }

        private static string CreateAudioCueAsset(AudioCueSO audioCue, AudioClip firstSelectedClip)
        {
            string targetDirectoryPath = GetDirectoryPath(firstSelectedClip);
            string suggestedAssetFileName = BuildAssetFileName(firstSelectedClip.name);
            string desiredAssetPath = NormalizePath(Path.Combine(targetDirectoryPath, suggestedAssetFileName));
            string uniqueAssetPath = AssetDatabase.GenerateUniqueAssetPath(desiredAssetPath);

            AssetDatabase.CreateAsset(audioCue, uniqueAssetPath);
            AssetDatabase.SaveAssets();

            return uniqueAssetPath;
        }

        private static string GetDirectoryPath(AudioClip referenceClip)
        {
            string clipPath = AssetDatabase.GetAssetPath(referenceClip);
            string directoryPath = Path.GetDirectoryName(clipPath);

            if (string.IsNullOrEmpty(directoryPath))
            {
                return DEFAULT_ASSET_DIRECTORY;
            }

            return NormalizePath(directoryPath);
        }

        private static string BuildAssetFileName(string clipName)
        {
            string assetFileName = clipName + AUDIO_CUE_ASSET_SUFFIX + ASSET_EXTENSION;

            return assetFileName;
        }

        private static string NormalizePath(string path)
        {
            string normalizedPath = path.Replace(WINDOWS_DIRECTORY_SEPARATOR, UNITY_DIRECTORY_SEPARATOR);

            return normalizedPath;
        }

        private static void SelectCreatedAsset(AudioCueSO createdAudioCue, string assetPath)
        {
            Undo.RegisterCreatedObjectUndo(createdAudioCue, "Create AudioCueSO");
            AssetDatabase.ImportAsset(assetPath);

            Selection.activeObject = createdAudioCue;
            EditorGUIUtility.PingObject(createdAudioCue);
        }
    }
}