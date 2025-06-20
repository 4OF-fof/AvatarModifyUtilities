using System;
using System.IO;

using UnityEngine;
using UnityEditor;
using AMU.Editor.Core.Controller;

namespace AMU.Editor.Core.Api
{
    public static class ObjectCaptureAPI
    {
        public static Texture2D CaptureObject(GameObject targetObject, string savePath, int width = 512, int height = 512)
        {
            if (targetObject == null)
            {
                Debug.LogError(LocalizationController.GetText("message_error_target_null"));
                return null;
            }

            if (string.IsNullOrEmpty(savePath))
            {
                Debug.LogError(LocalizationController.GetText("message_error_save_path_required"));
                return null;
            }

            GameObject tempCameraObject = new GameObject("TempCaptureCamera");
            Camera captureCamera = tempCameraObject.AddComponent<Camera>();

            try
            {
                Bounds bounds = GetObjectBounds(targetObject);
                if (bounds.size == Vector3.zero)
                {
                    Debug.LogWarning(LocalizationController.GetText("message_warning_no_renderable_bounds"));
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

                try
                {
                    SaveTexture(capturedTexture, savePath);
                }
                catch (Exception e)
                {
                    Debug.LogError(string.Format(LocalizationController.GetText("message_error_save_image_failed"), e.Message));
                    UnityEngine.Object.DestroyImmediate(capturedTexture);
                    return null;
                }

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

                Debug.Log(string.Format(LocalizationController.GetText("message_success_image_saved"), path));
            }
            catch (Exception e)
            {
                throw new Exception(LocalizationController.GetText("message_error_save_image_failed"), e);
            }
        }
    }
}
