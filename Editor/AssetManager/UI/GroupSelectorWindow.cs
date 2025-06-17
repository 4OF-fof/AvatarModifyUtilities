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
        private float _thumbnailSize = 80f;  // グループ選択用のサムネイルサイズ

        public static void ShowWindow(AssetDataManager dataManager, Action<AssetInfo> onGroupSelected)
        {
            var window = GetWindow<GroupSelectorWindow>(true, LocalizationManager.GetText("GroupSelector_windowTitle"), true);
            window.minSize = new Vector2(500, 400);
            window.maxSize = new Vector2(500, 400);
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
            GUILayout.Space(10);

            GUILayout.Label(LocalizationManager.GetText("GroupSelector_selectGroup"), EditorStyles.boldLabel);
            GUILayout.Space(10);

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

            // グループリスト表示
            using (var scrollView = new GUILayout.ScrollViewScope(_scrollPosition, false, false))
            {
                _scrollPosition = scrollView.scrollPosition;

                // グリッド表示の準備
                var windowWidth = position.width - 20; // マージンを考慮
                var itemWidth = _thumbnailSize + 20; // サムネイル + 余白
                var itemsPerRow = Mathf.Max(1, Mathf.FloorToInt(windowWidth / itemWidth));

                for (int i = 0; i < _availableGroups.Count; i += itemsPerRow)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        for (int j = 0; j < itemsPerRow && i + j < _availableGroups.Count; j++)
                        {
                            var group = _availableGroups[i + j];
                            DrawGroupItem(group);
                        }
                        GUILayout.FlexibleSpace();
                    }
                    GUILayout.Space(5);
                }
            }

            GUILayout.Space(10);

            // ボタン領域
            using (new GUILayout.HorizontalScope())
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
        private void DrawGroupItem(AssetInfo group)
        {
            using (new GUILayout.VerticalScope(GUILayout.Width(_thumbnailSize + 10)))
            {
                // サムネイル描画
                var thumbnailRect = GUILayoutUtility.GetRect(_thumbnailSize, _thumbnailSize);
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
                DrawGroupName(group);

                // クリックイベントの処理
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && thumbnailRect.Contains(Event.current.mousePosition))
                {
                    _selectedGroup = group;
                    Repaint();
                    Event.current.Use();
                }

                // ダブルクリックで確定
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && Event.current.clickCount == 2 && thumbnailRect.Contains(Event.current.mousePosition))
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
        private void DrawGroupName(AssetInfo group)
        {
            var nameStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                alignment = TextAnchor.UpperCenter,
                fontSize = 10,
                richText = true
            };
            var availableWidth = _thumbnailSize + 10;

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
