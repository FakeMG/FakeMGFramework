using System;
using System.Collections.Generic;
using System.Linq;
using FakeMG.SaveLoad;
using UnityEditor.IMGUI.Controls;

namespace FakeMG.SaveLoad.Editor
{
    internal sealed class SaveFileViewerFileTreeView : TreeView<int>
    {
        private readonly List<ValidatedSaveFileInfo> _fileEntries = new();
        private readonly Dictionary<string, int> _fileIdsByPath = new(StringComparer.Ordinal);

        public event Action<string> FileSelected;

        public SaveFileViewerFileTreeView(TreeViewState<int> state) : base(state)
        {
            Reload();
        }

        public void SetEntries(IEnumerable<ValidatedSaveFileInfo> fileEntries)
        {
            _fileEntries.Clear();
            _fileEntries.AddRange(fileEntries
                .OrderBy(entry => entry.SaveDirectoryPath, StringComparer.Ordinal)
                .ThenByDescending(entry => entry.TimestampUtc)
                .ThenBy(entry => entry.SaveFileName, StringComparer.Ordinal));

            Reload();
            ExpandAll();
        }

        public void SetSelectedFile(string saveFilePath)
        {
            if (string.IsNullOrEmpty(saveFilePath) || !_fileIdsByPath.TryGetValue(saveFilePath, out int fileId))
            {
                ClearFileSelection();
                return;
            }

            SetSelection(new List<int> { fileId });
            FrameItem(fileId);
        }

        public void ClearFileSelection()
        {
            SetSelection(new List<int>());
        }

        protected override TreeViewItem<int> BuildRoot()
        {
            TreeViewItem<int> root = new()
            {
                id = 0,
                depth = -1,
                displayName = "Save Files"
            };

            List<TreeViewItem<int>> allItems = new();
            _fileIdsByPath.Clear();

            int nextId = 1;
            AddGlobalDocumentItems(allItems, ref nextId);
            AddWorldItems(allItems, ref nextId);

            SetupParentsAndChildrenFromDepths(root, allItems);
            return root;
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds.Count == 0)
            {
                return;
            }

            if (FindItem(selectedIds[0], rootItem) is SaveFileViewerFileTreeViewItem fileItem)
            {
                FileSelected?.Invoke(fileItem.FileInfo.SaveFilePath);
            }
        }

        private void AddGlobalDocumentItems(ICollection<TreeViewItem<int>> allItems, ref int nextId)
        {
            List<ValidatedSaveFileInfo> globalDocuments = _fileEntries
                .Where(entry => entry.SaveKind == SaveFileKind.GlobalDocument)
                .ToList();

            if (globalDocuments.Count == 0)
            {
                return;
            }

            allItems.Add(CreateFolderItem(nextId++, 0, "Global Documents"));

            foreach (ValidatedSaveFileInfo entry in globalDocuments)
            {
                AddFileItem(entry, 1, allItems, ref nextId);
            }
        }

        private void AddWorldItems(ICollection<TreeViewItem<int>> allItems, ref int nextId)
        {
            List<IGrouping<string, ValidatedSaveFileInfo>> worldGroups = _fileEntries
                .Where(entry => entry.SaveKind != SaveFileKind.GlobalDocument)
                .GroupBy(entry => entry.OwnerId, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .ToList();

            if (worldGroups.Count == 0)
            {
                return;
            }

            allItems.Add(CreateFolderItem(nextId++, 0, "Worlds"));

            foreach (IGrouping<string, ValidatedSaveFileInfo> worldGroup in worldGroups)
            {
                allItems.Add(CreateFolderItem(nextId++, 1, worldGroup.Key));

                ValidatedSaveFileInfo manifest = worldGroup.FirstOrDefault(entry => entry.SaveKind == SaveFileKind.WorldManifest);
                if (manifest != null)
                {
                    AddFileItem(manifest, 2, allItems, ref nextId);
                }

                List<ValidatedSaveFileInfo> snapshots = worldGroup
                    .Where(entry => entry.SaveKind is SaveFileKind.Manual or SaveFileKind.Auto)
                    .OrderByDescending(entry => entry.TimestampUtc)
                    .ToList();

                if (snapshots.Count == 0)
                {
                    continue;
                }

                allItems.Add(CreateFolderItem(nextId++, 2, "Snapshots"));

                foreach (ValidatedSaveFileInfo snapshot in snapshots)
                {
                    AddFileItem(snapshot, 3, allItems, ref nextId);
                }
            }
        }

        private void AddFileItem(
            ValidatedSaveFileInfo entry,
            int depth,
            ICollection<TreeViewItem<int>> allItems,
            ref int nextId)
        {
            SaveFileViewerFileTreeViewItem fileItem = new()
            {
                id = nextId++,
                depth = depth,
                displayName = BuildFileDisplayName(entry),
                FileInfo = entry
            };

            allItems.Add(fileItem);
            _fileIdsByPath[entry.SaveFilePath] = fileItem.id;
        }

        private static TreeViewItem<int> CreateFolderItem(int id, int depth, string displayName)
        {
            return new TreeViewItem<int>
            {
                id = id,
                depth = depth,
                displayName = displayName
            };
        }

        private static string BuildFileDisplayName(ValidatedSaveFileInfo entry)
        {
            string badge = SaveFileKindPresentation.GetBadge(entry.SaveKind);
            return $"{badge} {entry.SaveFileName}    {entry.TimestampUtc:yyyy-MM-dd HH:mm:ss} UTC";
        }
    }

    internal static class SaveFileKindPresentation
    {
        public static string GetBadge(SaveFileKind saveKind)
        {
            return saveKind switch
            {
                SaveFileKind.GlobalDocument => "[Global]",
                SaveFileKind.WorldManifest => "[World]",
                SaveFileKind.Manual => "[Manual]",
                SaveFileKind.Auto => "[Auto]",
                _ => throw new ArgumentOutOfRangeException(nameof(saveKind), saveKind, "Unsupported save kind."),
            };
        }
    }

    internal sealed class SaveFileViewerFileTreeViewItem : TreeViewItem<int>
    {
        public ValidatedSaveFileInfo FileInfo { get; set; }
    }
}
