using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using AMU.Editor.Core.Api;

using AMU.Editor.VrcAssetManager.Controller;

namespace AMU.Editor.VrcAssetManager.UI.Components
{
    public static class AssetTypePanelComponent
    {
        private static string _selectedAssetType = "";
        private static string _newTypeName = "";
        private static Vector2 _scrollPosition = Vector2.zero;
        private static bool _showDeleteButtons = false;

        public static void Draw()
        {
            var controller = AssetLibraryController.Instance;
            
            _selectedAssetType = controller.filterOptions.assetType;
            
            using (new GUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(236f)))
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    var typeHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 16,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                        padding = new RectOffset(8, 8, 8, 8)
                    };
                    GUILayout.Label(LocalizationAPI.GetText("VrcAssetManager_ui_assetTypePanel_header"), typeHeaderStyle);
                    GUILayout.FlexibleSpace();
                }
                GUILayout.Space(5);

                var typeButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Normal,
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(12, 12, 8, 8),
                    margin = new RectOffset(2, 2, 1, 1),
                    fixedHeight = 36,
                    normal = {
                        background = null,
                        textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black
                    },
                    hover = {
                        background = CreateColorTexture(new Color(0.3f, 0.3f, 0.3f, 0.5f)),
                        textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black
                    }
                };

                var selectedTypeButtonStyle = new GUIStyle(typeButtonStyle)
                {
                    fontStyle = FontStyle.Bold,
                    normal = {
                        background = CreateColorTexture(EditorGUIUtility.isProSkin ? new Color(0.24f, 0.48f, 0.90f, 0.8f) : new Color(0.24f, 0.48f, 0.90f, 0.6f)),
                        textColor = EditorGUIUtility.isProSkin ? Color.white : Color.white
                    },
                    hover = {
                        background = CreateColorTexture(EditorGUIUtility.isProSkin ? new Color(0.24f, 0.48f, 0.90f, 0.9f) : new Color(0.24f, 0.48f, 0.90f, 0.7f)),
                        textColor = EditorGUIUtility.isProSkin ? Color.white : Color.white
                    }
                };

                bool isAllSelected = string.IsNullOrEmpty(_selectedAssetType);
                bool allPressed = GUILayout.Toggle(isAllSelected, LocalizationAPI.GetText("VrcAssetManager_ui_assetTypePanel_all"),
                    isAllSelected ? selectedTypeButtonStyle : typeButtonStyle,
                    GUILayout.ExpandWidth(true), GUILayout.Height(36));

                if (allPressed && !isAllSelected)
                {
                    if (ToolbarComponent.isUsingAdvancedSearch)
                    {
                        controller.filterOptions.ClearFilter();
                        ToolbarComponent.isUsingAdvancedSearch = false;
                    }
                    
                    _selectedAssetType = controller.filterOptions.assetType = "";
                }

                GUILayout.Space(5);

                if (controller.HasUnCategorizedAssets())
                {
                    bool isUnCategorizedSelected = _selectedAssetType == "UNCATEGORIZED";
                    bool unCategorizedPressed = GUILayout.Toggle(isUnCategorizedSelected,
                        LocalizationAPI.GetText("VrcAssetManager_ui_assetTypePanel_uncategorized"),
                        isUnCategorizedSelected ? selectedTypeButtonStyle : typeButtonStyle,
                        GUILayout.ExpandWidth(true), GUILayout.Height(36));

                    if (unCategorizedPressed && !isUnCategorizedSelected)
                    {
                        if (ToolbarComponent.isUsingAdvancedSearch)
                        {
                            controller.filterOptions.ClearFilter();
                            ToolbarComponent.isUsingAdvancedSearch = false;
                        }
                        
                        _selectedAssetType = controller.filterOptions.assetType = "UNCATEGORIZED";
                    }
                }

                GUILayout.Space(8);

                var allButtonRect = GUILayoutUtility.GetRect(1, 2, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(allButtonRect, new Color(0.5f, 0.5f, 0.5f, 0.7f));


                GUILayout.Space(8);

                float scrollViewHeight = controller.HasUnCategorizedAssets() ? 519 : 555;

                using (var scrollView = new GUILayout.ScrollViewScope(_scrollPosition, GUILayout.Height(scrollViewHeight), GUILayout.ExpandWidth(true)))
                {
                    _scrollPosition = scrollView.scrollPosition;

                    var assetTypes = controller.GetAllAssetTypes().ToList();
                    for (int i = 0; i < assetTypes.Count; i++)
                    {
                        var assetType = assetTypes[i];
                        bool isSelected = _selectedAssetType == assetType;

                        var rowRect = GUILayoutUtility.GetRect(1, 36, GUILayout.ExpandWidth(true));
                        float deleteButtonWidth = (_showDeleteButtons ? 24f : 0f);
                        float arrowButtonsWidth = (_showDeleteButtons ? 48f : 0f); // 上下2つのボタン分
                        float spacing = (_showDeleteButtons ? 8f : 0f);
                        float totalWidth = deleteButtonWidth + arrowButtonsWidth + spacing;
                        
                        var typeRect = new Rect(rowRect.x + arrowButtonsWidth + (_showDeleteButtons ? 4f : 0f), rowRect.y, 
                            rowRect.width - totalWidth, rowRect.height);
                        var deleteRect = new Rect(rowRect.x + rowRect.width - deleteButtonWidth, 
                            rowRect.y + (rowRect.height - 28) / 2, 20, 28);
                        var upArrowRect = new Rect(rowRect.x, rowRect.y + 2, 20, 16);
                        var downArrowRect = new Rect(rowRect.x + 22, rowRect.y + 2, 20, 16);

                        if (_showDeleteButtons)
                        {
                            var arrowButtonStyle = new GUIStyle(GUI.skin.button)
                            {
                                fontSize = 10,
                                fixedWidth = 20,
                                fixedHeight = 16,
                                margin = new RectOffset(1, 1, 1, 1),
                                padding = new RectOffset(0, 0, 0, 0)
                            };

                            GUI.enabled = i > 0;
                            if (GUI.Button(upArrowRect, "▲", arrowButtonStyle))
                            {
                                controller.ReorderAssetType(i, i - 1);
                                if (_selectedAssetType == assetType)
                                {
                                    _selectedAssetType = controller.filterOptions.assetType = assetType;
                                }
                            }

                            GUI.enabled = i < assetTypes.Count - 1;
                            if (GUI.Button(downArrowRect, "▼", arrowButtonStyle))
                            {
                                controller.ReorderAssetType(i, i + 1);
                                if (_selectedAssetType == assetType)
                                {
                                    _selectedAssetType = controller.filterOptions.assetType = assetType;
                                }
                            }

                            GUI.enabled = true;
                        }

                        bool pressed = GUI.Toggle(typeRect, isSelected, assetType,
                            isSelected ? selectedTypeButtonStyle : typeButtonStyle);

                        if (pressed && !isSelected)
                        {
                            if (ToolbarComponent.isUsingAdvancedSearch)
                            {
                                controller.filterOptions.ClearFilter();
                                ToolbarComponent.isUsingAdvancedSearch = false;
                            }
                            
                            _selectedAssetType = controller.filterOptions.assetType = assetType;
                        }

                        if (_showDeleteButtons)
                        {
                            var deleteButtonStyle = new GUIStyle(GUI.skin.button)
                            {
                                fontSize = 12,
                                fontStyle = FontStyle.Bold,
                                fixedWidth = 20,
                                fixedHeight = 28,
                                normal = { textColor = Color.red }
                            };
                            if (GUI.Button(deleteRect, EditorGUIUtility.IconContent("d_winbtn_win_close"), deleteButtonStyle))
                            {
                                if (EditorUtility.DisplayDialog(
                                    LocalizationAPI.GetText("VrcAssetManager_ui_assetTypePanel_confirmDeleteTitle"),
                                    LocalizationAPI.GetText("VrcAssetManager_ui_assetTypePanel_confirmDeleteMessage"),
                                    LocalizationAPI.GetText("VrcAssetManager_common_delete"),
                                    LocalizationAPI.GetText("VrcAssetManager_common_cancel")))
                                {
                                    controller.RemoveAssetType(assetType);
                                    if (_selectedAssetType == assetType)
                                    {
                                        _selectedAssetType = controller.filterOptions.assetType = "";
                                    }
                                }
                            }
                        }

                        GUILayout.Space(2);
                    }
                }

                GUILayout.Space(8);

                var rect = GUILayoutUtility.GetRect(1, 2, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.7f));

                GUILayout.Space(8);

                using (new GUILayout.VerticalScope())
                {
                    var labelStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        fontSize = 12,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter
                    };
                    GUILayout.Label(LocalizationAPI.GetText("VrcAssetManager_ui_assetTypePanel_addNewType"), labelStyle);

                    GUILayout.Space(4);

                    using (new GUILayout.HorizontalScope())
                    {
                        var textFieldStyle = new GUIStyle(EditorStyles.textField)
                        {
                            fontSize = 12,
                            fixedHeight = 24
                        };
                        _newTypeName = EditorGUILayout.TextField(_newTypeName, textFieldStyle, GUILayout.ExpandWidth(true));

                        GUI.enabled = !string.IsNullOrWhiteSpace(_newTypeName) &&
                                    !controller.GetAllAssetTypes().Contains(_newTypeName.Trim());

                        var addButtonStyle = new GUIStyle(GUI.skin.button)
                        {
                            fontSize = 14,
                            fontStyle = FontStyle.Bold,
                            fixedWidth = 30,
                            fixedHeight = 24
                        };

                        if (GUILayout.Button(EditorGUIUtility.IconContent("Toolbar Plus"), addButtonStyle))
                        {
                            var trimmedName = _newTypeName.Trim();
                            controller.AddAssetType(trimmedName);
                            _newTypeName = "";
                        }

                        GUI.enabled = true;
                    }

                    var messageAreaRect = GUILayoutUtility.GetRect(0, 16, GUILayout.ExpandWidth(true));

                    if (!string.IsNullOrWhiteSpace(_newTypeName))
                    {
                        var trimmedName = _newTypeName.Trim();
                        var messageStyle = new GUIStyle(EditorStyles.miniLabel)
                        {
                            fontSize = 10,
                            normal = { textColor = Color.red }
                        };

                        string errorMessage = "";
                        if (controller.GetAllAssetTypes().Contains(trimmedName))
                        {
                            errorMessage = LocalizationAPI.GetText("VrcAssetManager_ui_assetTypePanel_typeAlreadyExists");
                        }
                        else if (string.IsNullOrWhiteSpace(trimmedName))
                        {
                            errorMessage = LocalizationAPI.GetText("VrcAssetManager_ui_assetTypePanel_typeNameRequired");
                        }

                        if (!string.IsNullOrEmpty(errorMessage))
                        {
                            var labelRect = new Rect(messageAreaRect.x + 5, messageAreaRect.y, messageAreaRect.width - 5, messageAreaRect.height);
                            GUI.Label(labelRect, errorMessage, messageStyle);
                        }
                    }
                }

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    bool newShowDeleteButtons = GUILayout.Toggle(_showDeleteButtons,
                        LocalizationAPI.GetText("VrcAssetManager_ui_assetTypePanel_showDeleteButtons"));

                    if (newShowDeleteButtons != _showDeleteButtons)
                    {
                        _showDeleteButtons = newShowDeleteButtons;
                    }
                    GUILayout.FlexibleSpace();
                }
                GUILayout.Space(10);
            }
        }

        private static Texture2D CreateColorTexture(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}