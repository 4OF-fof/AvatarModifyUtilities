using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using AMU.AssetManager.Data;
using AMU.AssetManager.Helper;
using AMU.Editor.Core.Helper;

namespace AMU.AssetManager.UI
{
    public class AdvancedSearchWindow : EditorWindow
    {
        private AdvancedSearchCriteria _searchCriteria;
        private Vector2 _scrollPosition;
        private List<string> _availableTags;
        private bool[] _tagSelections;
        private string _tagSearchQuery = "";
        private Action<AdvancedSearchCriteria> _onSearchCallback;

        public static void ShowWindow(AdvancedSearchCriteria currentCriteria, Action<AdvancedSearchCriteria> onSearchCallback)
        {
            var window = GetWindow<AdvancedSearchWindow>("詳細検索");
            window.minSize = new Vector2(400, 500);
            window.maxSize = new Vector2(400, 800);
            window._searchCriteria = currentCriteria?.Clone() ?? new AdvancedSearchCriteria();
            window._onSearchCallback = onSearchCallback;
            window.InitializeTags();
            window.Show();
        }

        private void InitializeTags()
        {
            // TagTypeManagerの登録済みタグを取得
            var registeredTags = AssetTagManager.GetAllTags();

            // 実際に使用されているタグを取得
            var usedTags = new HashSet<string>();
            var dataManager = AssetDataManager.Instance;
            if (dataManager?.Library?.assets != null)
            {
                foreach (var asset in dataManager.Library.assets)
                {
                    if (asset.tags != null)
                    {
                        foreach (var tag in asset.tags)
                        {
                            if (!string.IsNullOrWhiteSpace(tag))
                            {
                                usedTags.Add(tag);
                            }
                        }
                    }
                }
            }

            // 重複を除いて統合
            var allTagsSet = new HashSet<string>(registeredTags);
            foreach (var tag in usedTags)
            {
                allTagsSet.Add(tag);
            }

            _availableTags = allTagsSet.ToList();
            _availableTags.Sort();

            _tagSelections = new bool[_availableTags.Count];

            // 現在選択されているタグを反映
            for (int i = 0; i < _availableTags.Count; i++)
            {
                _tagSelections[i] = _searchCriteria.selectedTags.Contains(_availableTags[i]);
            }
        }

        private void OnGUI()
        {
            if (_searchCriteria == null)
            {
                _searchCriteria = new AdvancedSearchCriteria();
                InitializeTags();
            }

            using (var scrollView = new EditorGUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scrollView.scrollPosition;

                GUILayout.Space(10);

                // ヘッダー
                var headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 16,
                    alignment = TextAnchor.MiddleCenter
                };
                GUILayout.Label("詳細検索", headerStyle);

                GUILayout.Space(15);

                // 検索フィールド設定
                DrawSearchFields();

                GUILayout.Space(15);

                // タグ検索
                DrawTagSelection();

                GUILayout.Space(15);

                // 検索ロジック設定
                DrawSearchLogicSettings();

                GUILayout.Space(20);

                // ボタン
                DrawButtons();
            }
        }

