using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace SimpleBookmarks.Editor 
{
    public class BookmarksWindow : EditorWindow
    {
        [SerializeField] private TreeViewState treeViewState;
        [SerializeField] private MultiColumnHeaderState multiColumnHeaderState;
        private SearchField _searchField;
        private BookmarksTreeView _bookmarksTreeView;

        private static class ConstantContent
        {
            public const float SearchFieldHeight = 18;
        }

        private void OnEnable()
        {
            treeViewState ??= new TreeViewState();
            var headerState = BookmarksTreeView.CreateDefaultMultiColumnHeaderState();
            if (MultiColumnHeaderState.CanOverwriteSerializedFields(multiColumnHeaderState, headerState))
                MultiColumnHeaderState.OverwriteSerializedFields(multiColumnHeaderState, headerState);
            multiColumnHeaderState = headerState;
            _bookmarksTreeView = new BookmarksTreeView(treeViewState, new MultiColumnHeader(multiColumnHeaderState), BookmarksContainer.Instance);
            
            _searchField ??= new SearchField();
            _searchField.downOrUpArrowKeyPressed += _bookmarksTreeView.SetFocusAndEnsureSelectedItem;
        }

        [MenuItem("Window/SimpleBookmarks")]
        public static void ShowWindow()
        {
            BookmarksWindow wnd = GetWindow<BookmarksWindow>();
            wnd.titleContent = new GUIContent("SimpleBookmarks");
        }

        private void OnGUI()
        {
            var searchRect = new Rect(0, 0, position.width, ConstantContent.SearchFieldHeight);
            var treeViewRect = new Rect(0, ConstantContent.SearchFieldHeight, position.width, position.height - searchRect.height);
            _bookmarksTreeView.searchString = _searchField.OnToolbarGUI(searchRect, _bookmarksTreeView.searchString);
            _bookmarksTreeView.OnGUI(treeViewRect);
        }
    }
}