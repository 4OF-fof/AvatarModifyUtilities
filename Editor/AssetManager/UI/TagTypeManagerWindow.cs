using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using AMU.Data.TagType;
using AMU.Data.Lang;

namespace AMU.Editor.TagType
{
    public class TagTypeManagerWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private int _selectedTab = 0;
        private string[] _tabNames;

        // タグ編集用
        private string _newTagName = "";
        private Color _newTagColor = Color.white;
        private string _editingTagId = "";

        // タイプ編集用
        private string _newTypeName = "";
        private string _newTypeDescription = "";
        private string _editingTypeId = "";

        // フィルタリング
        private string _searchFilter = "";

        private GUIStyle _headerStyle;
        private GUIStyle _cardStyle;
        private bool _stylesInitialized = false;

        [MenuItem("AMU/Tag & Type Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<TagTypeManagerWindow>("Tag & Type Manager");
            window.minSize = new Vector2(400, 800);
            window.maxSize = new Vector2(400, 800);
            window.Show();
        }
        private void OnEnable()
        {
            var language = EditorPrefs.GetString("Setting.Core_language", "ja_jp");
            LocalizationManager.LoadLanguage(language);

            // タブ名を再初期化
            _tabNames = new string[] { LocalizationManager.GetText("TagTypeManager_tabs_tags"), LocalizationManager.GetText("TagTypeManager_tabs_types") };

            TagTypeManager.OnDataChanged += Repaint;
        }

        private void OnDisable()
        {
            TagTypeManager.OnDataChanged -= Repaint;
        }

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };

