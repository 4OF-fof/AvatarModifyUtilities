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

        // Tag input UI state (similar to AssetDetailWindow)
        private string _newTagInput = "";
        private List<string> _filteredTagSuggestions = new List<string>();
        private bool _showTagSuggestions = false;
        private Vector2 _tagSuggestionScrollPos = Vector2.zero;

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

            // 従来のtagSelectionsは使用しなくなったが、互換性のため残しておく
            _tagSelections = new bool[_availableTags.Count];
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
                GUILayout.Space(5);
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
                GUILayout.Space(5);
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

                // タグ入力フィールド（詳細ウィンドウと同様のUI）
                DrawTagInput();

                GUILayout.Space(10);

                // 選択されたタグの表示
                DrawSelectedTags();

                GUILayout.Space(5);
            }
        }
        private void DrawTagInput()
        {
            GUILayout.Label("タグを追加:", EditorStyles.miniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                var newTagInput = EditorGUILayout.TextField(_newTagInput);

                // 入力が変更された場合にサジェストを更新
                if (newTagInput != _newTagInput)
                {
                    _newTagInput = newTagInput;
                    UpdateTagSuggestions();
                }

                // 追加ボタンは既存タグに完全マッチした場合のみ有効
                var isValidTag = !string.IsNullOrEmpty(_newTagInput.Trim()) &&
                               _availableTags.Contains(_newTagInput.Trim()) &&
                               !_searchCriteria.selectedTags.Contains(_newTagInput.Trim());

                GUI.enabled = isValidTag;
                if (GUILayout.Button("追加", GUILayout.Width(60)))
                {
                    AddSelectedTag();
                }
                GUI.enabled = true;
            }

            // サジェスト表示
            if (_showTagSuggestions && _filteredTagSuggestions.Count > 0 && !string.IsNullOrEmpty(_newTagInput))
            {
                DrawTagSuggestions();
            }
        }

        private void UpdateTagSuggestions()
        {
            _filteredTagSuggestions.Clear();
            _showTagSuggestions = false;

            if (string.IsNullOrEmpty(_newTagInput))
            {
                return;
            }

            var input = _newTagInput.ToLower();
            foreach (var tag in _availableTags)
            {
                // 既に選択されているタグはスキップ
                if (_searchCriteria.selectedTags.Contains(tag))
                    continue;

                // 入力文字列を含むタグをフィルタリング
                if (tag.ToLower().Contains(input))
                {
                    _filteredTagSuggestions.Add(tag);
                }
            }

            _showTagSuggestions = _filteredTagSuggestions.Count > 0;
        }
        private void DrawTagSuggestions()
        {
            var suggestionHeight = Mathf.Min(_filteredTagSuggestions.Count * 20f, 200f);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Height(suggestionHeight)))
            {
                _tagSuggestionScrollPos = EditorGUILayout.BeginScrollView(_tagSuggestionScrollPos);

                for (int i = 0; i < _filteredTagSuggestions.Count; i++)
                {
                    var tag = _filteredTagSuggestions[i];
                    var rect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.label, GUILayout.Height(18));

                    if (GUI.Button(rect, tag, EditorStyles.label))
                    {
                        _newTagInput = tag;
                        AddSelectedTag();
                        _showTagSuggestions = false;
                        GUI.FocusControl(null);
                        break;
                    }

                    // ホバー時のハイライト
                    if (rect.Contains(Event.current.mousePosition))
                    {
                        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 1f, 0.3f));
                        GUI.Label(rect, tag);
                    }
                }

                EditorGUILayout.EndScrollView();
            }

            // キーボード入力の処理
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Escape)
                {
                    _showTagSuggestions = false;
                    Event.current.Use();
                }
                else if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                {
                    if (_filteredTagSuggestions.Count > 0)
                    {
                        _newTagInput = _filteredTagSuggestions[0];
                        AddSelectedTag();
                        _showTagSuggestions = false;
                        Event.current.Use();
                    }
                }
            }
        }
        private void AddSelectedTag()
        {
            if (!string.IsNullOrEmpty(_newTagInput.Trim()) && !_searchCriteria.selectedTags.Contains(_newTagInput.Trim()))
            {
                var trimmedTag = _newTagInput.Trim();

                // 既存のタグのみ追加可能にする
                if (_availableTags.Contains(trimmedTag))
                {
                    _searchCriteria.selectedTags.Add(trimmedTag);
                    _newTagInput = "";
                    _showTagSuggestions = false;
                    GUI.FocusControl(null);
                }
            }
        }
        private void DrawSelectedTags()
        {
            if (_searchCriteria.selectedTags.Count == 0)
            {
                EditorGUILayout.HelpBox("タグが選択されていません", MessageType.Info);
                return;
            }

            GUILayout.Label("選択されたタグ:", EditorStyles.miniLabel);

            // タグを詳細ウィンドウと同じスタイルで表示
            using (new EditorGUILayout.VerticalScope())
            {
                for (int i = _searchCriteria.selectedTags.Count - 1; i >= 0; i--)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var tagName = _searchCriteria.selectedTags[i];
                        var originalColor = GUI.color;

                        // タグの色を取得して背景色に設定
                        var tagColor = AssetTagManager.GetTagColor(tagName);
                        GUI.color = tagColor;

                        var tagContent = new GUIContent(tagName);
                        GUILayout.Button(tagContent, EditorStyles.miniButton);

                        GUI.color = originalColor;

                        // 削除ボタン
                        if (GUILayout.Button("×", GUILayout.Width(20)))
                        {
                            _searchCriteria.selectedTags.RemoveAt(i);
                        }
                    }
                }
            }

            GUILayout.Space(5);

            // 全てクリアボタン
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("すべてクリア", GUILayout.Width(100)))
                {
                    _searchCriteria.selectedTags.Clear();
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
                if (GUILayout.Button("クリア", GUILayout.Width(100), GUILayout.Height(30)))
                {
                    _searchCriteria = new AdvancedSearchCriteria();
                    InitializeTags();
                    _tagSearchQuery = "";
                    _newTagInput = "";
                    _showTagSuggestions = false;
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
