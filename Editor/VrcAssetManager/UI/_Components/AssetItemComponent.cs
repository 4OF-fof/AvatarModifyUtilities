using System;
using UnityEditor;
using UnityEngine;

namespace AMU.Editor.VrcAssetManager.UI.Components
{
    [Serializable]
    public class MockAssetInfo
    {
        public string uid;
        public string name;
        public string assetType;
        public bool isFavorite;
        public bool isHidden;
        public bool isGroup;
        public DateTime createdDate;
        public long fileSize;

        public MockAssetInfo(string name, string type, bool favorite = false, bool hidden = false, bool group = false)
        {
            this.uid = Guid.NewGuid().ToString();
            this.name = name;
            this.assetType = type;
            this.isFavorite = favorite;
            this.isHidden = hidden;
            this.isGroup = group;
            this.createdDate = DateTime.Now.AddDays(UnityEngine.Random.Range(-30, 0));
            this.fileSize = UnityEngine.Random.Range(1024, 50 * 1024 * 1024);
        }
    }

    public class AssetItemComponent
    {
        private float _thumbnailSize = 110f;

        public void Draw(MockAssetInfo asset, bool isSelected, bool isMultiSelected, System.Action<MockAssetInfo> onLeftClick, System.Action<MockAssetInfo> onRightClick)
        {
            using (new GUILayout.VerticalScope(GUILayout.Width(_thumbnailSize + 10)))
            {
                // Thumbnail
                var thumbnailRect = GUILayoutUtility.GetRect(_thumbnailSize, _thumbnailSize);

                // Selection background
                if (isSelected && isMultiSelected)
                {
                    // Main selection in multi-select
                    EditorGUI.DrawRect(thumbnailRect, new Color(0.3f, 0.5f, 1f, 0.5f));
                }
                else if (isMultiSelected)
                {
                    // Sub selection in multi-select
                    EditorGUI.DrawRect(thumbnailRect, new Color(0.3f, 0.5f, 1f, 0.3f));
                }
                else if (isSelected)
                {
                    // Single selection
                    EditorGUI.DrawRect(thumbnailRect, new Color(0.3f, 0.5f, 1f, 0.3f));
                }

                // Draw thumbnail (default icon based on type)
                var defaultIcon = GetDefaultIcon(asset);
                if (defaultIcon != null)
                {
                    GUI.DrawTexture(thumbnailRect, defaultIcon, ScaleMode.ScaleToFit);
                }
                else
                {
                    GUI.Box(thumbnailRect, "No Image");
                }

                // Draw indicators
                DrawAssetIndicators(asset, thumbnailRect);

                // Asset name
                DrawAssetName(asset);

                // Handle click events
                HandleAssetItemEvents(asset, thumbnailRect, onLeftClick, onRightClick);
            }
        }

        /// <summary>
        /// Get default icon based on asset type
        /// </summary>
        private Texture2D GetDefaultIcon(MockAssetInfo asset)
        {
            if (asset.isGroup)
            {
                return EditorGUIUtility.IconContent("Folder Icon").image as Texture2D;
            }

            switch (asset.assetType)
            {
                case "Avatar":
                case "Prefab":
                    return EditorGUIUtility.IconContent("Prefab Icon").image as Texture2D;
                case "Material":
                    return EditorGUIUtility.IconContent("Material Icon").image as Texture2D;
                case "Texture":
                    return EditorGUIUtility.IconContent("Texture Icon").image as Texture2D;
                case "Animation":
                    return EditorGUIUtility.IconContent("AnimationClip Icon").image as Texture2D;
                case "Shader":
                    return EditorGUIUtility.IconContent("Shader Icon").image as Texture2D;
                default:
                    return EditorGUIUtility.IconContent("DefaultAsset Icon").image as Texture2D;
            }
        }

        /// <summary>
        /// Draw asset indicators (favorite, hidden, group)
        /// </summary>
        private void DrawAssetIndicators(MockAssetInfo asset, Rect thumbnailRect)
        {
            // Group indicator
            if (asset.isGroup)
            {
                DrawGroupIndicator(thumbnailRect);
            }

            // Favorite indicator
            if (asset.isFavorite)
            {
                DrawFavoriteIndicator(thumbnailRect);
            }

            // Archived/Hidden indicator
            if (asset.isHidden)
            {
                DrawArchivedIndicator(thumbnailRect);
            }
        }

        /// <summary>
        /// Draw favorite star indicator
        /// </summary>
        private void DrawFavoriteIndicator(Rect thumbnailRect)
        {
            var starSize = 25f;
            var starRect = new Rect(thumbnailRect.x + thumbnailRect.width - starSize - 3, thumbnailRect.y + 3, starSize, starSize);

            var originalColor = GUI.color;
            var starStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(starSize * 0.8f),
                alignment = TextAnchor.MiddleCenter
            };

