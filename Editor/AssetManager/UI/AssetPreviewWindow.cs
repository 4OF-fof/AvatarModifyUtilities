using System;
using UnityEngine;
using UnityEditor;
using AMU.AssetManager.Data;
using AMU.AssetManager.Helper;
using AMU.Data.Lang;

namespace AMU.AssetManager.UI
{
    public class AssetPreviewWindow : EditorWindow
    {
        public static void ShowWindow(AssetInfo asset)
        {
            var window = GetWindow<AssetPreviewWindow>(LocalizationManager.GetText("AssetPreview_windowTitle"));
            window.minSize = new Vector2(400, 400);
            window._asset = asset;
            window.Show();
        }

        private AssetInfo _asset;
        private AssetThumbnailManager _thumbnailManager;
        private AssetFileManager _fileManager;
        private AssetDataManager _dataManager;

        // Preview state
        private PreviewRenderUtility _previewUtility;
        private GameObject _previewObject;
        private Texture2D _previewTexture;
        private bool _isGeneratingPreview = false;

        // Preview controls
        private Vector2 _previewDirection = new Vector2(120f, -20f);
        private float _previewDistance = 5f;
        private bool _wireframe = false;
        private bool _lighting = true;
        private Color _backgroundColor = Color.gray;

        private void OnEnable()
        {
            var language = EditorPrefs.GetString("Setting.Core_language", "ja_jp");
            LocalizationManager.LoadLanguage(language);

            InitializeManagers();
            InitializePreview();
        }

        private void OnDisable()
        {
            CleanupPreview();
            _thumbnailManager?.ClearCache();
        }

        private void InitializeManagers()
        {
            if (_thumbnailManager == null)
            {
                _thumbnailManager = new AssetThumbnailManager();
                _thumbnailManager.OnThumbnailSaved += OnThumbnailSaved;
            }

            if (_fileManager == null)
            {
                _fileManager = new AssetFileManager();
            }

            if (_dataManager == null)
            {
                _dataManager = new AssetDataManager();
                _dataManager.LoadData();
            }
        }

        private void InitializePreview()
        {
            if (_previewUtility == null)
            {
                _previewUtility = new PreviewRenderUtility();
                _previewUtility.camera.transform.position = new Vector3(0, 0, -5);
                _previewUtility.camera.transform.rotation = Quaternion.identity;
            }

            LoadPreviewObject();
        }

        private void CleanupPreview()
        {
            if (_previewUtility != null)
            {
                _previewUtility.Cleanup();
                _previewUtility = null;
            }

            if (_previewObject != null)
            {
                DestroyImmediate(_previewObject);
                _previewObject = null;
            }

            if (_previewTexture != null)
            {
                DestroyImmediate(_previewTexture);
                _previewTexture = null;
            }
        }

