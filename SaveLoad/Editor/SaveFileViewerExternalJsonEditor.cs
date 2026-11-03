using System;
using System.IO;
using FakeMG.Framework;
using Unity.CodeEditor;
using UnityEditor;

namespace FakeMG.SaveLoad.Editor
{
    /// <summary>
    /// Round-trips raw JSON through the external script editor (Preferences > External Tools). The JSON is written to a
    /// file under the project's Temp folder, which Unity wipes on exit, and every save made in the external editor is
    /// reported back through <see cref="TryReadSavedJson"/>.
    /// </summary>
    internal sealed class SaveFileViewerExternalJsonEditor
    {
        private const string TEMP_SUBDIRECTORY_NAME = "SaveFileViewer";
        private const string JSON_EXTENSION = ".json";

        private string _tempFilePath;
        private DateTime _lastKnownWriteTimeUtc;

        public bool IsEditing => _tempFilePath != null;

        #region Public Methods

        public void Open(string json, string documentName)
        {
            Close();

            string tempDirectoryPath = Path.Combine(GetProjectTempDirectoryPath(), TEMP_SUBDIRECTORY_NAME);
            Directory.CreateDirectory(tempDirectoryPath);

            _tempFilePath = Path.Combine(tempDirectoryPath, ToSafeFileName(documentName) + JSON_EXTENSION);
            File.WriteAllText(_tempFilePath, json);
            _lastKnownWriteTimeUtc = File.GetLastWriteTimeUtc(_tempFilePath);

            if (!CodeEditor.CurrentEditor.OpenProject(_tempFilePath))
            {
                EditorUtility.OpenWithDefaultApp(_tempFilePath);
            }
        }

        /// <summary>Returns true once per external save, with the saved JSON.</summary>
        public bool TryReadSavedJson(out string json)
        {
            json = null;
            if (!IsEditing || !File.Exists(_tempFilePath))
            {
                return false;
            }

            DateTime writeTimeUtc = File.GetLastWriteTimeUtc(_tempFilePath);
            if (writeTimeUtc == _lastKnownWriteTimeUtc)
            {
                return false;
            }

            try
            {
                json = File.ReadAllText(_tempFilePath);
            }
            catch (IOException exception)
            {
                // The external editor may still hold the file while writing; the next poll retries.
                Echo.Warning($"[SaveFileViewer] Could not read externally edited JSON yet: {exception.Message}");
                return false;
            }

            _lastKnownWriteTimeUtc = writeTimeUtc;
            return true;
        }

        public void Close()
        {
            if (!IsEditing)
            {
                return;
            }

            try
            {
                File.Delete(_tempFilePath);
            }
            catch (IOException exception)
            {
                Echo.Warning($"[SaveFileViewer] Could not delete temp file '{_tempFilePath}': {exception.Message}");
            }

            _tempFilePath = null;
        }

        #endregion

        #region Private Methods

        // Derived from Unity's own temp path so this follows wherever Unity keeps the project's Temp folder.
        private static string GetProjectTempDirectoryPath()
        {
            return Path.GetDirectoryName(Path.GetFullPath(FileUtil.GetUniqueTempPathInProject()));
        }

        private static string ToSafeFileName(string documentName)
        {
            char[] invalidCharacters = Path.GetInvalidFileNameChars();
            char[] safeCharacters = documentName.ToCharArray();

            for (int i = 0; i < safeCharacters.Length; i++)
            {
                if (Array.IndexOf(invalidCharacters, safeCharacters[i]) >= 0)
                {
                    safeCharacters[i] = '_';
                }
            }

            return new string(safeCharacters);
        }

        #endregion
    }
}
