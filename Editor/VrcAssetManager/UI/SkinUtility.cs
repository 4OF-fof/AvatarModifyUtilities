using UnityEngine;
using UnityEditor;

namespace AMU.Editor.VrcAssetManager.UI
{
    public static class SkinUtility
    {
        private static bool _applied = false;
        private static Texture2D _transparentTex;

        public static void ApplySkin()
        {
            if (_applied) return;
            _applied = true;
            if (_transparentTex == null)
                _transparentTex = MakeTex(2, 2, new Color(0, 0, 0, 0));

            var thinScrollbar = new GUIStyle(GUI.skin.verticalScrollbar)
            {
                fixedWidth = 6,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0)
            };
            var thinHScrollbar = new GUIStyle(GUI.skin.horizontalScrollbar)
            {
                fixedHeight = 6,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0)
            };
            var thinScrollbarThumb = new GUIStyle(GUI.skin.verticalScrollbarThumb)
            {
                fixedWidth = 6,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0)
            };
            var thinHScrollbarThumb = new GUIStyle(GUI.skin.horizontalScrollbarThumb)
            {
                fixedHeight = 6,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0)
            };

            var thinScrollbarUp = new GUIStyle(GUI.skin.verticalScrollbarUpButton)
            {
                fixedWidth = 0,
                fixedHeight = 0,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                normal = { background = _transparentTex },
                active = { background = _transparentTex },
                hover = { background = _transparentTex },
                focused = { background = _transparentTex },
                onNormal = { background = _transparentTex },
                onActive = { background = _transparentTex },
                onHover = { background = _transparentTex },
                onFocused = { background = _transparentTex }
            };
            var thinScrollbarDown = new GUIStyle(GUI.skin.verticalScrollbarDownButton)
            {
                fixedWidth = 0,
                fixedHeight = 0,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                normal = { background = _transparentTex },
                active = { background = _transparentTex },
                hover = { background = _transparentTex },
                focused = { background = _transparentTex },
                onNormal = { background = _transparentTex },
                onActive = { background = _transparentTex },
                onHover = { background = _transparentTex },
                onFocused = { background = _transparentTex }
            };
            var thinScrollbarLeft = new GUIStyle(GUI.skin.horizontalScrollbarLeftButton)
            {
                fixedWidth = 0,
                fixedHeight = 0,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                normal = { background = _transparentTex },
                active = { background = _transparentTex },
                hover = { background = _transparentTex },
                focused = { background = _transparentTex },
                onNormal = { background = _transparentTex },
                onActive = { background = _transparentTex },
                onHover = { background = _transparentTex },
                onFocused = { background = _transparentTex }
            };
            var thinScrollbarRight = new GUIStyle(GUI.skin.horizontalScrollbarRightButton)
            {
                fixedWidth = 0,
                fixedHeight = 0,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                normal = { background = _transparentTex },
                active = { background = _transparentTex },
                hover = { background = _transparentTex },
                focused = { background = _transparentTex },
                onNormal = { background = _transparentTex },
                onActive = { background = _transparentTex },
                onHover = { background = _transparentTex },
                onFocused = { background = _transparentTex }
            };

            GUI.skin.verticalScrollbar = thinScrollbar;
            GUI.skin.horizontalScrollbar = thinHScrollbar;
            GUI.skin.verticalScrollbarThumb = thinScrollbarThumb;
            GUI.skin.horizontalScrollbarThumb = thinHScrollbarThumb;
            GUI.skin.verticalScrollbarUpButton = thinScrollbarUp;
            GUI.skin.verticalScrollbarDownButton = thinScrollbarDown;
            GUI.skin.horizontalScrollbarLeftButton = thinScrollbarLeft;
            GUI.skin.horizontalScrollbarRightButton = thinScrollbarRight;
        }

        private static Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}