        private void OnGUI()
        {
            if (_asset == null)
            {
                GUILayout.Label("No asset selected", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            DrawHeader();
            DrawPreviewArea();
            DrawControls();
        }

        private void DrawHeader()
        {
            using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(_asset.name, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(LocalizationManager.GetText("AssetPreview_exportImage"), EditorStyles.toolbarButton))
                {
                    ExportPreviewImage();
                }

                if (GUILayout.Button(LocalizationManager.GetText("AssetPreview_resetView"), EditorStyles.toolbarButton))
                {
                    ResetView();
                }
            }
        }

        private void DrawPreviewArea()
        {
            var previewRect = GUILayoutUtility.GetRect(position.width, position.height - 120);
            
            if (_isGeneratingPreview)
            {
                GUI.Label(previewRect, LocalizationManager.GetText("AssetPreview_generating"), EditorStyles.centeredGreyMiniLabel);
                return;
            }

            if (_previewObject == null)
            {
                GUI.Label(previewRect, LocalizationManager.GetText("AssetPreview_noPreview"), EditorStyles.centeredGreyMiniLabel);
                return;
            }

            // Handle mouse input for camera control
            HandlePreviewInput(previewRect);

            // Render preview
            RenderPreview(previewRect);
        }

        private void DrawControls()
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Preview Controls", EditorStyles.boldLabel);

                using (new GUILayout.HorizontalScope())
                {
                    // Wireframe toggle
                    bool newWireframe = GUILayout.Toggle(_wireframe, LocalizationManager.GetText("AssetPreview_wireframe"));
                    if (newWireframe != _wireframe)
                    {
                        _wireframe = newWireframe;
                        UpdateRenderSettings();
                    }

                    // Lighting toggle
                    bool newLighting = GUILayout.Toggle(_lighting, LocalizationManager.GetText("AssetPreview_lighting"));
                    if (newLighting != _lighting)
                    {
                        _lighting = newLighting;
                        UpdateRenderSettings();
                    }
                }

                // Background color
                GUILayout.BeginHorizontal();
                GUILayout.Label(LocalizationManager.GetText("AssetPreview_background"), GUILayout.Width(80));
                var newBackgroundColor = EditorGUILayout.ColorField(_backgroundColor);
                if (newBackgroundColor != _backgroundColor)
                {
                    _backgroundColor = newBackgroundColor;
                    UpdateRenderSettings();
                }
                GUILayout.EndHorizontal();

                // Distance slider
                GUILayout.BeginHorizontal();
                GUILayout.Label("Distance", GUILayout.Width(80));
                _previewDistance = GUILayout.HorizontalSlider(_previewDistance, 1f, 20f);
                GUILayout.EndHorizontal();
            }
        }

        private void HandlePreviewInput(Rect previewRect)
        {
            Event current = Event.current;

            if (previewRect.Contains(current.mousePosition))
            {
                if (current.type == EventType.MouseDrag)
                {
                    if (current.button == 0) // Left mouse drag
                    {
                        _previewDirection.x += current.delta.x;
                        _previewDirection.y -= current.delta.y;
                        current.Use();
                        Repaint();
                    }
                }
                else if (current.type == EventType.ScrollWheel)
                {
                    _previewDistance += current.delta.y * 0.1f;
                    _previewDistance = Mathf.Clamp(_previewDistance, 1f, 20f);
                    current.Use();
                    Repaint();
                }
            }
        }

        private void RenderPreview(Rect previewRect)
        {
            if (_previewUtility == null || _previewObject == null)
                return;

            // Update camera position
            _previewUtility.camera.transform.position = Quaternion.Euler(_previewDirection.y, _previewDirection.x, 0) * Vector3.back * _previewDistance;
            _previewUtility.camera.transform.LookAt(Vector3.zero);

            // Render
            _previewUtility.BeginPreview(previewRect, GUIStyle.none);
            
            if (_lighting)
            {
                _previewUtility.lights[0].intensity = 1.4f;
                _previewUtility.lights[0].transform.rotation = Quaternion.Euler(40f, 40f, 0);
                _previewUtility.lights[1].intensity = 1.4f;
            }

            _previewUtility.camera.backgroundColor = _backgroundColor;
            _previewUtility.camera.clearFlags = CameraClearFlags.Color;

            if (_wireframe)
            {
                GL.wireframe = true;
            }

            _previewUtility.camera.Render();

            if (_wireframe)
            {
                GL.wireframe = false;
            }

            var texture = _previewUtility.EndPreview();
            GUI.DrawTexture(previewRect, texture, ScaleMode.StretchToFill, false);
        }

        private void LoadPreviewObject()
        {
            if (_asset == null || string.IsNullOrEmpty(_asset.filePath))
                return;

            _isGeneratingPreview = true;
            Repaint();

            try
            {
                switch (_asset.assetType)
                {
                    case AssetType.Avatar:
                    case AssetType.Prefab:
                        LoadPrefabPreview();
                        break;
                    case AssetType.Material:
                        LoadMaterialPreview();
                        break;
                    case AssetType.Texture:
                        LoadTexturePreview();
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetPreviewWindow] Failed to load preview for {_asset.name}: {ex.Message}");
            }
            finally
            {
                _isGeneratingPreview = false;
                Repaint();
            }
        }

