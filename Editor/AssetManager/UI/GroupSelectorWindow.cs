using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using AMU.AssetManager.Data;
using AMU.AssetManager.Helper;
using AMU.Data.Lang;

namespace AMU.AssetManager.UI
{
    public class GroupSelectorWindow : EditorWindow
    {
        private List<AssetInfo> _availableGroups;
        private List<AssetInfo> _filteredGroups;
        private AssetInfo _selectedGroup;
        private Vector2 _scrollPosition = Vector2.zero;
        private Action<AssetInfo> _onGroupSelected;
        private AssetDataManager _dataManager;
        private AssetThumbnailManager _thumbnailManager;
        
        // 検索機能
        private string _searchText = "";
        private bool _isSearchFocused = false;

        // パフォーマンス最適化用のフィールド
        private Dictionary<string, Texture2D> _cachedThumbnails = new Dictionary<string, Texture2D>();
        private Dictionary<string, int> _cachedChildCounts = new Dictionary<string, int>();
        private HashSet<string> _requestedThumbnails = new HashSet<string>();

        // レイアウト定数
        private const int ITEMS_PER_ROW = 4;
        private const float ITEM_WIDTH = 140f;
        private const float THUMBNAIL_SIZE = 110f;
        private const float ITEM_HEIGHT = 150f; // サムネイル + 名前領域

        // バーチャルスクロール用
        private int _firstVisibleRow = 0;
        private int _lastVisibleRow = 0;
        private int _totalRows = 0;

        public static void ShowWindow(AssetDataManager dataManager, Action<AssetInfo> onGroupSelected)
        {
            var window = GetWindow<GroupSelectorWindow>(true, LocalizationManager.GetText("GroupSelector_windowTitle"), true);
            window.minSize = new Vector2(600, 540); // 検索フィールド分の高さを追加
            window.maxSize = new Vector2(600, 540);
            window._onGroupSelected = onGroupSelected;
            window._dataManager = dataManager;
            window._thumbnailManager = AssetThumbnailManager.Instance;
            window.LoadAvailableGroups();
            window.ShowModal();
        }

        private void OnEnable()
        {
            var language = EditorPrefs.GetString("Setting.Core_language", "ja_jp");
            LocalizationManager.LoadLanguage(language);
        }

        private void OnDisable()
        {
            // キャッシュをクリア
            foreach (var texture in _cachedThumbnails.Values)
            {
                if (texture != null && texture != _thumbnailManager?.GetDefaultThumbnail(null))
                {
                    DestroyImmediate(texture);
                }
            }
            _cachedThumbnails.Clear();
            _cachedChildCounts.Clear();
            _requestedThumbnails.Clear();
        }

        private void LoadAvailableGroups()
        {
            if (_dataManager == null)
            {
                _availableGroups = new List<AssetInfo>();
                _filteredGroups = new List<AssetInfo>();
                return;
            }

            // 利用可能なグループを取得
            _availableGroups = _dataManager.GetGroupAssets();
            _filteredGroups = new List<AssetInfo>(_availableGroups);
            _selectedGroup = _filteredGroups.FirstOrDefault();

            // レイアウト計算
            UpdateLayoutCalculation();

            // 子アセット数をキャッシュ
            _cachedChildCounts.Clear();
            foreach (var group in _availableGroups)
            {
                var childCount = _dataManager.GetGroupChildren(group.uid)?.Count ?? 0;
                _cachedChildCounts[group.uid] = childCount;
            }
        }
        
        private void UpdateLayoutCalculation()
        {
            _totalRows = Mathf.CeilToInt((float)_filteredGroups.Count / ITEMS_PER_ROW);
        }
        
