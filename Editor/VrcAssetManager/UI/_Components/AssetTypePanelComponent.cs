using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AMU.Editor.Core.Api;

using AMU.Editor.VrcAssetManager.Controller;

namespace AMU.Editor.VrcAssetManager.UI.Components
{
    public static class AssetTypePanelComponent
    {
        // Mock data for display purposes only
        private static List<string> _mockAssetTypes = new List<string>
        {
            "Avatar",
            "Accessories", 
            "Clothing",
            "Hair",
            "Eyes",
            "Face",
            "Body",
            "Shader",
            "Texture",
            "Animation",
            "VFX",
            "Tools"
        };

        private static string _selectedAssetType = "";
        private static string _newTypeName = "";
        private static Vector2 _scrollPosition = Vector2.zero;
        private static float _panelWidth = 240f;

        // Cached styles
        private static GUIStyle _typeButtonStyle;
        private static GUIStyle _selectedTypeButtonStyle;
        private static GUIStyle _typeHeaderStyle;
        private static bool _stylesInitialized = false;

        public static void Draw(AssetLibraryController controller)
        {
            InitializeStyles();

            using (new GUILayout.VerticalScope(GUILayout.Width(_panelWidth)))
            {
                // Header with improved style
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Asset Types", _typeHeaderStyle);
                    GUILayout.FlexibleSpace();
                }
                GUILayout.Space(5);

                // Asset types list with scroll view
                using (var scrollView = new GUILayout.ScrollViewScope(_scrollPosition, GUILayout.ExpandHeight(true)))
                {
                    _scrollPosition = scrollView.scrollPosition;

                    // All types button with improved style
                    bool isAllSelected = string.IsNullOrEmpty(_selectedAssetType);
                    bool allPressed = GUILayout.Toggle(isAllSelected, LocalizationAPI.GetText("AssetType_all"), 
                        isAllSelected ? _selectedTypeButtonStyle : _typeButtonStyle, 
                        GUILayout.ExpandWidth(true), GUILayout.Height(36));

                    if (allPressed && !isAllSelected)
                    {
                        _selectedAssetType = "";
                    }

                    GUILayout.Space(8);

                    // Individual type buttons with improved styles
                    foreach (var assetType in _mockAssetTypes)
                    {
                        bool isSelected = _selectedAssetType == assetType;

                        using (new GUILayout.HorizontalScope())
                        {
                            bool pressed = GUILayout.Toggle(isSelected, assetType, 
                                isSelected ? _selectedTypeButtonStyle : _typeButtonStyle, 
                                GUILayout.ExpandWidth(true), GUILayout.Height(36));

                            if (pressed && !isSelected)
                            {
                                _selectedAssetType = assetType;
                            }

                            // Show delete button for custom types (mock - always show for demo)
                            if (assetType != "Avatar" && assetType != "Accessories") // Simulate default types
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
                                    // Mock deletion confirmation
                                    Debug.Log($"Delete type requested: {assetType}");
                                }
                            }
                        }

                        GUILayout.Space(2);
                    }
                    GUILayout.Space(10);

                    // Add new type form at the bottom
                    DrawAddTypeForm();
                }
            }
        }

        private static void DrawAddTypeForm()
        {
            // Separator line
            var rect = GUILayoutUtility.GetRect(1, 2, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.7f));

            GUILayout.Space(8);

            // Add new type section with improved styling
            using (new GUILayout.VerticalScope())
            {
                var labelStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold
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
                                  !_mockAssetTypes.Contains(_newTypeName.Trim());

                    var addButtonStyle = new GUIStyle(GUI.skin.button)
                    {
                        fontSize = 14,
                        fontStyle = FontStyle.Bold,
                        fixedWidth = 30,
                        fixedHeight = 24
                    };

                    if (GUILayout.Button("+", addButtonStyle))
                    {
                        // Mock add functionality
                        var trimmedName = _newTypeName.Trim();
                        if (!_mockAssetTypes.Contains(trimmedName))
                        {
                            _mockAssetTypes.Add(trimmedName);
                            _newTypeName = "";
                            Debug.Log($"Added new type: {trimmedName}");
                        }
                    }

                    GUI.enabled = true;
                }

                // Show validation message if needed
                if (!string.IsNullOrWhiteSpace(_newTypeName))
                {
                    var trimmedName = _newTypeName.Trim();
                    var messageStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        fontSize = 10,
                        normal = { textColor = Color.red }
                    };

                    if (_mockAssetTypes.Contains(trimmedName))
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Space(5);
                            GUILayout.Label(LocalizationAPI.GetText("AssetType_typeAlreadyExists"), messageStyle);
                        }
                    }
                    else if (string.IsNullOrWhiteSpace(trimmedName))
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Space(5);
                            GUILayout.Label(LocalizationAPI.GetText("AssetType_typeNameRequired"), messageStyle);
                        }
                    }
                }
            }
        }

        private static void InitializeStyles()
        {
            if (_stylesInitialized) return;

            // Type header style
            _typeHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(8, 8, 8, 8)
            };

            // Type button style (unselected)
            _typeButtonStyle = new GUIStyle(GUI.skin.button)
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

            // Type button style (selected)
            _selectedTypeButtonStyle = new GUIStyle(_typeButtonStyle)
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

            _stylesInitialized = true;
        }

        /// <summary>
        /// Creates a solid color texture for UI backgrounds
        /// </summary>
        private static Texture2D CreateColorTexture(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        // Public accessors for state (to be used by the main window)
        public static string SelectedAssetType => _selectedAssetType;
        public static void SetSelectedAssetType(string type) => _selectedAssetType = type;
        public static void SetPanelWidth(float width) => _panelWidth = width;
    }
}