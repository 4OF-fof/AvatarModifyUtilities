using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using AMU.Data.TagType;

namespace AMU.Editor.TagType
{
    public class TagTypeManagerWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private int _selectedTab = 0;
        private string[] _tabNames = { "タグ", "タイプ" };

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
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnEnable()
        {
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

            EditorGUILayout.BeginVertical();

            // ヘッダー
            EditorGUILayout.LabelField("Tag & Type Manager", _headerStyle);
            EditorGUILayout.Space();

            // タブ
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames);
            EditorGUILayout.Space();

            // 検索フィルター
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("検索:", GUILayout.Width(40));
            _searchFilter = EditorGUILayout.TextField(_searchFilter);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            if (_selectedTab == 0)
            {
                DrawTagsTab();
            }
            else
            {
                DrawTypesTab();
            }

            EditorGUILayout.EndScrollView();

            // フッター
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("データリロード"))
            {
                TagTypeManager.LoadData();
            }
            if (GUILayout.Button("デフォルトにリセット"))
            {
                if (EditorUtility.DisplayDialog("確認", "データをデフォルト状態にリセットしますか？", "はい", "いいえ"))
                {
                    TagTypeManager.ResetToDefaults();
                }
            }
            if (GUILayout.Button("ファイルの場所を開く"))
            {
                EditorUtility.RevealInFinder(TagTypeManager.GetDataFilePath());
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawTagsTab()
        {
            // 新しいタグ追加
            EditorGUILayout.BeginVertical(_cardStyle);
            EditorGUILayout.LabelField("新しいタグを追加", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("名前:", GUILayout.Width(50));
            _newTagName = EditorGUILayout.TextField(_newTagName);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("色:", GUILayout.Width(50));
            _newTagColor = EditorGUILayout.ColorField(_newTagColor);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("追加", GUILayout.Width(100)))
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
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

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
            EditorGUILayout.BeginVertical(_cardStyle);

            EditorGUILayout.BeginHorizontal();

            // カラーインジケーター
            var color = Color.white;
            if (!ColorUtility.TryParseHtmlString(tag.color, out color))
            {
                color = Color.gray; // パースに失敗した場合はグレー
            }

            var rect = GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20), GUILayout.Height(20));
            EditorGUI.DrawRect(rect, color);

            // タグ情報
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(tag.name, EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            // ボタン
            EditorGUILayout.BeginVertical();
            if (GUILayout.Button("編集", GUILayout.Width(60)))
            {
                _editingTagId = tag.id;
            }
            if (GUILayout.Button("削除", GUILayout.Width(60)))
            {
                if (EditorUtility.DisplayDialog("確認", $"タグ '{tag.name}' を削除しますか？", "はい", "いいえ"))
                {
                    TagTypeManager.RemoveTag(tag.id);
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            // 編集モード
            if (_editingTagId == tag.id)
            {
                EditorGUILayout.Space();
                DrawTagEditForm(tag);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTagEditForm(TagItem tag)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("編集", EditorStyles.boldLabel);

            tag.name = EditorGUILayout.TextField("名前:", tag.name);

            Color color = Color.white;
            if (ColorUtility.TryParseHtmlString(tag.color, out color))
            {
                color = EditorGUILayout.ColorField("色:", color);
                tag.color = "#" + ColorUtility.ToHtmlStringRGB(color);
            }
            else
            {
                // パースに失敗した場合は白からスタート
                color = EditorGUILayout.ColorField("色:", Color.white);
                tag.color = "#" + ColorUtility.ToHtmlStringRGB(color);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("保存"))
            {
                TagTypeManager.UpdateTag(tag);
                _editingTagId = "";
            }
            if (GUILayout.Button("キャンセル"))
            {
                _editingTagId = "";
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawTypesTab()
        {
            // 新しいタイプ追加
            EditorGUILayout.BeginVertical(_cardStyle);
            EditorGUILayout.LabelField("新しいタイプを追加", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("名前:", GUILayout.Width(50));
            _newTypeName = EditorGUILayout.TextField(_newTypeName);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("説明:", GUILayout.Width(50));
            _newTypeDescription = EditorGUILayout.TextField(_newTypeDescription);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
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
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

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
            EditorGUILayout.BeginVertical(_cardStyle);

            EditorGUILayout.BeginHorizontal();

            // タイプ情報
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(type.name, EditorStyles.boldLabel);
            if (type.isDefault)
            {
                EditorGUILayout.LabelField("[デフォルト]", EditorStyles.miniLabel, GUILayout.Width(80));
            }
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(type.description))
            {
                EditorGUILayout.LabelField(type.description, EditorStyles.miniLabel);
            }
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            // ボタン
            EditorGUILayout.BeginVertical();
            if (GUILayout.Button("編集", GUILayout.Width(60)))
            {
                _editingTypeId = type.id;
            }
            if (!type.isDefault && GUILayout.Button("削除", GUILayout.Width(60)))
            {
                if (EditorUtility.DisplayDialog("確認", $"タイプ '{type.name}' を削除しますか？", "はい", "いいえ"))
                {
                    TagTypeManager.RemoveType(type.id);
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            // 編集モード
            if (_editingTypeId == type.id)
            {
                EditorGUILayout.Space();
                DrawTypeEditForm(type);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTypeEditForm(TypeItem type)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
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

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("保存"))
            {
                TagTypeManager.UpdateType(type);
                _editingTypeId = "";
            }
            if (GUILayout.Button("キャンセル"))
            {
                _editingTypeId = "";
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
    }
}