        private void FilterGroups()
        {
            if (string.IsNullOrEmpty(_searchText))
            {
                _filteredGroups = new List<AssetInfo>(_availableGroups);
            }
            else
            {
                _filteredGroups = _availableGroups
                    .Where(group => group.name.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }
            
            UpdateLayoutCalculation();
            
            // 選択されたグループがフィルタリング後にも存在するかチェック
            if (_selectedGroup != null && !_filteredGroups.Contains(_selectedGroup))
            {
                _selectedGroup = _filteredGroups.FirstOrDefault();
            }
            
            _scrollPosition = Vector2.zero; // 検索時にスクロール位置をリセット
        }

        private void OnGUI()
        {
            using (new GUILayout.VerticalScope())
            {
                // ヘッダー部分
                GUILayout.Space(5);
                GUILayout.Label(LocalizationManager.GetText("GroupSelector_selectGroup"), EditorStyles.boldLabel);
                GUILayout.Space(5);
                
                // 検索フィールド
                DrawSearchField();
                GUILayout.Space(5);

                if (_filteredGroups == null || _filteredGroups.Count == 0)
                {
                    // グループがない場合
                    var message = string.IsNullOrEmpty(_searchText) 
                        ? LocalizationManager.GetText("GroupSelector_noGroups")
                        : "検索結果が見つかりません";
                    GUILayout.Label(message, EditorStyles.helpBox);
                    GUILayout.FlexibleSpace();

                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(LocalizationManager.GetText("GroupSelector_cancel"), GUILayout.Height(30), GUILayout.Width(100)))
                        {
                            Close();
                        }
                    }
                    return;
                }

                // スクロール可能なメインエリア（ウィンドウの大部分を使用）
                using (var scrollView = new GUILayout.ScrollViewScope(_scrollPosition, false, false, GUILayout.ExpandHeight(true)))
                {
                    _scrollPosition = scrollView.scrollPosition;
                    DrawGroupGrid();
                }

                // ボタン領域（固定高さ）
                GUILayout.Space(5);
                using (new GUILayout.HorizontalScope(GUILayout.Height(35)))
                {
                    if (GUILayout.Button(LocalizationManager.GetText("GroupSelector_add"), GUILayout.Height(30)))
                    {
                        if (_selectedGroup != null)
                        {
                            _onGroupSelected?.Invoke(_selectedGroup);
                            Close();
                        }
                    }

                    if (GUILayout.Button(LocalizationManager.GetText("GroupSelector_cancel"), GUILayout.Height(30)))
                    {
                        Close();
                    }
                }

                // キーボードイベント処理
                HandleKeyboardEvents();
            }
        }

        /// <summary>
        /// 検索フィールドを描画
        /// </summary>
        private void DrawSearchField()
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("検索:", GUILayout.Width(40));
                
                GUI.SetNextControlName("SearchField");
                var newSearchText = GUILayout.TextField(_searchText, GUILayout.ExpandWidth(true));
                
                if (newSearchText != _searchText)
                {
                    _searchText = newSearchText;
                    FilterGroups();
                    Repaint();
                }
                
