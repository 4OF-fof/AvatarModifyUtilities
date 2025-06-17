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
        private AssetInfo _selectedGroup;
        private Vector2 _scrollPosition = Vector2.zero;
        private Action<AssetInfo> _onGroupSelected;
        private AssetDataManager _dataManager;
        private AssetThumbnailManager _thumbnailManager;

        public static void ShowWindow(AssetDataManager dataManager, Action<AssetInfo> onGroupSelected)
        {
            var window = GetWindow<GroupSelectorWindow>(true, LocalizationManager.GetText("GroupSelector_windowTitle"), true);
            window.minSize = new Vector2(600, 500);
            window.maxSize = new Vector2(600, 500);
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
            // サムネイルキャッシュのクリーンアップは不要（シングルトンなので）
        }

        private void LoadAvailableGroups()
        {
            if (_dataManager == null)
            {
                _availableGroups = new List<AssetInfo>();
                return;
            }

            // 利用可能なグループを取得
            _availableGroups = _dataManager.GetGroupAssets();
            _selectedGroup = _availableGroups.FirstOrDefault();
        }

        private void OnGUI()
        {
            using (new GUILayout.VerticalScope())
            {
                // ヘッダー部分（最小限のスペース）
                GUILayout.Space(5);
                GUILayout.Label(LocalizationManager.GetText("GroupSelector_selectGroup"), EditorStyles.boldLabel);
                GUILayout.Space(5);

                if (_availableGroups == null || _availableGroups.Count == 0)
                {
                    // グループがない場合
                    GUILayout.Label(LocalizationManager.GetText("GroupSelector_noGroups"), EditorStyles.helpBox);
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
        /// グループのグリッド表示を描画
        /// </summary>
        private void DrawGroupGrid()
        {
            // 固定ウィンドウサイズ（600x500）に最適化された固定値
            const int itemsPerRow = 4; // 600px幅で4列
            const float itemWidth = 140f; // 各アイテムの幅
            const float thumbnailSize = 110f; // サムネイルサイズ

            using (new GUILayout.VerticalScope())
            {
                for (int i = 0; i < _availableGroups.Count; i += itemsPerRow)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        for (int j = 0; j < itemsPerRow && i + j < _availableGroups.Count; j++)
                        {
                            var group = _availableGroups[i + j];
                            DrawGroupItem(group, itemWidth, thumbnailSize);
                        }

                        // 最後の行で残りスペースを埋める
                        if (i + itemsPerRow >= _availableGroups.Count)
                        {
                            var remainingItems = _availableGroups.Count - i;
                            for (int k = remainingItems; k < itemsPerRow; k++)
                            {
                                GUILayout.Space(itemWidth);
                            }
                        }
                    }
                    GUILayout.Space(2);
                }
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
        }

        /// <summary>
        /// グループアイテムを描画（メインウィンドウと同様のスタイル）
        /// </summary>
        private void DrawGroupItem(AssetInfo group, float itemWidth, float thumbnailSize)
        {
            using (new GUILayout.VerticalScope(GUILayout.Width(itemWidth)))
            {
                // サムネイル描画
                var thumbnailRect = GUILayoutUtility.GetRect(thumbnailSize, thumbnailSize);

                // アイテムを中央揃えにするための調整
                var centerOffset = (itemWidth - thumbnailSize) / 2;
                thumbnailRect.x += centerOffset;

                var isSelected = _selectedGroup == group;

                // 選択状態の描画
                if (isSelected)
                {
                    EditorGUI.DrawRect(thumbnailRect, new Color(0.3f, 0.5f, 1f, 0.3f));
                }

                // サムネイル取得と描画
                Texture2D thumbnail = _thumbnailManager?.GetThumbnail(group);
                if (thumbnail != null)
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

                // グループ名の描画
                DrawGroupName(group, itemWidth);

                // クリックイベントの処理（全体のアイテム領域で反応）
                var itemRect = GUILayoutUtility.GetLastRect();
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && itemRect.Contains(Event.current.mousePosition))
                {
                    _selectedGroup = group;
                    Repaint();
                    Event.current.Use();
                }

                // ダブルクリックで確定
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && Event.current.clickCount == 2 && itemRect.Contains(Event.current.mousePosition))
                {
                    _onGroupSelected?.Invoke(_selectedGroup);
                    Close();
                    Event.current.Use();
                }
            }
        }

        /// <summary>
        /// グループ名を描画（2行固定、アセット数表示付き）
        /// </summary>
        private void DrawGroupName(AssetInfo group, float availableWidth)
        {
            var nameStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                alignment = TextAnchor.UpperCenter,
                fontSize = 10,
                richText = true
            };

            // グループの子アセット数を取得
            var childCount = _dataManager?.GetGroupChildren(group.uid)?.Count ?? 0;
            var displayText = $"{group.name}\n({childCount}個)";

            // 2行固定の高さを設定
            var fixedHeight = nameStyle.lineHeight * 2 + 5;
            var rect = GUILayoutUtility.GetRect(availableWidth, fixedHeight);

            // テキストが長い場合は切り詰める
            var truncatedText = TruncateTextToFitHeight(displayText, nameStyle, availableWidth, fixedHeight);
            var content = new GUIContent(truncatedText, group.name); // ツールチップに完全な名前を表示

            GUI.Label(rect, content, nameStyle);
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