        private void LoadPrefabPreview()
        {
            if (!_asset.filePath.StartsWith("Assets/"))
                return;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(_asset.filePath);
            if (prefab != null)
            {
                _previewObject = Instantiate(prefab);
                _previewObject.hideFlags = HideFlags.HideAndDontSave;

                // Center the object
                var bounds = GetObjectBounds(_previewObject);
                _previewObject.transform.position = -bounds.center;

                // Adjust camera distance based on object size
                _previewDistance = bounds.size.magnitude * 1.5f;
            }
        }

        private void LoadMaterialPreview()
        {
            if (!_asset.filePath.StartsWith("Assets/"))
                return;

            var material = AssetDatabase.LoadAssetAtPath<Material>(_asset.filePath);
            if (material != null)
            {
                // Create a sphere to show the material
                _previewObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _previewObject.hideFlags = HideFlags.HideAndDontSave;
                _previewObject.GetComponent<Renderer>().material = material;
                _previewDistance = 3f;
            }
        }

        private void LoadTexturePreview()
        {
            if (!_asset.filePath.StartsWith("Assets/"))
                return;

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(_asset.filePath);
            if (texture != null)
            {
                // Create a quad to show the texture
                _previewObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                _previewObject.hideFlags = HideFlags.HideAndDontSave;
                
                var material = new Material(Shader.Find("Unlit/Texture"));
                material.mainTexture = texture;
                _previewObject.GetComponent<Renderer>().material = material;
                
                // Adjust aspect ratio
                float aspect = (float)texture.width / texture.height;
                _previewObject.transform.localScale = new Vector3(aspect, 1, 1);
                _previewDistance = 2f;
            }
        }

        private Bounds GetObjectBounds(GameObject obj)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(obj.transform.position, Vector3.one);

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return bounds;
        }

        private void UpdateRenderSettings()
        {
            Repaint();
        }

        private void ResetView()
        {
            _previewDirection = new Vector2(120f, -20f);
            _previewDistance = 5f;
            _wireframe = false;
            _lighting = true;
            _backgroundColor = Color.gray;
            
            if (_previewObject != null)
            {
                var bounds = GetObjectBounds(_previewObject);
                _previewDistance = bounds.size.magnitude * 1.5f;
            }
            
            Repaint();
        }

        private void ExportPreviewImage()
        {
            if (_previewObject == null)
                return;

            string defaultName = $"{_asset.name}_preview.png";
            string path = EditorUtility.SaveFilePanel("Export Preview Image", "", defaultName, "png");
            
            if (!string.IsNullOrEmpty(path))
            {
                var previewRect = new Rect(0, 0, 512, 512);
                
                _previewUtility.BeginPreview(previewRect, GUIStyle.none);
                _previewUtility.camera.Render();
                var texture = _previewUtility.EndPreview();

                // Convert to PNG and save
                var renderTexture = texture as RenderTexture;
                if (renderTexture != null)
                {
                    RenderTexture.active = renderTexture;
                    var exportTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
                    exportTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                    exportTexture.Apply();
                    RenderTexture.active = null;

                    byte[] pngData = exportTexture.EncodeToPNG();
                    System.IO.File.WriteAllBytes(path, pngData);
                    DestroyImmediate(exportTexture);

                    Debug.Log($"[AssetPreviewWindow] Preview image exported to: {path}");
                }
            }
        }

        private void OnThumbnailSaved(AssetInfo asset)
        {
            if (asset != null && _dataManager != null)
            {
                _dataManager.UpdateAsset(asset);
            }
        }
    }
}
