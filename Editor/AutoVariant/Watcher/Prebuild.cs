using UnityEngine;
using UnityEditor;
using VRC.SDKBase.Editor.BuildPipeline;
using Untitled.Editor.Core.Helper;
using System.IO;
using System.Collections.Generic;
using System;

public class MyPreBuildProcess : IVRCSDKBuildRequestedCallback
{
    // マテリアル状態保存用の構造体
    [System.Serializable]
    private class RendererMaterialState
    {
        public Renderer renderer;
        public Material[] originalMaterials;

        public RendererMaterialState(Renderer renderer)
        {
            this.renderer = renderer;
            this.originalMaterials = (Material[])renderer.sharedMaterials.Clone();
        }

        public void RestoreMaterials()
        {
            if (renderer != null)
            {
                renderer.sharedMaterials = originalMaterials;
            }
        }
    }

    private static List<RendererMaterialState> _materialStates = new List<RendererMaterialState>();

    public int callbackOrder => 0;
    public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
    {
        if (EditorPrefs.GetBool("Setting.AutoVariant_enablePrebuild", true))
        {
            OptimizeMaterials();
        }
        return true;
    }

    private void OptimizeMaterials()
    {
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        List<GameObject> optimizedAvatars = new List<GameObject>();

        // 既存の状態をクリア
        _materialStates.Clear();

        foreach (GameObject obj in allObjects)
        {
            if (PipelineManagerHelper.isVRCAvatar(obj))
            {
                // マテリアル状態を保存
                SaveMaterialStates(obj);
                
                MaterialVariantOptimizer.OptimizeMaterials(obj);
                optimizedAvatars.Add(obj);
                Debug.Log($"[MyPreBuildProcess] Optimized materials for VRC Avatar: {obj.name}");
            }
        }

        foreach (GameObject avatar in optimizedAvatars)
        {
            ExportOptimizedAvatar(avatar);
        }

        // Unity packageエクスポート後にマテリアルを元に戻す
        RestoreMaterialStates();
    }

    private void SaveMaterialStates(GameObject avatar)
    {
        Renderer[] renderers = avatar.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer.sharedMaterials != null && renderer.sharedMaterials.Length > 0)
            {
                _materialStates.Add(new RendererMaterialState(renderer));
            }
        }
        Debug.Log($"[MyPreBuildProcess] Saved material states for {renderers.Length} renderers in {avatar.name}");
    }

    private void RestoreMaterialStates()
    {
        int restoredCount = 0;
        foreach (var state in _materialStates)
        {
            if (state.renderer != null)
            {
                state.RestoreMaterials();
                restoredCount++;
            }
        }
        Debug.Log($"[MyPreBuildProcess] Restored materials for {restoredCount} renderers");
        _materialStates.Clear();
    }

    private void ExportOptimizedAvatar(GameObject avatar)
    {
        string blueprintId = PipelineManagerHelper.GetBlueprintId(avatar);
        
        string basePath = EditorPrefs.GetString("Setting.Core_dirPath", 
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Untitled"));
        
        if (!Directory.Exists(basePath))
        {
            Directory.CreateDirectory(basePath);
            Debug.Log($"[MyPreBuildProcess] Created base directory: {basePath}");
        }
        
        string dirName = string.IsNullOrEmpty(blueprintId) ? "local" : blueprintId;
        string avatarDir = Path.Combine(basePath, dirName);
        
        if (string.IsNullOrEmpty(blueprintId))
        {
            Debug.Log($"[MyPreBuildProcess] No blueprint ID found for {avatar.name}, exporting to local directory");
        }
        if (!Directory.Exists(avatarDir))
        {
            Directory.CreateDirectory(avatarDir);
        }

        string dateString = DateTime.Now.ToString("yyMMdd");
        string fileName = GenerateUniqueFileName(avatarDir, dateString, avatar.name, string.IsNullOrEmpty(blueprintId));
        string fullPath = Path.Combine(avatarDir, fileName);

        try
        {
            List<string> assetPaths = CollectAvatarAssets(avatar);
            
            if (assetPaths.Count > 0)
            {
                AssetDatabase.ExportPackage(assetPaths.ToArray(), fullPath, ExportPackageOptions.Recurse);
                Debug.Log($"[MyPreBuildProcess] Exported optimized avatar to: {fullPath}");
            }
            else
            {
                Debug.LogWarning($"[MyPreBuildProcess] No assets found to export for {avatar.name}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[MyPreBuildProcess] Failed to export {avatar.name}: {e.Message}");
        }
    }

    private string GenerateUniqueFileName(string directory, string dateString, string avatarName, bool isLocal)
    {
        string baseName;
        if (isLocal)
        {
            baseName = $"{dateString}-{avatarName}-";
        }
        else
        {
            baseName = $"{dateString}-";
        }
        
        int number = 1;
        string fileName;

        do
        {
            fileName = $"{baseName}{number:D3}.unitypackage";
            number++;
        }
        while (File.Exists(Path.Combine(directory, fileName)));

        return fileName;
    }

    private List<string> CollectAvatarAssets(GameObject avatar)
    {
        List<string> assetPaths = new List<string>();
        bool includeAllAssets = EditorPrefs.GetBool("Setting.AutoVariant_includeAllAssets", true);

        string avatarPrefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(avatar);
        if (!string.IsNullOrEmpty(avatarPrefabPath))
        {
            if (includeAllAssets)
            {
                assetPaths.Add(avatarPrefabPath);
                
                string[] dependencies = AssetDatabase.GetDependencies(avatarPrefabPath, true);
                foreach (string dependency in dependencies)
                {
                    if (dependency.StartsWith("Assets/") && !assetPaths.Contains(dependency))
                    {
                        assetPaths.Add(dependency);
                    }
                }
            }
            else
            {
                assetPaths.Add(avatarPrefabPath);
                
                string[] dependencies = AssetDatabase.GetDependencies(avatarPrefabPath, true);
                foreach (string dependency in dependencies)
                {
                    if (dependency.StartsWith("Assets/Untitled_Variants/") && !assetPaths.Contains(dependency))
                    {
                        assetPaths.Add(dependency);
                    }
                }
            }
            
            Debug.Log($"[MyPreBuildProcess] Collected {assetPaths.Count} assets for {avatar.name} (includeAllAssets: {includeAllAssets})");
        }
        else
        {
            Debug.LogWarning($"[MyPreBuildProcess] Could not find prefab path for {avatar.name}");
        }

        return assetPaths;
    }
}