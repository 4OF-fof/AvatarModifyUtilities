using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AMU.Editor.VrcAssetManager.Controller;
using AMU.Editor.VrcAssetManager.Schema;

namespace AMU.Editor.VrcAssetManager.UI
{
    public class AdvancedSearchWindow : EditorWindow
    {
        private string _name = "";
        private string _author = "";
        private string _description = "";
        private List<string> _tags = new List<string>();
        private bool _tagsAnd = false;
        private bool _filterAnd = false;
        private Vector2 _scrollPosition = Vector2.zero;
        private Action<bool> _onClose;
        private bool _closedBySearch = false;

        public static void ShowWindow(Action<bool> onClose)
        {
            var window = GetWindow<AdvancedSearchWindow>("詳細検索");
            window._onClose = onClose;
            var controller = AssetLibraryController.Instance;
            if (controller != null && controller.filterOptions != null)
            {
                window._name = controller.filterOptions.name ?? "";
                window._author = controller.filterOptions.authorName ?? "";
                window._description = controller.filterOptions.description ?? "";
                window._tags = controller.filterOptions.tags != null ? new List<string>(controller.filterOptions.tags) : new List<string>();
                window._tagsAnd = controller.filterOptions.tagsAnd;
                window._filterAnd = controller.filterOptions.filterAnd;
            }
            window.minSize = window.maxSize = new Vector2(300, 400);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("詳細検索", EditorStyles.boldLabel);
            GUILayout.Space(8);

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField("検索条件", EditorStyles.boldLabel);
                GUILayout.Space(4);

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("名前", GUILayout.Width(60));
                    _name = EditorGUILayout.TextField(_name, GUILayout.MinWidth(200));
                }
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("作者", GUILayout.Width(60));
                    _author = EditorGUILayout.TextField(_author, GUILayout.MinWidth(200));
                }
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("説明", GUILayout.Width(60));
                    _description = EditorGUILayout.TextField(_description, GUILayout.MinWidth(200));
                }
            }

            GUILayout.Space(10);

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label("タグ", EditorStyles.boldLabel);
                if (_tags.Count > 0)
                {
                    using (var scrollView = new GUILayout.ScrollViewScope(_scrollPosition, GUILayout.Height(135)))
                    {
                        _scrollPosition = scrollView.scrollPosition;
                        using (new GUILayout.VerticalScope())
                        {
                            foreach (var tag in _tags)
                            {
                                GUIStyle tagBox = new GUIStyle(EditorStyles.helpBox);
                                tagBox.fontSize = 12;
                                tagBox.fixedHeight = 24;
                                tagBox.alignment = TextAnchor.MiddleCenter;
                                tagBox.padding = new RectOffset(6, 6, 2, 2);
                                tagBox.margin = new RectOffset(2, 2, 2, 2);
                                EditorGUILayout.LabelField(tag, tagBox, GUILayout.ExpandWidth(true));
                            }
                        }
                    }
                    if (GUILayout.Button("タグ選択", GUILayout.ExpandWidth(true)))
                    {
                        TagSelectorWindow.ShowWindow(tags =>
                        {
                            _tags = tags ?? new List<string>();
                            Repaint();
                        }, _tags, true);
                    }
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        _tagsAnd = EditorGUILayout.ToggleLeft("タグをANDで絞り込む", _tagsAnd, GUILayout.Width(130));
                        GUILayout.FlexibleSpace();
                    }
                }
                else
                {
                    if (GUILayout.Button("タグ選択", GUILayout.ExpandWidth(true)))
                    {
                        TagSelectorWindow.ShowWindow(tags =>
                        {
                            _tags = tags ?? new List<string>();
                            Repaint();
                        }, _tags, true);
                    }
                }
            }

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            _filterAnd = EditorGUILayout.ToggleLeft("全体をANDで絞り込む", _filterAnd, GUILayout.Width(160));
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("キャンセル", GUILayout.Width(80)))
                {
                    Close();
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("検索", GUILayout.Width(150)))
                {
                    ApplySearch();
                    _closedBySearch = true;
                    Close();
                }
            }
        }

        private void ApplySearch()
        {
            var controller = AssetLibraryController.Instance;
            if (controller == null) return;
            var opt = controller.filterOptions ?? new FilterOptions();
            opt.name = _name;
            opt.authorName = _author;
            opt.description = _description;
            opt.tags = new List<string>(_tags);
            opt.tagsAnd = _tagsAnd;
            opt.filterAnd = _filterAnd;
            controller.filterOptions = opt;
        }

        protected void OnDestroy()
        {
            var tagSelector = Resources.FindObjectsOfTypeAll<TagSelectorWindow>();
            if (tagSelector != null && tagSelector.Length > 0)
            {
                foreach (var win in tagSelector)
                {
                    win.Close();
                }
            }
        }

        protected void OnDisable()
        {
            if (_onClose != null)
            {
                _onClose.Invoke(_closedBySearch);
                _onClose = null;
            }
            _closedBySearch = false;
        }
    }
}
