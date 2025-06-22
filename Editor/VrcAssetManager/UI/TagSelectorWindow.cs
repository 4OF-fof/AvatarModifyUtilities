using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using AMU.Editor.VrcAssetManager.Controller;
using AMU.Editor.Core.Api;
using AMU.Editor.Core.Controller;

namespace AMU.Editor.VrcAssetManager.UI
{
    public class TagSelectorWindow : EditorWindow
    {
        // ウィンドウの設定
        private bool _allowMultipleSelection;
        private Action<List<string>> _onTagsSelected;
        
        // タグデータ
        private List<string> _availableTags;
        private List<string> _filteredTags;
        private HashSet<string> _selectedTags = new HashSet<string>();
        
        // UI状態
        private Vector2 _scrollPosition = Vector2.zero;
        private string _searchText = "";
        private bool _isSearchFocused = false;
        
        // レイアウト定数
        private const int ITEMS_PER_ROW = 1; // 縦一列に変更
        private const float ITEM_WIDTH = 300f; // 幅を広く
        private const float ITEM_HEIGHT = 30f; // 高さを少し低く
        private const float WINDOW_PADDING = 10f;
        
        // スタイル
        private GUIStyle _selectedTagStyle;
        private GUIStyle _unselectedTagStyle;
        private bool _stylesInitialized = false;

        /// <summary>
        /// タグ選択ウィンドウを表示します
        /// </summary>
        /// <param name="allowMultipleSelection">複数選択を許可するかどうか</param>
        /// <param name="onTagsSelected">選択完了時のコールバック</param>
        /// <param name="initialSelectedTags">初期選択されるタグのリスト（オプション）</param>
        public static void ShowWindow(bool allowMultipleSelection, Action<List<string>> onTagsSelected, List<string> initialSelectedTags = null)
        {
            var window = GetWindow<TagSelectorWindow>(true, "Tag Selector", true);
            window.minSize = new Vector2(350, 500); // 縦長に変更
            window.maxSize = new Vector2(350, 800); // 縦長に変更
            window._allowMultipleSelection = allowMultipleSelection;
            window._onTagsSelected = onTagsSelected;
            window.LoadAvailableTags();
            window.SetInitialSelection(initialSelectedTags);
            window.FilterTags();
        }

        private void OnEnable()
        {
            // 言語設定を取得
            var language = SettingsAPI.GetSetting<string>("Core_language");
            LocalizationController.LoadLanguage(language);
        }

        private void LoadAvailableTags()
        {
            try
            {
                var controller = new AssetLibraryController();
                controller.InitializeLibrary();
                _availableTags = controller.GetAllTags().ToList();
                _availableTags.Sort(); // アルファベット順にソート
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TagSelectorWindow] Failed to load tags: {ex.Message}");
                _availableTags = new List<string>();
            }
        }

        private void SetInitialSelection(List<string> initialSelectedTags)
        {
            _selectedTags.Clear();
            
            if (initialSelectedTags != null && initialSelectedTags.Count > 0)
            {
                foreach (var tag in initialSelectedTags)
                {
                    // 利用可能なタグリストに存在するタグのみを選択状態にする
                    if (_availableTags.Contains(tag))
                    {
                        if (_allowMultipleSelection)
                        {
                            _selectedTags.Add(tag);
                        }
                        else
                        {
                            // 単一選択の場合は最初の有効なタグのみを選択
                            _selectedTags.Add(tag);
                            break;
                        }
                    }
                }
            }
        }

        private void FilterTags()
        {
            if (string.IsNullOrEmpty(_searchText))
            {
                _filteredTags = new List<string>(_availableTags);
            }
            else
            {
                _filteredTags = _availableTags
                    .Where(tag => tag.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }
        }

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            _selectedTagStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { background = Texture2D.whiteTexture, textColor = Color.white },
                hover = { background = Texture2D.whiteTexture, textColor = Color.white },
                active = { background = Texture2D.whiteTexture, textColor = Color.white },
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            _unselectedTagStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter
            };

            _stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitializeStyles();