                // クリアボタン
                if (GUILayout.Button("×", GUILayout.Width(25)))
                {
                    _searchText = "";
                    FilterGroups();
                    GUI.FocusControl(null);
                    Repaint();
                }
            }
            
            // フォーカス管理
            if (!_isSearchFocused && Event.current.type == EventType.Repaint)
            {
                GUI.FocusControl("SearchField");
                _isSearchFocused = true;
            }
        }

        /// <summary>
        /// グループのグリッド表示を描画（修正版）
        /// </summary>
        private void DrawGroupGrid()
        {
            if (_filteredGroups == null || _filteredGroups.Count == 0)
                return;

            // 表示可能な行数を計算
            var visibleHeight = position.height - 140; // ヘッダー、検索フィールド、ボタン領域を除く
            var visibleRows = Mathf.CeilToInt(visibleHeight / ITEM_HEIGHT) + 1; // 余裕を持って+1

            // スクロール位置から可視範囲を計算
            _firstVisibleRow = Mathf.Max(0, Mathf.FloorToInt(_scrollPosition.y / ITEM_HEIGHT));
            _lastVisibleRow = Mathf.Min(_totalRows - 1, _firstVisibleRow + visibleRows);

            // 上部の非表示領域のスペース
            if (_firstVisibleRow > 0)
            {
                GUILayout.Space(_firstVisibleRow * ITEM_HEIGHT);
            }

            // 可視範囲の行のみ描画
            for (int row = _firstVisibleRow; row <= _lastVisibleRow; row++)
            {
                using (new GUILayout.HorizontalScope())
                {
                    for (int col = 0; col < ITEMS_PER_ROW; col++)
                    {
                        int index = row * ITEMS_PER_ROW + col;
                        if (index < _filteredGroups.Count)
                        {
                            var group = _filteredGroups[index];
                            DrawGroupItem(group, ITEM_WIDTH, THUMBNAIL_SIZE);
                        }
                        else
                        {
                            // 空のスペースを描画
                            GUILayoutUtility.GetRect(ITEM_WIDTH, ITEM_HEIGHT);
                        }
                    }
                }
            }

            // 下部の非表示領域のスペース
            var remainingRows = _totalRows - (_lastVisibleRow + 1);
            if (remainingRows > 0)
            {
                GUILayout.Space(remainingRows * ITEM_HEIGHT);
            }

            // 可視範囲のサムネイルを非同期で要求
            RequestVisibleThumbnails();
        }

        /// <summary>
        /// 可視範囲のサムネイルを非同期で要求
        /// </summary>
        private void RequestVisibleThumbnails()
        {
            for (int row = _firstVisibleRow; row <= _lastVisibleRow; row++)
            {
                for (int col = 0; col < ITEMS_PER_ROW; col++)
                {
                    int index = row * ITEMS_PER_ROW + col;
                    if (index < _filteredGroups.Count)
                    {
                        var group = _filteredGroups[index];
                        if (!_cachedThumbnails.ContainsKey(group.uid) && !_requestedThumbnails.Contains(group.uid))
                        {
                            _requestedThumbnails.Add(group.uid);
                            RequestThumbnailAsync(group);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// サムネイルを非同期で要求
        /// </summary>
        private void RequestThumbnailAsync(AssetInfo group)
        {
            var thumbnail = _thumbnailManager?.GetThumbnail(group);
            if (thumbnail != null)
            {
                _cachedThumbnails[group.uid] = thumbnail;
                Repaint();
            }
        }

        /// <summary>
        /// キーボードイベントを処理
        /// </summary>
        private void HandleKeyboardEvents()
        {
            // Enterキーで確定
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
            {
                if (_selectedGroup != null)
                {
                    _onGroupSelected?.Invoke(_selectedGroup);
                    Close();
                }
                Event.current.Use();
            }

            // Escapeキーでキャンセル
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                Close();
                Event.current.Use();
            }
            
            // 矢印キーでの選択移動
            if (Event.current.type == EventType.KeyDown && _filteredGroups != null && _filteredGroups.Count > 0)
            {
                var currentIndex = _selectedGroup != null ? _filteredGroups.IndexOf(_selectedGroup) : -1;
                var newIndex = currentIndex;
                
                switch (Event.current.keyCode)
                {
                    case KeyCode.LeftArrow:
                        if (currentIndex > 0) newIndex = currentIndex - 1;
                        break;
                    case KeyCode.RightArrow:
                        if (currentIndex < _filteredGroups.Count - 1) newIndex = currentIndex + 1;
                        break;
                    case KeyCode.UpArrow:
                        if (currentIndex >= ITEMS_PER_ROW) newIndex = currentIndex - ITEMS_PER_ROW;
                        break;
                    case KeyCode.DownArrow:
                        if (currentIndex + ITEMS_PER_ROW < _filteredGroups.Count) newIndex = currentIndex + ITEMS_PER_ROW;
                        break;
                }
                
                if (newIndex != currentIndex && newIndex >= 0 && newIndex < _filteredGroups.Count)
                {
                    _selectedGroup = _filteredGroups[newIndex];
                    Event.current.Use();
                    Repaint();
                }
            }
        }

        /// <summary>
        /// グループアイテムを描画（最適化版）
        /// </summary>
        private void DrawGroupItem(AssetInfo group, float itemWidth, float thumbnailSize)
        {
            var isSelected = _selectedGroup == group;

            // アイテム全体の背景領域を描画（クリック判定用）
            var itemRect = GUILayoutUtility.GetRect(itemWidth, ITEM_HEIGHT);

            // 選択状態の描画（アイテム全体）
            if (isSelected)
            {
                EditorGUI.DrawRect(itemRect, new Color(0.3f, 0.5f, 1f, 0.2f));
            }

            // ホバー状態の描画
            if (itemRect.Contains(Event.current.mousePosition))
            {
                EditorGUI.DrawRect(itemRect, new Color(0.7f, 0.7f, 0.7f, 0.1f));
            }

            // サムネイル領域の計算
            var thumbnailRect = new Rect(
                itemRect.x + (itemWidth - thumbnailSize) / 2,
                itemRect.y + 5,
                thumbnailSize,
                thumbnailSize
            );

            // 選択状態のサムネイル境界線
            if (isSelected)
            {
                var borderRect = new Rect(thumbnailRect.x - 2, thumbnailRect.y - 2, thumbnailRect.width + 4, thumbnailRect.height + 4);
                EditorGUI.DrawRect(borderRect, new Color(0.3f, 0.5f, 1f, 0.8f));
            }

            // キャッシュされたサムネイルを使用
            if (_cachedThumbnails.TryGetValue(group.uid, out var thumbnail) && thumbnail != null)
            {
                GUI.DrawTexture(thumbnailRect, thumbnail, ScaleMode.ScaleToFit);
            }
            else
            {
                // グループのデフォルトアイコン（フォルダアイコン）
                var defaultIcon = EditorGUIUtility.IconContent("Folder Icon").image as Texture2D;
                if (defaultIcon != null)
                {
                    GUI.DrawTexture(thumbnailRect, defaultIcon, ScaleMode.ScaleToFit);
                }
                else
                {
                    GUI.Box(thumbnailRect, "フォルダ");
                }
            }

            // グループ名の描画領域
            var nameRect = new Rect(
                itemRect.x,
                itemRect.y + thumbnailSize + 10,
                itemWidth,
                itemRect.height - thumbnailSize - 10
            );

            // グループ名の描画
            DrawGroupNameInRect(group, nameRect);

            // クリックイベントの処理（アイテム全体で判定）
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && itemRect.Contains(Event.current.mousePosition))
            {
                _selectedGroup = group;
                Event.current.Use();
                Repaint();
            }

            // ダブルクリックで確定
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && Event.current.clickCount == 2 && itemRect.Contains(Event.current.mousePosition))
            {
                _onGroupSelected?.Invoke(_selectedGroup);
                Close();
                Event.current.Use();
            }
        }

        /// <summary>
        /// 指定されたRect内にグループ名を描画
        /// </summary>
        private void DrawGroupNameInRect(AssetInfo group, Rect rect)
        {
            var nameStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                alignment = TextAnchor.UpperCenter,
                fontSize = 10,
                richText = true
            };

            // キャッシュされた子アセット数を使用
            var childCount = _cachedChildCounts.TryGetValue(group.uid, out var count) ? count : 0;
            var displayText = $"{group.name}\n({childCount}個)";
            
            // 検索テキストがある場合はハイライト表示
            if (!string.IsNullOrEmpty(_searchText))
            {
                displayText = HighlightSearchText(displayText, _searchText);
            }

            // テキストが長い場合は切り詰める
            var truncatedText = TruncateTextToFitHeight(displayText, nameStyle, rect.width, rect.height);
            var content = new GUIContent(truncatedText, group.name); // ツールチップに完全な名前を表示

            GUI.Label(rect, content, nameStyle);
        }
        
        /// <summary>
        /// テキスト内の検索文字列をハイライト表示用にマークアップ
        /// </summary>
        private string HighlightSearchText(string text, string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
                return text;
                
            var index = text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var beforeText = text.Substring(0, index);
                var highlightText = text.Substring(index, searchText.Length);
                var afterText = text.Substring(index + searchText.Length);
                return $"{beforeText}<color=yellow>{highlightText}</color>{afterText}";
            }
            
            return text;
        }

        /// <summary>
        /// 指定された高さに収まるようにテキストを切り詰める
        /// </summary>
        private string TruncateTextToFitHeight(string text, GUIStyle style, float width, float maxHeight)
        {
            var testContent = new GUIContent(text);
            var textHeight = style.CalcHeight(testContent, width);

            if (textHeight <= maxHeight)
            {
                return text;
            }

            // テキストを段階的に短くしていく
            var lines = text.Split('\n');
            if (lines.Length > 1)
            {
                // 最初の行のみを使用し、必要に応じて切り詰める
                var firstLine = lines[0];
                var secondLine = lines.Length > 1 ? lines[1] : "";

                // 最初の行が長すぎる場合は切り詰める
                var maxChars = Mathf.Max(10, firstLine.Length);
                while (maxChars > 5)
                {
                    var truncated = firstLine.Length > maxChars ? firstLine.Substring(0, maxChars - 3) + "..." : firstLine;
                    var testText = truncated + "\n" + secondLine;
                    testContent = new GUIContent(testText);

                    if (style.CalcHeight(testContent, width) <= maxHeight)
                    {
                        return testText;
                    }
                    maxChars -= 2;
                }
            }

            // 単一行での切り詰め
            var words = text.Split(' ');
            var result = "";
            for (int i = 0; i < words.Length; i++)
            {
                var testText = string.IsNullOrEmpty(result) ? words[i] : result + " " + words[i];
                testContent = new GUIContent(testText);

                if (style.CalcHeight(testContent, width) > maxHeight)
                {
                    return result + "...";
                }
                result = testText;
            }

            return result;
        }
    }
}
