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

        public static void Draw(AssetLibraryController controller)
        {
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
                    GUILayout.Label("Asset Types", typeHeaderStyle);
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
                bool allPressed = GUILayout.Toggle(isAllSelected, LocalizationAPI.GetText("AssetType_all"),
                    isAllSelected ? selectedTypeButtonStyle : typeButtonStyle,
                    GUILayout.ExpandWidth(true), GUILayout.Height(36));

                if (allPressed && !isAllSelected)
                {
                    _selectedAssetType = controller.filterOptions.assetType = "";

                }

                GUILayout.Space(5);

                if (controller.HasUnCategorizedAssets())
                {
                    bool isUnCategorizedSelected = _selectedAssetType == "UNCATEGORIZED";
                    bool unCategorizedPressed = GUILayout.Toggle(isUnCategorizedSelected,
                        LocalizationAPI.GetText("AssetType_uncategorized"),
                        isUnCategorizedSelected ? selectedTypeButtonStyle : typeButtonStyle,
                        GUILayout.ExpandWidth(true), GUILayout.Height(36));

                    if (unCategorizedPressed && !isUnCategorizedSelected)
                    {
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

                    foreach (var assetType in controller.GetAllAssetTypes())
                    {
                        bool isSelected = _selectedAssetType == assetType;

                        using (new GUILayout.HorizontalScope())
                        {
                            bool pressed = GUILayout.Toggle(isSelected, assetType,
                                isSelected ? selectedTypeButtonStyle : typeButtonStyle,
                                GUILayout.ExpandWidth(true), GUILayout.Height(36));

                            if (pressed && !isSelected)
                            {
                                _selectedAssetType = controller.filterOptions.assetType = assetType;
                            }

                            if (_showDeleteButtons)
                            {
                                var deleteButtonStyle = new GUIStyle(GUI.skin.button)
                                {
                                    fontSize = 12,
                                    fontStyle = FontStyle.Bold,
                                    fixedWidth = 24,
                                    fixedHeight = 36,
                                    normal = { textColor = Color.red }
                                };

                                if (GUILayout.Button("×", deleteButtonStyle))
                                {
                                    if (EditorUtility.DisplayDialog(
                                        LocalizationAPI.GetText("AssetType_confirmDelete_title"),
                                        LocalizationAPI.GetText("AssetType_confirmDelete_message"),
                                        LocalizationAPI.GetText("Common_delete"),
                                        LocalizationAPI.GetText("Common_cancel")))
                                    {
                                        controller.RemoveAssetType(assetType);
                                        if (_selectedAssetType == assetType)
                                        {
                                            _selectedAssetType = controller.filterOptions.assetType = "";
                                        }
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
                    GUILayout.Label(LocalizationAPI.GetText("AssetType_addNewType"), labelStyle);

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

                        if (GUILayout.Button("+", addButtonStyle))
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
                            errorMessage = LocalizationAPI.GetText("AssetType_typeAlreadyExists");
                        }
                        else if (string.IsNullOrWhiteSpace(trimmedName))
                        {
                            errorMessage = LocalizationAPI.GetText("AssetType_typeNameRequired");
                        }

                        if (!string.IsNullOrEmpty(errorMessage))
                        {
                            var labelRect = new Rect(messageAreaRect.x + 5, messageAreaRect.y, messageAreaRect.width - 5, messageAreaRect.height);
                            GUI.Label(labelRect, errorMessage, messageStyle);
                        }
                    }
                }

                GUILayout.Space(5);

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    bool newShowDeleteButtons = GUILayout.Toggle(_showDeleteButtons,
                        LocalizationAPI.GetText("AssetType_showDeleteButtons"),
                        GUILayout.Width(150));

                    if (newShowDeleteButtons != _showDeleteButtons)
                    {
                        _showDeleteButtons = newShowDeleteButtons;
                    }
                    GUILayout.FlexibleSpace();
                }
                GUILayout.Space(5);
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