            using (new GUILayout.VerticalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                DrawHeader();
                DrawSearchField();
                DrawTagList();
                DrawFooter();
            }
        }

        private void DrawHeader()
        {
            GUILayout.Space(WINDOW_PADDING);
            
            string headerText = _allowMultipleSelection 
                ? LocalizationController.GetText("TagSelector_selectMultipleTags") ?? "タグを選択してください（複数選択可）"
                : LocalizationController.GetText("TagSelector_selectSingleTag") ?? "タグを選択してください（単一選択）";
                
            GUILayout.Label(headerText, EditorStyles.boldLabel);
            GUILayout.Space(5);
        }

        private void DrawSearchField()
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(LocalizationController.GetText("TagSelector_search") ?? "検索:", GUILayout.Width(50));
                
                GUI.SetNextControlName("SearchField");
                var newSearchText = GUILayout.TextField(_searchText);
                
                if (newSearchText != _searchText)
                {
                    _searchText = newSearchText;
                    FilterTags();
                }
                
                if (GUILayout.Button("×", GUILayout.Width(25)))
                {
                    _searchText = "";
                    FilterTags();
                    GUI.FocusControl(null);
                }
            }
            
            GUILayout.Space(10);
        }

        private void DrawTagList()
        {
            if (_filteredTags == null || _filteredTags.Count == 0)
            {
                var message = string.IsNullOrEmpty(_searchText)
                    ? LocalizationController.GetText("TagSelector_noTags") ?? "利用可能なタグがありません"
                    : LocalizationController.GetText("TagSelector_noSearchResults") ?? "検索結果が見つかりません";
                    
                GUILayout.Label(message, EditorStyles.helpBox);
                return;
            }

            // helpboxでタグリストを囲む
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (var scrollView = new GUILayout.ScrollViewScope(_scrollPosition, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
                {
                    _scrollPosition = scrollView.scrollPosition;
                    DrawTagGrid();
                }
            }
        }

        private void DrawTagGrid()
        {
            // 縦一列に配置
            using (new GUILayout.VerticalScope())
            {
                foreach (var tag in _filteredTags)
                {
                    DrawTagButton(tag);
                    GUILayout.Space(2); // タグ間の間隔
                }
            }
        }

        private void DrawTagButton(string tag)
        {
            bool isSelected = _selectedTags.Contains(tag);
            var style = isSelected ? _selectedTagStyle : _unselectedTagStyle;
            
            // 選択されているタグの背景色を設定
            if (isSelected)
            {
                var originalColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.3f, 0.6f, 1f, 1f); // 青色
                
                if (GUILayout.Button(tag, style, GUILayout.ExpandWidth(true), GUILayout.Height(ITEM_HEIGHT)))
                {
                    OnTagClicked(tag);
                }
                
                GUI.backgroundColor = originalColor;
            }
            else
            {
                if (GUILayout.Button(tag, style, GUILayout.ExpandWidth(true), GUILayout.Height(ITEM_HEIGHT)))
                {
                    OnTagClicked(tag);
                }
            }
        }

        private void OnTagClicked(string tag)
        {
            if (_allowMultipleSelection)
            {
                // 複数選択モード
                if (_selectedTags.Contains(tag))
                {
                    _selectedTags.Remove(tag);
                }
                else
                {
                    _selectedTags.Add(tag);
                }
            }
            else
            {
                // 単一選択モード
                _selectedTags.Clear();
                _selectedTags.Add(tag);
                
                // 単一選択の場合は即座にウィンドウを閉じて結果を返す
                CompleteSelection();
            }
        }

        private void DrawFooter()
        {
            GUILayout.Space(10);
            
            // 選択状況の表示
            string selectionInfo = _allowMultipleSelection 
                ? $"{LocalizationController.GetText("TagSelector_selectedCount") ?? "選択済み"}: {_selectedTags.Count}"
                : _selectedTags.Count > 0 
                    ? $"{LocalizationController.GetText("TagSelector_selected") ?? "選択中"}: {_selectedTags.First()}"
                    : LocalizationController.GetText("TagSelector_noSelection") ?? "未選択";
                    
            GUILayout.Label(selectionInfo, EditorStyles.miniLabel);
            GUILayout.Space(5);
            
            // ボタンを縦に配置
            if (_allowMultipleSelection)
            {
                if (GUILayout.Button(LocalizationController.GetText("TagSelector_clearAll") ?? "全て解除"))
                {
                    _selectedTags.Clear();
                }
                GUILayout.Space(3);
            }
            
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button(LocalizationController.GetText("Common_cancel") ?? "キャンセル"))
                {
                    Close();
                }
                
                GUI.enabled = _selectedTags.Count > 0;
                if (GUILayout.Button(LocalizationController.GetText("Common_ok") ?? "OK"))
                {
                    CompleteSelection();
                }
                GUI.enabled = true;
            }
            
            GUILayout.Space(WINDOW_PADDING);
        }

        private void CompleteSelection()
        {
            var selectedTagsList = _selectedTags.ToList();
            _onTagsSelected?.Invoke(selectedTagsList);
            Close();
        }

        private void OnDestroy()
        {
            // ウィンドウが閉じられた時のクリーンアップ
            _onTagsSelected = null;
        }
    }
}