            _cardStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(5, 5, 2, 2)
            };

            _stylesInitialized = true;
        }
        private void OnGUI()
        {
            InitializeStyles();

            using (new EditorGUILayout.VerticalScope())
            {
                // ヘッダー
                EditorGUILayout.LabelField("Tag & Type Manager", _headerStyle);
                EditorGUILayout.Space();

                // タブ
                _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames);
                EditorGUILayout.Space();

                // 検索フィルター
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(LocalizationManager.GetText("TagTypeManager_search"), GUILayout.Width(40));
                    _searchFilter = EditorGUILayout.TextField(_searchFilter);
                }
                EditorGUILayout.Space();

                using (var scrollView = new EditorGUILayout.ScrollViewScope(_scrollPosition))
                {
                    _scrollPosition = scrollView.scrollPosition;

                    if (_selectedTab == 0)
                    {
                        DrawTagsTab();
                    }
                    else
                    {
                        DrawTypesTab();
                    }
                }

                // フッター
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(LocalizationManager.GetText("TagTypeManager_reloadData")))
                    {
                        TagTypeManager.LoadData();
                    }
                    if (GUILayout.Button(LocalizationManager.GetText("TagTypeManager_resetToDefault")))
                    {
                        if (EditorUtility.DisplayDialog(LocalizationManager.GetText("Common_warning"), LocalizationManager.GetText("TagTypeManager_resetConfirm"), LocalizationManager.GetText("Common_yes"), LocalizationManager.GetText("Common_no")))
                        {
                            TagTypeManager.ResetToDefaults();
                        }
                    }
                    if (GUILayout.Button(LocalizationManager.GetText("TagTypeManager_openFileLocation")))
                    {
                        EditorUtility.RevealInFinder(TagTypeManager.GetDataFilePath());
                    }
                }
            }
        }
        private void DrawTagsTab()
        {
            // 新しいタグ追加
            using (new EditorGUILayout.VerticalScope(_cardStyle))
            {
                EditorGUILayout.LabelField(LocalizationManager.GetText("TagTypeManager_addNewTag"), EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(LocalizationManager.GetText("TagTypeManager_name"), GUILayout.Width(50));
                    _newTagName = EditorGUILayout.TextField(_newTagName);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(LocalizationManager.GetText("TagTypeManager_color"), GUILayout.Width(50));
                    _newTagColor = EditorGUILayout.ColorField(_newTagColor);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(LocalizationManager.GetText("TagTypeManager_add"), GUILayout.Width(100)))
                    {
                        if (!string.IsNullOrWhiteSpace(_newTagName))
                        {
                            var colorHex = "#" + ColorUtility.ToHtmlStringRGB(_newTagColor);
                            var newTag = new TagItem(_newTagName, colorHex);
                            TagTypeManager.AddTag(newTag);
                            _newTagName = "";
                            _newTagColor = Color.white;
                        }
                    }
                }
            }

            EditorGUILayout.Space();

            // タグ一覧
            var tags = TagTypeManager.GetAllTags();
            if (!string.IsNullOrEmpty(_searchFilter))
            {
                tags = tags.Where(t => t.name.ToLower().Contains(_searchFilter.ToLower())).ToList();
            }

            foreach (var tag in tags.OrderBy(t => t.name))
            {
                DrawTagItem(tag);
            }
        }
        private void DrawTagItem(TagItem tag)
        {
            using (new EditorGUILayout.VerticalScope(_cardStyle))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    // カラーインジケーター
                    var color = Color.white;
                    if (!ColorUtility.TryParseHtmlString(tag.color, out color))
                    {
                        color = Color.gray; // パースに失敗した場合はグレー
                    }

                    var rect = GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20), GUILayout.Height(20));
                    EditorGUI.DrawRect(rect, color);

                    // タグ情報（改善版：より見やすく）
                    using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                    {
                        var nameStyle = new GUIStyle(EditorStyles.boldLabel)
                        {
                            wordWrap = true,
                            richText = true
                        };

                        var nameContent = new GUIContent(tag.name, tag.name); // ツールチップとして完全な名前を表示
                        EditorGUILayout.LabelField(nameContent, nameStyle, GUILayout.ExpandWidth(true));
                    }

                    // ボタン
                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(120)))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button(LocalizationManager.GetText("TagTypeManager_edit"), GUILayout.Width(55)))
                            {
                                _editingTagId = tag.id;
                            }
                            if (GUILayout.Button(LocalizationManager.GetText("TagTypeManager_delete"), GUILayout.Width(55)))
                            {
                                if (EditorUtility.DisplayDialog(LocalizationManager.GetText("Common_warning"), string.Format(LocalizationManager.GetText("TagTypeManager_deleteTagConfirm"), tag.name), LocalizationManager.GetText("Common_yes"), LocalizationManager.GetText("Common_no")))
                                {
                                    TagTypeManager.RemoveTag(tag.id);
                                }
                            }
                        }
                    }
                }

                // 編集モード
                if (_editingTagId == tag.id)
                {
                    EditorGUILayout.Space();
                    DrawTagEditForm(tag);
                }
            }
        }
        private void DrawTagEditForm(TagItem tag)
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField(LocalizationManager.GetText("TagTypeManager_editTag"), EditorStyles.boldLabel);

                tag.name = EditorGUILayout.TextField(LocalizationManager.GetText("TagTypeManager_name"), tag.name);

                Color color = Color.white; if (ColorUtility.TryParseHtmlString(tag.color, out color))
                {
                    color = EditorGUILayout.ColorField(LocalizationManager.GetText("TagTypeManager_color"), color);
                    tag.color = "#" + ColorUtility.ToHtmlStringRGB(color);
                }
                else
                {
                    // パースに失敗した場合は白からスタート
                    color = EditorGUILayout.ColorField(LocalizationManager.GetText("TagTypeManager_color"), Color.white);
                    tag.color = "#" + ColorUtility.ToHtmlStringRGB(color);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(LocalizationManager.GetText("TagTypeManager_save")))
                    {
                        TagTypeManager.UpdateTag(tag);
                        _editingTagId = "";
                    }
                    if (GUILayout.Button(LocalizationManager.GetText("Common_cancel")))
                    {
                        _editingTagId = "";
                    }
                }
            }
        }
        private void DrawTypesTab()
        {
            // 新しいタイプ追加
            using (new EditorGUILayout.VerticalScope(_cardStyle))
            {
                EditorGUILayout.LabelField("新しいタイプを追加", EditorStyles.boldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("名前:", GUILayout.Width(50));
                    _newTypeName = EditorGUILayout.TextField(_newTypeName);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("説明:", GUILayout.Width(50));
                    _newTypeDescription = EditorGUILayout.TextField(_newTypeDescription);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("追加", GUILayout.Width(100)))
                    {
                        if (!string.IsNullOrWhiteSpace(_newTypeName))
                        {
                            var newType = new TypeItem(_newTypeName, _newTypeDescription, false);
                            TagTypeManager.AddType(newType);
                            _newTypeName = "";
                            _newTypeDescription = "";
                        }
                    }
                }
            }

            EditorGUILayout.Space();

            // タイプ一覧
            var types = TagTypeManager.GetAllTypes();
            if (!string.IsNullOrEmpty(_searchFilter))
            {
                types = types.Where(t => t.name.ToLower().Contains(_searchFilter.ToLower())).ToList();
            }

            foreach (var type in types.OrderBy(t => t.sortOrder).ThenBy(t => t.name))
            {
                DrawTypeItem(type);
            }
        }
        private void DrawTypeItem(TypeItem type)
        {
            using (new EditorGUILayout.VerticalScope(_cardStyle))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    // タイプ情報（改善版：より見やすく）
                    using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            var nameStyle = new GUIStyle(EditorStyles.boldLabel)
                            {
                                wordWrap = true,
                                richText = true
                            };

                            var nameContent = new GUIContent(type.name, type.name); // ツールチップとして完全な名前を表示
                            EditorGUILayout.LabelField(nameContent, nameStyle, GUILayout.ExpandWidth(true));

                            if (type.isDefault)
                            {
                                var defaultStyle = new GUIStyle(EditorStyles.miniLabel)
                                {
                                    normal = { textColor = new Color(0.7f, 0.7f, 1f, 1f) },
                                    fontStyle = FontStyle.Italic
                                };
                                EditorGUILayout.LabelField("[デフォルト]", defaultStyle, GUILayout.Width(80));
                            }
                        }

                        if (!string.IsNullOrEmpty(type.description))
                        {
                            var descStyle = new GUIStyle(EditorStyles.miniLabel)
                            {
                                wordWrap = true,
                                richText = true
                            };

                            var descContent = new GUIContent(type.description, type.description);
                            EditorGUILayout.LabelField(descContent, descStyle, GUILayout.ExpandWidth(true));
                        }
                    }

                    // ボタン
                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(120)))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button("編集", GUILayout.Width(55)))
                            {
                                _editingTypeId = type.id;
                            }
                            if (!type.isDefault && GUILayout.Button("削除", GUILayout.Width(55)))
                            {
                                if (EditorUtility.DisplayDialog("確認", $"タイプ '{type.name}' を削除しますか？", "はい", "いいえ"))
                                {
                                    TagTypeManager.RemoveType(type.id);
                                }
                            }
                        }
                    }
                }                // 編集モード
                if (_editingTypeId == type.id)
                {
                    EditorGUILayout.Space();
                    DrawTypeEditForm(type);
                }
            }
        }

        private void DrawTypeEditForm(TypeItem type)
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField("編集", EditorStyles.boldLabel);

                if (!type.isDefault)
                {
                    type.name = EditorGUILayout.TextField("名前:", type.name);
                }
                else
                {
                    EditorGUILayout.LabelField("名前:", type.name);
                }

                type.description = EditorGUILayout.TextField("説明:", type.description);
                type.isVisible = EditorGUILayout.Toggle("表示:", type.isVisible);
                type.sortOrder = EditorGUILayout.IntField("ソート順:", type.sortOrder);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("保存"))
                    {
                        TagTypeManager.UpdateType(type);
                        _editingTypeId = "";
                    }
                    if (GUILayout.Button("キャンセル"))
                    {
                        _editingTypeId = "";
                    }
                }
            }
        }
    }
}
