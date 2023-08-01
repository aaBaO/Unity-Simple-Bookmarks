﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace SimpleBookmarks.Editor 
{
    internal class BookmarksTreeView : TreeView
    {
        private const float RowHeight = 20f;
        private const float ToggleWidth = 20;

        private List<TreeViewItem> _viewItems;
        private BookmarksContainer _bookmarksContainer;
        private Regex _searchRegex;

        enum EColumns
        {
            Object,
            AssetPath,
            Note
        }

        private static class GUIConst
        {
            public static readonly GUIContent ReloadMenuItem = new GUIContent("Reload");
            public static readonly GUIContent WatchContainerMenuItem = new GUIContent("WatchContainer");
            public static readonly GUIContent RenameGroupMenuItem = new GUIContent("Rename", "System shortcut key also work");
            public static readonly GUIContent AddGroupMenuItem = new GUIContent("AddGroup");
            public static readonly GUIContent RemoveGroupMenuItem = new GUIContent("RemoveGroup");
            public static readonly GUIContent RemoveItemMenuItem = new GUIContent("RemoveItem");
        }

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState()
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    width = 200,
                    sortedAscending = false,
                    headerContent = new GUIContent("Item"),
                    contextMenuText = null,
                    headerTextAlignment = TextAlignment.Left,
                    minWidth = 20,
                    autoResize = false,
                    allowToggleVisibility = false,
                    canSort = false,
                },
                new MultiColumnHeaderState.Column
                {
                    width = 200,
                    sortedAscending = false,
                    headerContent = new GUIContent("AssetPath"),
                    contextMenuText = null,
                    headerTextAlignment = TextAlignment.Left,
                    minWidth = 20,
                    autoResize = false,
                    allowToggleVisibility = false,
                    canSort = false,
                },
                new MultiColumnHeaderState.Column
                {
                    width = 100,
                    sortedAscending = false,
                    headerContent = new GUIContent("Note"),
                    contextMenuText = null,
                    headerTextAlignment = TextAlignment.Left,
                    minWidth = 20,
                    autoResize = false,
                    allowToggleVisibility = false,
                    canSort = false,
                },
            };

            return new MultiColumnHeaderState(columns);
        }
        
        

        public BookmarksTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader, BookmarksContainer container) : base(state, multiColumnHeader)
        {
            _bookmarksContainer = container;

            rowHeight = RowHeight;
            columnIndexForTreeFoldouts = 0;
            showAlternatingRowBackgrounds = false;
            showBorder = true;
            customFoldoutYOffset = (RowHeight - EditorGUIUtility.singleLineHeight) * 0.5f;
            extraSpaceBeforeIconAndLabel = ToggleWidth;
            useScrollView = true;
            
            Reload();
        }


        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem(0, -1, "Root");
            return root;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            if(root.hasChildren) root.children.Clear();
            
            foreach (var group in _bookmarksContainer.groups)
            {
                var groupItem = new GroupViewItem() 
                {
                    id = group.GetHashCode(),
                    displayName = group.name,
                    
                    Data = group
                };
                root.AddChild(groupItem);
                
                foreach (var item in group.items)
                {
                    groupItem.AddChild(new ObjectViewItem()
                    {
                        id = item.obj ? item.obj.GetInstanceID() : int.MinValue,
                        Data = item
                    });
                }
            }
            
            SetupDepthsFromParentsAndChildren(root);
            
            return base.BuildRows(root);
        }

        protected override void SearchChanged(string newSearch)
        {
            _searchRegex = new Regex(newSearch, RegexOptions.IgnoreCase);
        }

        protected override bool DoesItemMatchSearch(TreeViewItem item, string search)
        {
            if (_searchRegex == null) return false;
            return item switch
            {
                GroupViewItem groupViewItem => _searchRegex.IsMatch(groupViewItem.Data.name),
                ObjectViewItem objectViewItem => objectViewItem.Data.obj
                           && (_searchRegex.IsMatch(objectViewItem.Data.obj.name) ||
                               _searchRegex.IsMatch(AssetDatabase.GetAssetPath(objectViewItem.Data.obj))),
                _ => base.DoesItemMatchSearch(item, search)
            };
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            using var check = new EditorGUI.ChangeCheckScope();
            for (var c = 0; c < args.GetNumVisibleColumns(); c++)
            {
                CellGUI(args.GetCellRect(c), args.item, (EColumns)args.GetColumn(c), ref args);
            }

            var e = Event.current;
            if (e.type == EventType.ContextClick && args.rowRect.Contains(e.mousePosition))
            {
                var genericMenu = new GenericMenu();
                AddItemsToMenu(genericMenu, ref args);
                genericMenu.ShowAsContext();
                Event.current.Use();
            }

            if (check.changed)
            {
                _bookmarksContainer.Save();
            }
        }

        protected override void ContextClicked()
        {
            var genericMenu = new GenericMenu();
            genericMenu.AddItem(GUIConst.AddGroupMenuItem, false, () =>
            {
                _bookmarksContainer.AddGroup(new()
                {
                    name = "NewGroup",
                    items = new List<Item>()
                });
                _bookmarksContainer.Save();
                Reload();
            });
            genericMenu.AddItem(GUIConst.ReloadMenuItem, false, Reload);
            genericMenu.AddSeparator(string.Empty);
            genericMenu.AddItem(GUIConst.WatchContainerMenuItem, false, () =>
            {
                var path = $"{Application.dataPath}/../{BookmarksContainer.DataPath}";
#if UNITY_EDITOR_WIN
                System.Diagnostics.Process.Start("explorer.exe", $"/select,{path.Replace("/", "\\")}");
#else
                System.Diagnostics.Process.Start("open", $"{path}");
#endif
            });
            genericMenu.ShowAsContext();
            Event.current.Use();
        }

        private void AddItemsToMenu(GenericMenu genericMenu, ref RowGUIArgs args)
        {
            var guiArgs = args;
            switch (guiArgs.item)
            {
                case GroupViewItem groupViewItem:
                    genericMenu.AddItem(GUIConst.RenameGroupMenuItem, false, () => BeginRename(guiArgs.item));
                    genericMenu.AddItem(GUIConst.AddGroupMenuItem, false, () =>
                    {
                        _bookmarksContainer.AddGroup(new()
                        {
                            name = "NewGroup",
                            items = new List<Item>()
                        });
                        _bookmarksContainer.Save();
                        Reload();
                    });
                    genericMenu.AddItem(GUIConst.RemoveGroupMenuItem, false, () =>
                    {
                        _bookmarksContainer.RemoveGroup(groupViewItem.Data);
                        _bookmarksContainer.Save();
                        Reload();
                    });
                    break;
                case ObjectViewItem objectViewItem:
                    genericMenu.AddItem(GUIConst.RemoveItemMenuItem, false, () =>
                    {
                        _bookmarksContainer.RemoveItem(objectViewItem.Data);
                        _bookmarksContainer.Save();
                        Reload();
                    });
                    break;
            }
        }

        private void CellGUI(Rect rect, TreeViewItem item, EColumns columns, ref RowGUIArgs args)
        {
            CenterRectUsingSingleLineHeight(ref rect);
            var indent = GetContentIndent(item);
            
            switch (item)
            {
                case GroupViewItem groupViewItem:
                    switch (columns)
                    {
                        case EColumns.Object:
                        {
                            var objectRect = rect;
                            objectRect.x += indent;
                            objectRect.width = rect.width - indent;
                            EditorGUI.LabelField(objectRect, groupViewItem.Data.name, EditorStyles.boldLabel);
                            args.rowRect = rect;
                        }
                            break;
                        case EColumns.Note:
                            groupViewItem.Data.note = EditorGUI.TextField(rect, groupViewItem.Data.note);
                            break;
                    }
                    break;
                case ObjectViewItem objectViewItem:
                    switch (columns)
                    {
                        case EColumns.Object:
                        {
                            var objectRect = rect;
                            objectRect.x += indent;
                            objectRect.width = rect.width - indent;
                            objectViewItem.Data.obj = EditorGUI.ObjectField(objectRect, objectViewItem.Data.obj, typeof(UnityEngine.Object), false);
                        }
                            break;
                        case EColumns.AssetPath:
                            EditorGUI.SelectableLabel(rect, AssetDatabase.GetAssetPath(objectViewItem.Data.obj), EditorStyles.textField);
                            break;
                        case EColumns.Note:
                            objectViewItem.Data.note = EditorGUI.TextField(rect, objectViewItem.Data.note);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(columns), columns, null);
                    }
                    break;
            }
        }

        #region Drag & Drop

        private const string DragGenericLabel = "DargInBookMarkTreeView";
        
        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            return !hasSearch;
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            DragAndDrop.PrepareStartDrag();
            var sortedDraggedIDs = SortItemIDsInRowOrder(args.draggedItemIDs);
            var draggedItems = new HashSet<Item>();
            foreach (var viewItem in FindRows(sortedDraggedIDs))
            {
                switch (viewItem)
                {
                    case GroupViewItem groupViewItem:
                        foreach (var item in groupViewItem.Data.items) draggedItems.Add(item);
                        break;
                    case ObjectViewItem objectViewItem:
                        draggedItems.Add(objectViewItem.Data);
                        break;
                }
            }

            if (draggedItems.Count == 0)
            {
                DragAndDrop.AcceptDrag();
                return;
            }

            var items = draggedItems.ToList();
            DragAndDrop.SetGenericData(DragGenericLabel, items);
            DragAndDrop.StartDrag(draggedItems.Count > 1 ? "<Multiple>" : items[0].obj.name);
        }
        
        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            if (args.performDrop)
            {
                var draggedItems = (List<Item>)DragAndDrop.GetGenericData(DragGenericLabel);
                var dragFromTreeView = draggedItems != null;
                switch (args.dragAndDropPosition)
                {
                    case DragAndDropPosition.UponItem:
                    case DragAndDropPosition.BetweenItems:
                    case DragAndDropPosition.OutsideItems:
                    {
                        var parentItem = GetParentViewItem(args.parentItem);
                        foreach (var g in _bookmarksContainer.groups)
                        {
                            for (var i = g.items.Count - 1; i >= 0 ; i--)
                            {
                                if (dragFromTreeView)
                                {
                                    if(draggedItems.Contains(g.items[i]))
                                        g.items.RemoveAt(i);
                                }
                                else
                                {
                                    foreach (var obj in DragAndDrop.objectReferences)
                                    {
                                        if (g.items[i].Equals(new Item() { obj = obj }))
                                            g.items.RemoveAt(i);
                                    }
                                }
                            }
                        }

                        if (args.dragAndDropPosition == DragAndDropPosition.BetweenItems)
                        {
                            var insertIndex = args.insertAtIndex;
                            if (dragFromTreeView)
                            {
                                for (var i = 0; i < draggedItems.Count; i++)
                                {
                                    var preferIndex = GetPreferIndex(parentItem.Data.items, insertIndex + i);
                                    parentItem.Data.items.Insert(preferIndex, draggedItems[i]);
                                }
                            }
                            else
                            {
                                for (var i = 0; i < DragAndDrop.objectReferences.Length; i++)
                                {
                                    var preferIndex = GetPreferIndex(parentItem.Data.items, insertIndex + i);
                                    parentItem.Data.items.Insert(preferIndex, new Item() { obj = DragAndDrop.objectReferences[i] });
                                }
                            }
                        }
                        else
                        {
                            if (dragFromTreeView)
                                parentItem.Data.items.AddRange(draggedItems);
                            else
                                parentItem.Data.items.AddRange(DragAndDrop.objectReferences.Select(o=> new Item(){obj = o}));
                        }
                        
                        _bookmarksContainer.Save();
                        Reload();
                    }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            
            return DragAndDropVisualMode.Move;
        }

        private int GetPreferIndex(List<Item> dataItems, int insertIndex)
        {
            var index = insertIndex;
            while (dataItems.Count < index)
                --index;

            return index;
        }

        private GroupViewItem GetParentViewItem(TreeViewItem treeViewItem)
        {
            return treeViewItem switch
            {
                GroupViewItem groupViewItem => groupViewItem,
                ObjectViewItem objectViewItem => objectViewItem.parent as GroupViewItem,
                _ => (GroupViewItem)GetRows()[0]
            };
        }

        #endregion

        #region Rename

        protected override bool CanRename(TreeViewItem item)
        {
            return item is GroupViewItem;
        }

        protected override void RenameEnded(RenameEndedArgs args)
        {
            if (!args.acceptedRename) return;
            
            var targetItem = (GroupViewItem)FindItem(args.itemID, rootItem);
            if (targetItem == null)
            {
                throw new NullReferenceException($"Can not found target item: {args.itemID}");
            }

            targetItem.Data.name = targetItem.displayName = args.newName;
        }

        protected override Rect GetRenameRect(Rect rowRect, int row, TreeViewItem item)
        {
            var indent = GetContentIndent(item);
            rowRect.x = indent;
            return rowRect;
        }

        #endregion
    }

    public class GroupViewItem : TreeViewItem
    {
        internal Group Data;
    }

    public class ObjectViewItem : TreeViewItem
    {
        internal Item Data;
    }
}