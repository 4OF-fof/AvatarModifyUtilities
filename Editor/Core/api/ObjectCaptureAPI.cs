using UnityEngine;
using UnityEditor;
using System.IO;

namespace AMU.Editor.Core.API
{
    /// <summary>
    /// オブジェクトキャプチャ機能を提供するAPI
    /// </summary>
    public static class ObjectCaptureAPI
    {
        /// <summary>
        /// 指定されたオブジェクトをキャプチャしてテクスチャとして保存します
        /// </summary>
        /// <param name="targetObject">キャプチャ対象のオブジェクト</param>
        /// <param name="savePath">保存先パス</param>
        /// <param name="width">キャプチャ幅</param>
        /// <param name="height">キャプチャ高さ</param>
        /// <returns>キャプチャされたテクスチャ</returns>
        public static Texture2D CaptureObject(GameObject targetObject, string savePath, int width = 512, int height = 512)
        {
            if (targetObject == null)
            {
                Debug.LogError("Target object is null");
                return null;
            }

            if (string.IsNullOrEmpty(savePath))
            {
                Debug.LogError("Save path is required");
                return null;
            }

            GameObject tempCameraObject = new GameObject("TempCaptureCamera");
            Camera captureCamera = tempCameraObject.AddComponent<Camera>();

            try
            {
                Bounds bounds = GetObjectBounds(targetObject);
                if (bounds.size == Vector3.zero)
                {
                    Debug.LogWarning("Object has no renderable bounds");
                    bounds = new Bounds(targetObject.transform.position, Vector3.one);
                }

                Vector3 cameraPosition = bounds.center + Vector3.forward * (bounds.size.magnitude * 1.0f);
                captureCamera.transform.position = cameraPosition;
                captureCamera.transform.LookAt(bounds.center);

                captureCamera.clearFlags = CameraClearFlags.SolidColor;
                captureCamera.backgroundColor = Color.clear;
                captureCamera.orthographic = true;
                captureCamera.orthographicSize = Mathf.Max(bounds.size.x, bounds.size.y) * 0.45f;
                captureCamera.nearClipPlane = 0.1f;
                captureCamera.farClipPlane = bounds.size.magnitude * 3f;

                RenderTexture renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                captureCamera.targetTexture = renderTexture;

                captureCamera.Render();

                RenderTexture.active = renderTexture;
                Texture2D capturedTexture = new Texture2D(width, height, TextureFormat.ARGB32, false);
                capturedTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                capturedTexture.Apply();

                SaveTexture(capturedTexture, savePath);

                RenderTexture.active = null;
                captureCamera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(renderTexture);

                return capturedTexture;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tempCameraObject);
            }
        }

        private static Bounds GetObjectBounds(GameObject obj)
        {
            Bounds bounds = new Bounds();
            bool hasBounds = false;

            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return bounds;
        }

        private static void SaveTexture(Texture2D texture, string path)
        {
            try
            {
                byte[] bytes = texture.EncodeToPNG();

                string directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllBytes(path, bytes);

                if (path.StartsWith(Application.dataPath))
                {
                    string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                    AssetDatabase.ImportAsset(relativePath);
                }

                Debug.Log($"Image saved to: {path}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save image: {e.Message}");
            }
        }
    }
}
