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
        private AssetLibraryController _controller;
        private string _name = "";
        private string _author = "";
        private string _description = "";
        private List<string> _tags = new List<string>();
        private bool _tagsAnd = false;
        private bool _filterAnd = false;

        public static void ShowWindow(AssetLibraryController controller)
        {
            var window = GetWindow<AdvancedSearchWindow>("詳細検索");
            window._controller = controller;
            // 既存条件を初期値に
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

            EditorGUILayout.BeginVertical(GUI.skin.box);
            // 全体ANDトグル
            _filterAnd = EditorGUILayout.ToggleLeft("全体をANDで絞り込む", _filterAnd);
            EditorGUILayout.LabelField("検索条件", EditorStyles.boldLabel);
            GUILayout.Space(4);

            // 名前・作者・説明
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("名前", GUILayout.Width(60));
            _name = EditorGUILayout.TextField(_name, GUILayout.MinWidth(200));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("作者", GUILayout.Width(60));
            _author = EditorGUILayout.TextField(_author, GUILayout.MinWidth(200));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("説明", GUILayout.Width(60));
            _description = EditorGUILayout.TextField(_description, GUILayout.MinWidth(200));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("タグ", EditorStyles.boldLabel);
            if (_tags.Count > 0)
            {
                EditorGUILayout.BeginVertical();
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
                EditorGUILayout.EndVertical();
                if (GUILayout.Button("タグ選択", GUILayout.ExpandWidth(true)))
                {
                    TagSelectorWindow.ShowWindow(true, tags =>
                    {
                        _tags = tags ?? new List<string>();
                        Repaint();
                    }, _tags);
                }
                _tagsAnd = EditorGUILayout.ToggleLeft("タグをANDで絞り込む", _tagsAnd);
            }
            else
            {
                if (GUILayout.Button("タグ選択", GUILayout.ExpandWidth(true)))
                {
                    TagSelectorWindow.ShowWindow(true, tags =>
                    {
                        _tags = tags ?? new List<string>();
                        Repaint();
                    }, _tags);
                }
            }
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            GUILayout.Space(8);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("キャンセル", GUILayout.Width(100)))
                {
                    Close();
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("検索", GUILayout.Width(120)))
                {
                    ApplySearch();
                    Close();
                }
            }
        }

        private void ApplySearch()
        {
            if (_controller == null) return;
            var opt = _controller.filterOptions ?? new FilterOptions();
            opt.name = _name;
            opt.authorName = _author;
            opt.description = _description;
            opt.tags = new List<string>(_tags);
            opt.tagsAnd = _tagsAnd;
            opt.filterAnd = _filterAnd;
            _controller.filterOptions = opt;
        }
    }
}
