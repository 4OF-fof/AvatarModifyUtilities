using System;
using UnityEditor;
using UnityEngine;
using AMU.Editor.VrcAssetManager.Schema;

namespace AMU.Editor.VrcAssetManager.UI.Components
{
    public class AssetItemComponent
    {
        private float _thumbnailSize;

        public void Draw(AssetSchema asset, bool isSelected, bool isMultiSelected, System.Action<AssetSchema> onLeftClick, System.Action<AssetSchema> onRightClick)
        {
            using (new GUILayout.VerticalScope(GUILayout.Width(_thumbnailSize + 10)))
            {
                var thumbnailRect = GUILayoutUtility.GetRect(_thumbnailSize, _thumbnailSize);

                if (isSelected && isMultiSelected)
                {
                    EditorGUI.DrawRect(thumbnailRect, new Color(0.3f, 0.5f, 1f, 0.5f));
                }
                else if (isMultiSelected)
                {
                    EditorGUI.DrawRect(thumbnailRect, new Color(0.3f, 0.5f, 1f, 0.3f));
                }
                else if (isSelected)
                {
                    EditorGUI.DrawRect(thumbnailRect, new Color(0.3f, 0.5f, 1f, 0.3f));
                }
                
                var prefabIcon = EditorGUIUtility.IconContent("Prefab Icon").image as Texture2D;

                if (prefabIcon != null)
                {
                    GUI.DrawTexture(thumbnailRect, prefabIcon, ScaleMode.ScaleToFit);
                }

                DrawIndicator(thumbnailRect, asset);

                var baseFontSize = 10f;
                var baseThumbnailSize = 110f;
                var scaledFontSize = Mathf.RoundToInt(baseFontSize * (_thumbnailSize / baseThumbnailSize));
                scaledFontSize = Mathf.Clamp(scaledFontSize, 8, 16);
                
                var nameStyle = new GUIStyle(EditorStyles.label)
                {
                    wordWrap = true,
                    alignment = TextAnchor.UpperCenter,
                    fontSize = scaledFontSize,
                    richText = true
                };
                var availableWidth = _thumbnailSize + 10;

                var fixedHeight = nameStyle.lineHeight * 2 + 5;
                var rect = GUILayoutUtility.GetRect(availableWidth, fixedHeight);

                var displayText = TruncateTextToFitHeight(asset.Metadata.Name, nameStyle, availableWidth, fixedHeight);
                var content = new GUIContent(displayText);

                if (displayText != asset.Metadata.Name)
                {
                    EditorGUI.DrawRect(rect, new Color(0.2f, 0.3f, 0.4f, 0.15f));
                }

                GUI.Label(rect, content, nameStyle);

                HandleAssetItemEvents(asset, thumbnailRect, onLeftClick, onRightClick);
            }
        }

        private void DrawIndicator(Rect thumbnailRect, AssetSchema asset)
        {
            if (asset.HasChildAssets)
            {
                var iconSize = 20f * (_thumbnailSize / 110f);
                var indicatorRect = new Rect(thumbnailRect.x + 4, thumbnailRect.y + 4, iconSize, iconSize);

                var folderIcon = EditorGUIUtility.IconContent("Folder Icon").image as Texture2D;
                if (folderIcon != null)
                {
                    var originalColor = GUI.color;
                    
                    GUI.color = Color.black;
                    for (int x = -1; x <= 1; x++)
                    {
                        for (int y = -1; y <= 1; y++)
                        {
                            if (x == 0 && y == 0) continue;
                            var outlineRect = new Rect(indicatorRect.x + x, indicatorRect.y + y, indicatorRect.width, indicatorRect.height);
                            GUI.DrawTexture(outlineRect, folderIcon, ScaleMode.ScaleToFit);
                        }
                    }

                    GUI.color = originalColor;
                    GUI.DrawTexture(indicatorRect, folderIcon, ScaleMode.ScaleToFit);
                }
            }

            if (asset.State.IsFavorite)
            {
                var starSize = 25f * (_thumbnailSize / 110f);
                var iconSize = 20f * (_thumbnailSize / 110f);

                var yOffset = asset.HasChildAssets ? 2 + iconSize + 2 : 2;
                var starRect = new Rect(thumbnailRect.x + 2, thumbnailRect.y + yOffset, starSize, starSize);

                var originalColor = GUI.color;
                var starStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.RoundToInt(starSize * 0.8f),
                    alignment = TextAnchor.MiddleCenter
                };

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

                GUI.color = Color.yellow;
                GUI.Label(starRect, "★", starStyle);

                GUI.color = originalColor;
            }
        }

        private string TruncateTextToFitHeight(string text, GUIStyle style, float width, float maxHeight)
        {
            var testContent = new GUIContent(text);
            var textHeight = style.CalcHeight(testContent, width);

            if (textHeight <= maxHeight)
                return text;

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

        private void HandleAssetItemEvents(AssetSchema asset, Rect thumbnailRect, System.Action<AssetSchema> onLeftClick, System.Action<AssetSchema> onRightClick)
        {
            if (Event.current.type == EventType.MouseDown && thumbnailRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.button == 0)
                {
                    onLeftClick?.Invoke(asset);
                    Event.current.Use();
                    GUI.changed = true;
                }
                else if (Event.current.button == 1)
                {
                    onRightClick?.Invoke(asset);
                    Event.current.Use();
                }
            }
        }

        public void SetThumbnailSize(float size) => _thumbnailSize = size;
    }
}