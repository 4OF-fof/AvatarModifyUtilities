using System;
using UnityEditor;
using UnityEngine;
using AMU.Editor.VrcAssetManager.Schema;
using AMU.Editor.VrcAssetManager.UI.Components;

namespace AMU.Editor.VrcAssetManager.UI.Components
{
    public class AssetItemComponent
    {
        public void Draw(AssetSchema asset, bool isSelected, bool isMultiSelected, Action<AssetSchema> onLeftClick, Action<AssetSchema> onRightClick, Action<AssetSchema> onDoubleClick)
        {

            using (new GUILayout.VerticalScope(GUILayout.Width(125)))
            {
                var thumbnailRect = GUILayoutUtility.GetRect(115, 115);

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

                DrawThumbnailComponent.Draw(thumbnailRect, asset);

                DrawIndicator(thumbnailRect, 115, asset);

                var nameStyle = new GUIStyle(EditorStyles.label)
                {
                    wordWrap = true,
                    alignment = TextAnchor.UpperCenter,
                    fontSize = 12,
                    richText = true
                };

                var rect = GUILayoutUtility.GetRect(125, 30);

                var content = new GUIContent(asset.metadata.name);

                GUI.Label(rect, content, nameStyle);

                HandleAssetItemEvents(asset, thumbnailRect, onLeftClick, onRightClick, onDoubleClick);
            }
        }

        private void DrawIndicator(Rect thumbnailRect, float thumbnailSize, AssetSchema asset)
        {
            if (asset.hasChildAssets)
            {
                var iconSize = 20;
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

            if (asset.state.isFavorite)
            {
                var starSize = 25;
                var iconSize = 20;

                var yOffset = asset.hasChildAssets ? 2 + iconSize + 2 : 2;
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

        private void HandleAssetItemEvents(AssetSchema asset, Rect thumbnailRect, Action<AssetSchema> onLeftClick, Action<AssetSchema> onRightClick, Action<AssetSchema> onDoubleClick)
        {
            if (Event.current.type == EventType.MouseDown && thumbnailRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.button == 0)
                {
                    if (Event.current.clickCount == 2)
                    {
                        onDoubleClick?.Invoke(asset);
                        Event.current.Use();
                    }
                    else
                    {
                        onLeftClick?.Invoke(asset);
                        Event.current.Use();
                        GUI.changed = true;
                    }
                }
                else if (Event.current.button == 1)
                {
                    onRightClick?.Invoke(asset);
                    Event.current.Use();
                }
            }
        }
    }
}