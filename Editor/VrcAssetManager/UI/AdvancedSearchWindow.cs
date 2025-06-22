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
            window.minSize = new Vector2(400, 340);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("詳細検索", EditorStyles.boldLabel);
            GUILayout.Space(8);

            _filterAnd = EditorGUILayout.ToggleLeft("全体をANDで絞り込む", _filterAnd);
            GUILayout.Space(4);

            _name = EditorGUILayout.TextField("名前", _name);
            _author = EditorGUILayout.TextField("作者", _author);
            _description = EditorGUILayout.TextField("説明", _description);

            GUILayout.Space(8);
            GUILayout.Label("タグ", EditorStyles.label);
            using (new GUILayout.HorizontalScope())
            {
                if (_tags.Count > 0)
                {
                    GUILayout.Label(string.Join(", ", _tags), EditorStyles.textField, GUILayout.ExpandWidth(true));
                }
                else
                {
                    GUILayout.Label("(未選択)", EditorStyles.textField, GUILayout.ExpandWidth(true));
                }
                if (GUILayout.Button("タグ選択", GUILayout.Width(80)))
                {
                    TagSelectorWindow.ShowWindow(true, tags =>
                    {
                        _tags = tags ?? new List<string>();
                        Repaint();
                    }, _tags);
                }
            }
            _tagsAnd = EditorGUILayout.ToggleLeft("タグをANDで絞り込む", _tagsAnd);

            GUILayout.FlexibleSpace();
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("キャンセル"))
                {
                    Close();
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("検索", GUILayout.Width(100)))
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
