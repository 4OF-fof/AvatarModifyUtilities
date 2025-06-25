using System;
using UnityEditor;
using UnityEngine;
using AMU.Editor.Core.Api;

namespace AMU.Editor.VrcAssetManager.UI.Components
{
    public class SpecialAssetItemComponent
    {
        public void DrawBackButton(Action onBackClick)
        {
            using (new GUILayout.VerticalScope(GUILayout.Width(125)))
            {
                var thumbnailRect = GUILayoutUtility.GetRect(115, 115);

                var iconSize = 96;
                var iconRect = new Rect(
                    thumbnailRect.x + (thumbnailRect.width - iconSize) / 2,
                    thumbnailRect.y + (thumbnailRect.height - iconSize) / 2,
                    iconSize, iconSize
                );

                var backIcon = EditorGUIUtility.IconContent("d_back@2x");
                if (backIcon != null)
                {   
                    var iconStyle = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 24,
                        fixedWidth = iconSize,
                        fixedHeight = iconSize,
                        imagePosition = ImagePosition.ImageOnly
                    };
                    GUI.Label(iconRect, backIcon, iconStyle);
                }

                var nameStyle = new GUIStyle(EditorStyles.label)
                {
                    wordWrap = true,
                    alignment = TextAnchor.UpperCenter,
                    fontSize = 12,
                    richText = true
                };

                var rect = GUILayoutUtility.GetRect(125, 30);

                var content = new GUIContent(LocalizationAPI.GetText("AssetManager_backToParent"));
                GUI.Label(rect, content, nameStyle);

                HandleSpecialItemEvents(thumbnailRect, onBackClick);
            }
        }

        public void DrawAddButton(Action onAddClick)
        {
            using (new GUILayout.VerticalScope(GUILayout.Width(125)))
            {
                var thumbnailRect = GUILayoutUtility.GetRect(115, 115);

                var iconSize = 96;
                var iconRect = new Rect(
                    thumbnailRect.x + (thumbnailRect.width - iconSize) / 2,
                    thumbnailRect.y + (thumbnailRect.height - iconSize) / 2,
                    iconSize, iconSize
                );

                var folderIcon = EditorGUIUtility.IconContent("FolderEmpty Icon");
                if (folderIcon != null)
                {
                    var folderStyle = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fixedWidth = iconSize,
                        fixedHeight = iconSize,
                        imagePosition = ImagePosition.ImageOnly
                    };
                    GUI.Label(iconRect, folderIcon, folderStyle);
                }

                var addIcon = EditorGUIUtility.IconContent("Toolbar Plus");
                if (addIcon != null)
                {
                    var smallerSize = iconSize / 2;
                    var smallIconRect = new Rect(
                        thumbnailRect.x + (thumbnailRect.width - smallerSize) / 2,
                        thumbnailRect.y + (thumbnailRect.height - smallerSize) / 2 + 5,
                        smallerSize, smallerSize
                    );
                    
                    var iconStyle = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 24,
                        fixedWidth = smallerSize,
                        fixedHeight = smallerSize,
                        imagePosition = ImagePosition.ImageOnly
                    };
                    GUI.Label(smallIconRect, addIcon, iconStyle);
                }

                var nameStyle = new GUIStyle(EditorStyles.label)
                {
                    wordWrap = true,
                    alignment = TextAnchor.UpperCenter,
                    fontSize = 12,
                    richText = true
                };

                var rect = GUILayoutUtility.GetRect(125, 30);

                var content = new GUIContent(LocalizationAPI.GetText("AssetManager_addToGroup"));
                GUI.Label(rect, content, nameStyle);

                HandleSpecialItemEvents(thumbnailRect, onAddClick);
            }
        }

        private void HandleSpecialItemEvents(Rect thumbnailRect, Action onClick)
        {
            if (Event.current.type == EventType.MouseDown && thumbnailRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.button == 0 && Event.current.clickCount == 2)
                {
                    onClick?.Invoke();
                    Event.current.Use();
                    GUI.changed = true;
                }
            }
        }
    }
}
