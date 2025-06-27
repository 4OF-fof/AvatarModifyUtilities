using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AMU.Editor.Core.Controller;
using AMU.Editor.Core.Schema;

namespace AMU.Editor.Core.UI.Components
{
    public class MenuComponent
    {
        private const float MenuWidth = 240f;
        private const float MenuPadding = 8f;
        private const float MenuTopSpace = 12f;

        private string[] menuItems;
        private int selectedMenu = 0;
        private string menuSearch = "";
        private Dictionary<string, SettingItem[]> settingItems;
        private Vector2 windowPosition;

        public int SelectedMenu => selectedMenu;
        public string[] MenuItems => menuItems;

        public void Initialize(Dictionary<string, SettingItem[]> settingItems)
        {
            this.settingItems = settingItems;
            var keys = settingItems.Keys.ToList();
            if (keys.Contains("Core_general"))
            {
                keys.Remove("Core_general");
                menuItems = (new[] { "Core_general" }).Concat(keys).ToArray();
            }
            else
            {
                menuItems = keys.ToArray();
            }
            selectedMenu = 0;
        }

        public void Draw(Vector2 windowPosition)
        {
            this.windowPosition = windowPosition;
            DrawMenuBackground();
            using (new GUILayout.VerticalScope(GUILayout.Width(MenuWidth)))
            {
                GUILayout.Space(MenuTopSpace);
                GUILayout.Space(MenuPadding);
                DrawMenuSearchBar();
                GUILayout.Space(MenuPadding);
                var filteredMenuIndices = GetFilteredMenuIndices();
                UpdateSelectedMenu(filteredMenuIndices);
                DrawMenuItems(filteredMenuIndices);
            }
        }

        private void DrawMenuBackground()
        {
            var menuRect = new Rect(0, 0, MenuWidth, windowPosition.y);
            var menuBgColor = new Color(0.19f, 0.19f, 0.19f, 1f);
            var borderColor = new Color(0.22f, 0.22f, 0.22f, 1f);
            EditorGUI.DrawRect(menuRect, borderColor);
            EditorGUI.DrawRect(new Rect(0, 0, MenuWidth, windowPosition.y), menuBgColor);
        }

        private void DrawMenuSearchBar()
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(MenuPadding);
                menuSearch = GUILayout.TextField(menuSearch, GUI.skin.FindStyle("SearchTextField"), GUILayout.Width(MenuWidth - MenuPadding * 2));
                GUILayout.Space(MenuPadding);
            }
        }

        public List<int> GetFilteredMenuIndices()
        {
            if (string.IsNullOrEmpty(menuSearch))
                return Enumerable.Range(0, menuItems.Length).ToList();

            return menuItems
                .Select((menu, i) => new { menu, i })
                .Where(x => settingItems[x.menu].Any(item => LocalizationController.GetText(item.Name).Contains(menuSearch)))
                .Select(x => x.i)
                .ToList();
        }

        private void UpdateSelectedMenu(List<int> filteredMenuIndices)
        {
            if (!filteredMenuIndices.Contains(selectedMenu) && filteredMenuIndices.Count > 0)
                selectedMenu = filteredMenuIndices[0];
        }

        private void DrawMenuItems(List<int> filteredMenuIndices)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(MenuPadding);
                using (new GUILayout.VerticalScope())
                {
                    var buttonStyle = new GUIStyle(GUI.skin.button);
                    buttonStyle.fontSize = 16;

                    foreach (var i in filteredMenuIndices)
                    {
                        bool isSelected = selectedMenu == i;
                        bool pressed = GUILayout.Toggle(isSelected, LocalizationController.GetText(menuItems[i]), buttonStyle, GUILayout.ExpandWidth(true), GUILayout.Height(36));
                        if (pressed && !isSelected)
                        {
                            selectedMenu = i;
                            GUI.FocusControl(null);
                        }
                    }
                }
                GUILayout.Space(MenuPadding);
            }
        }

        public string GetMenuSearch()
        {
            return menuSearch;
        }
    }
}