            // Draw black outline
            GUI.color = Color.black;
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) continue;
                    var outlineRect = new Rect(starRect.x + x, starRect.y + y, starRect.width, starRect.height);
                    GUI.Label(outlineRect, "★", starStyle);
                }
            }

            // Draw main star
            GUI.color = Color.yellow;
            GUI.Label(starRect, "★", starStyle);

            GUI.color = originalColor;
        }

        /// <summary>
        /// Draw archived/hidden indicator
        /// </summary>
        private void DrawArchivedIndicator(Rect thumbnailRect)
        {
            var hiddenRect = new Rect(thumbnailRect.x + 5, thumbnailRect.y + 5, 15, 15);
            var oldColor = GUI.color;
            GUI.color = Color.red;

            // Draw a simple "H" for hidden instead of emoji for compatibility
            var hiddenStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.red }
            };
            
            EditorGUI.DrawRect(hiddenRect, new Color(1f, 1f, 1f, 0.8f));
            GUI.Label(hiddenRect, "H", hiddenStyle);
            GUI.color = oldColor;
        }

        /// <summary>
        /// Draw group indicator
        /// </summary>
        private void DrawGroupIndicator(Rect thumbnailRect)
        {
            var indicatorRect = new Rect(thumbnailRect.x + 2, thumbnailRect.y + 2, 16, 16);
            EditorGUI.DrawRect(indicatorRect, new Color(0.2f, 0.6f, 1f, 0.8f));

            var labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            };

            GUI.Label(indicatorRect, "G", labelStyle);
        }

        /// <summary>
        /// Draw asset name with word wrapping and truncation
        /// </summary>
        private void DrawAssetName(MockAssetInfo asset)
        {
            var nameStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                alignment = TextAnchor.UpperCenter,
                fontSize = 10,
                richText = true
            };
            var availableWidth = _thumbnailSize + 10;

            // Fixed height for 2 lines
            var fixedHeight = nameStyle.lineHeight * 2 + 5;
            var rect = GUILayoutUtility.GetRect(availableWidth, fixedHeight);

            // Truncate text to fit height
            var displayText = TruncateTextToFitHeight(asset.name, nameStyle, availableWidth, fixedHeight);
            var content = new GUIContent(displayText);

            // Highlight if text was truncated
            if (displayText != asset.name)
            {
                EditorGUI.DrawRect(rect, new Color(0.2f, 0.3f, 0.4f, 0.15f));
            }

            GUI.Label(rect, content, nameStyle);
        }

        /// <summary>
        /// Truncate text to fit within specified height
        /// </summary>
        private string TruncateTextToFitHeight(string text, GUIStyle style, float width, float maxHeight)
        {
            var testContent = new GUIContent(text);
            var textHeight = style.CalcHeight(testContent, width);

            if (textHeight <= maxHeight)
                return text;

            // Truncate by words
            var words = text.Split(' ');
            var result = "";

            for (int i = 0; i < words.Length; i++)
            {
                var testText = string.IsNullOrEmpty(result) ? words[i] : result + " " + words[i];
                var testContent2 = new GUIContent(testText);
                var testHeight = style.CalcHeight(testContent2, width);

                if (testHeight > maxHeight)
                {
                    break;
                }
                result = testText;
            }

            // If result is empty (first word too long), truncate by characters
            if (string.IsNullOrEmpty(result) && text.Length > 0)
            {
                for (int i = 1; i <= text.Length; i++)
                {
                    var testText = text.Substring(0, i);
                    var testContent3 = new GUIContent(testText);
                    var testHeight = style.CalcHeight(testContent3, width);

                    if (testHeight > maxHeight)
                    {
                        result = i > 1 ? text.Substring(0, i - 1) : text.Substring(0, 1);
                        break;
                    }
                    result = testText;
                }
            }

            return result;
        }

        /// <summary>
        /// Handle mouse events for asset items
        /// </summary>
        private void HandleAssetItemEvents(MockAssetInfo asset, Rect thumbnailRect, System.Action<MockAssetInfo> onLeftClick, System.Action<MockAssetInfo> onRightClick)
        {
            if (Event.current.type == EventType.MouseDown && thumbnailRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.button == 0) // Left click
                {
                    onLeftClick?.Invoke(asset);
                    Event.current.Use();
                    GUI.changed = true;
                }
                else if (Event.current.button == 1) // Right click
                {
                    onRightClick?.Invoke(asset);
                    Event.current.Use();
                }
            }
        }

        // Public setters
        public void SetThumbnailSize(float size) => _thumbnailSize = size;
    }
}