        private void DrawSearchFields()
        {
            var sectionStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14
            };
            GUILayout.Label("検索フィールド", sectionStyle);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // 名前検索
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("名前", GUILayout.Width(80));
                    _searchCriteria.nameQuery = EditorGUILayout.TextField(_searchCriteria.nameQuery);
                }

                GUILayout.Space(5);

                // 説明検索
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("説明", GUILayout.Width(80));
                    _searchCriteria.descriptionQuery = EditorGUILayout.TextField(_searchCriteria.descriptionQuery);
                }

                GUILayout.Space(5);

                // 作者検索
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("作者", GUILayout.Width(80));
                    _searchCriteria.authorQuery = EditorGUILayout.TextField(_searchCriteria.authorQuery);
                }
            }
        }

        private void DrawTagSelection()
        {
            var sectionStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14
            };
            GUILayout.Label("タグ検索", sectionStyle);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Space(5);

                // タグ検索ボックス
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("タグ絞込", GUILayout.Width(80));
                    _tagSearchQuery = EditorGUILayout.TextField(_tagSearchQuery);
                }

                GUILayout.Space(5);

                // タグ選択
                var filteredTags = string.IsNullOrEmpty(_tagSearchQuery)
                    ? _availableTags
                    : _availableTags.Where(tag => tag.ToLower().Contains(_tagSearchQuery.ToLower())).ToList();

                if (filteredTags.Any())
                {
                    var tagAreaHeight = Mathf.Min(200f, filteredTags.Count * 20f + 10f);
                    using (var tagScrollView = new EditorGUILayout.ScrollViewScope(Vector2.zero, GUILayout.Height(tagAreaHeight)))
                    {
                        foreach (var tag in filteredTags)
                        {
                            var originalIndex = _availableTags.IndexOf(tag);
                            if (originalIndex >= 0 && originalIndex < _tagSelections.Length)
                            {
                                var wasSelected = _tagSelections[originalIndex];
                                _tagSelections[originalIndex] = EditorGUILayout.Toggle(tag, _tagSelections[originalIndex]);

                                // 選択状態が変わった場合、selectedTagsリストを更新
                                if (wasSelected != _tagSelections[originalIndex])
                                {
                                    if (_tagSelections[originalIndex])
                                    {
                                        if (!_searchCriteria.selectedTags.Contains(tag))
                                            _searchCriteria.selectedTags.Add(tag);
                                    }
                                    else
                                    {
                                        _searchCriteria.selectedTags.Remove(tag);
                                    }
                                }
                            }
                        }
                    }
                }

                // 選択されたタグの表示
                if (_searchCriteria.selectedTags.Count > 0)
                {
                    GUILayout.Space(5);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("すべてクリア", GUILayout.Width(100)))
                        {
                            _searchCriteria.selectedTags.Clear();
                            for (int i = 0; i < _tagSelections.Length; i++)
                            {
                                _tagSelections[i] = false;
                            }
                        }
                    }
                }
            }
        }

        private void DrawSearchLogicSettings()
        {
            // タグが選択されていない場合は何も表示しない
            if (_searchCriteria.selectedTags.Count == 0)
                return;

            var sectionStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14
            };
            GUILayout.Label("検索ロジック", sectionStyle);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (_searchCriteria.selectedTags.Count > 1)
                {
                    _searchCriteria.useAndLogicForTags = EditorGUILayout.Toggle(
                        "AND検索",
                        _searchCriteria.useAndLogicForTags);

                    var logicText = _searchCriteria.useAndLogicForTags
                        ? "選択したすべてのタグが含まれているアセットを検索します"
                        : "選択したタグのいずれかが含まれているアセットを検索します";
                    EditorGUILayout.HelpBox(logicText, MessageType.Info);
                }
                else if (_searchCriteria.selectedTags.Count == 1)
                {
                    EditorGUILayout.HelpBox($"タグ「{_searchCriteria.selectedTags[0]}」を含むアセットを検索します", MessageType.Info);
                }
            }
        }

        private void DrawButtons()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                // 検索ボタン
                GUI.enabled = _searchCriteria.HasCriteria();
                if (GUILayout.Button("検索", GUILayout.Width(100), GUILayout.Height(30)))
                {
                    _onSearchCallback?.Invoke(_searchCriteria);
                    Close();
                }
                GUI.enabled = true;

                GUILayout.Space(10);

                // クリアボタン
                if (GUILayout.Button("クリア", GUILayout.Width(100), GUILayout.Height(30)))
                {
                    _searchCriteria = new AdvancedSearchCriteria();
                    InitializeTags();
                    _tagSearchQuery = "";
                }

                GUILayout.Space(10);

                // キャンセルボタン
                if (GUILayout.Button("キャンセル", GUILayout.Width(100), GUILayout.Height(30)))
                {
                    Close();
                }

                GUILayout.FlexibleSpace();
            }
        }

        private void OnDestroy()
        {
            // ウィンドウが閉じられた時のクリーンアップ
        }
    }
}
