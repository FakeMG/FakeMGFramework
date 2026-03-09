using System;
using System.Collections.Generic;
using System.Linq;
using FakeMG.SaveLoad.Advanced;
using UnityEditor.IMGUI.Controls;

namespace FakeMG.SaveLoad.Editor
{
    internal sealed class SaveFileViewerFileTreeView : TreeView<int>
    {
        private const string ROOT_FOLDER_KEY = "__ROOT__";

        private readonly List<ManagedSaveFileInfo> _fileEntries = new();
        private readonly Dictionary<string, int> _fileIdsByPath = new(StringComparer.Ordinal);

        public event Action<string> FileSelected;

        public SaveFileViewerFileTreeView(TreeViewState<int> state) : base(state)
        {
            Reload();
        }

        public void SetEntries(IEnumerable<ManagedSaveFileInfo> fileEntries)
        {
            _fileEntries.Clear();
            _fileEntries.AddRange(fileEntries
                .OrderBy(entry => entry.SaveDirectoryPath, StringComparer.Ordinal)
                .ThenByDescending(entry => entry.Metadata.Timestamp)
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
            Dictionary<string, int> folderDepths = new(StringComparer.Ordinal);
            _fileIdsByPath.Clear();

            int nextId = 1;

            foreach (ManagedSaveFileInfo entry in _fileEntries)
            {
                int fileDepth = EnsureDirectoryItems(entry.SaveDirectoryPath, folderDepths, allItems, ref nextId);
                SaveFileViewerFileTreeViewItem fileItem = new()
                {
                    id = nextId++,
                    depth = fileDepth,
                    displayName = BuildFileDisplayName(entry),
                    FileInfo = entry
                };

                allItems.Add(fileItem);
                _fileIdsByPath[entry.SaveFilePath] = fileItem.id;
            }

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

        private static int EnsureDirectoryItems(
            string saveDirectoryPath,
            IDictionary<string, int> folderDepths,
            ICollection<TreeViewItem<int>> allItems,
            ref int nextId)
        {
            if (string.IsNullOrEmpty(saveDirectoryPath))
            {
                if (!folderDepths.ContainsKey(ROOT_FOLDER_KEY))
                {
                    allItems.Add(new TreeViewItem<int>
                    {
                        id = nextId++,
                        depth = 0,
                        displayName = SaveFileCatalog.ROOT_FOLDER_DISPLAY_NAME
                    });

                    folderDepths[ROOT_FOLDER_KEY] = 0;
                }

                return 1;
            }

            string[] segments = saveDirectoryPath.Split('/');
            string currentPath = string.Empty;

            for (int i = 0; i < segments.Length; i++)
            {
                currentPath = string.IsNullOrEmpty(currentPath)
                    ? segments[i]
                    : $"{currentPath}/{segments[i]}";

                if (folderDepths.ContainsKey(currentPath))
                {
                    continue;
                }

                allItems.Add(new TreeViewItem<int>
                {
                    id = nextId++,
                    depth = i,
                    displayName = segments[i]
                });

                folderDepths[currentPath] = i;
            }

            return segments.Length;
        }

        private static string BuildFileDisplayName(ManagedSaveFileInfo entry)
        {
            string badge = SaveFileCatalog.GetSaveKindBadge(entry.Metadata);
            return $"{badge} {entry.SaveFileName}    {entry.Metadata.Timestamp:yyyy-MM-dd HH:mm:ss}";
        }
    }

    internal sealed class SaveFileViewerFileTreeViewItem : TreeViewItem<int>
    {
        public ManagedSaveFileInfo FileInfo { get; set; }
    }
}