using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using Untitled.Editor.Core.Helper;

[InitializeOnLoad]
public static class PrefabAdditionDetector
{
    static System.Collections.Generic.HashSet<int> recentlyHandled = new System.Collections.Generic.HashSet<int>();
    static double lastClearTime = 0;
    static double clearInterval = 1.0;

    static PrefabAdditionDetector()
    {
        if (!EditorPrefs.GetBool("Setting.AutoVariant_enableAutoVariant", false)) return;
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
    }

    static void OnHierarchyChanged()
    {
        if (!EditorPrefs.GetBool("Setting.AutoVariant_enableAutoVariant", false)) return;
        if (PrefabStageUtility.GetCurrentPrefabStage() != null)
        {
            return;
        }
        if (EditorApplication.timeSinceStartup - lastClearTime > clearInterval)
        {
            recentlyHandled.Clear();
            lastClearTime = EditorApplication.timeSinceStartup;
        }
        var addedPrefabs = FindAddedPrefabRoots();
        foreach (var go in addedPrefabs)
        {
            if (recentlyHandled.Contains(go.GetInstanceID())) continue;
            HandlePrefabAddition(go);
            recentlyHandled.Add(go.GetInstanceID());
        }
    }

    static System.Collections.Generic.List<GameObject> FindAddedPrefabRoots()
    {
        if (!EditorPrefs.GetBool("Setting.AutoVariant_enableAutoVariant", false)) return new System.Collections.Generic.List<GameObject>();
        
        if (PrefabStageUtility.GetCurrentPrefabStage() != null)
        {
            return new System.Collections.Generic.List<GameObject>();
        }
        
        var result = new System.Collections.Generic.List<GameObject>();
        foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (!go.scene.IsValid()) continue;
            if ((go.hideFlags & HideFlags.HideInHierarchy) != 0) continue;
            
            bool isPrefabRoot = go.transform.parent == null && PrefabUtility.IsAnyPrefabInstanceRoot(go);
            bool isPrefabChild = go.transform.parent != null && PrefabUtility.IsPartOfAnyPrefab(go) && 
                               PrefabUtility.IsAnyPrefabInstanceRoot(go);
            
            if (!isPrefabRoot && !isPrefabChild) continue;

            var prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(go);
            if (IsUntitled(go, prefabAsset)) continue;

            result.Add(go);
        }
        return result;
    }

    static bool IsUntitled(GameObject go, Object prefabAsset)
    {
        return go.name.StartsWith("Untitled_") || (prefabAsset != null && prefabAsset.name.StartsWith("Untitled_"));
    }

    static void HandlePrefabAddition(GameObject go)
    {
        if (!EditorPrefs.GetBool("Setting.AutoVariant_enableAutoVariant", false)) return;

        var blueprintId = PipelineManagerHelper.GetBlueprintId(go);
        if (!string.IsNullOrEmpty(blueprintId)) return;

        var prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(go);
        var prefabPath = AssetDatabase.GetAssetPath(prefabAsset);

        Debug.Log($"Prefab added to scene: {go.name}");

        if (string.IsNullOrEmpty(prefabPath)) return;

        string variantDir = "Assets/Untitled_Variants";
        EnsureVariantDirectoryExists(variantDir);

        string materialDir = Path.Combine(variantDir, "Material").Replace("\\", "/");
        EnsureVariantDirectoryExists(materialDir);

        bool isPrefabChild = go.transform.parent != null && 
                           PrefabUtility.IsPartOfAnyPrefab(go.transform.parent.gameObject);

        if (isPrefabChild)
        {
            CopyAndReplaceMaterials(go, materialDir);
            Debug.Log($"Materials processed for prefab child: {go.name}");
        }
        else
        {
            CopyAndReplaceMaterials(go, materialDir);

            string variantName = "Untitled_" + go.name + ".prefab";
            string variantPath = Path.Combine(variantDir, variantName).Replace("\\", "/");

            if (!File.Exists(variantPath))
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(go, variantPath, InteractionMode.UserAction);
                Debug.Log($"Prefab Variant created: {variantPath}");
            }

            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            try
            {
                ReplaceWithVariant(go, variantPath);
            }
            finally
            {
                EditorApplication.hierarchyChanged += OnHierarchyChanged;
            }
        }
    }

    static void EnsureVariantDirectoryExists(string variantDir)
    {
        if (!EditorPrefs.GetBool("Setting.AutoVariant_enableAutoVariant", false)) return;
        if (!AssetDatabase.IsValidFolder(variantDir))
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, variantDir.Replace("Assets/", "")));
            AssetDatabase.Refresh();
        }
    }

    static void CopyAndReplaceMaterials(GameObject go, string materialDir)
    {
        foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
        {
            var materials = renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < materials.Length; i++)
            {
                var mat = materials[i];
                if (mat == null) continue;

                string matPath = AssetDatabase.GetAssetPath(mat);
                if (string.IsNullOrEmpty(matPath)) continue;

                if (matPath.StartsWith("Assets/Untitled_Variants/")) continue;

                string matCopyPath = Path.Combine(materialDir, mat.name + ".mat").Replace("\\", "/");
                if (!AssetDatabase.IsValidFolder(materialDir))
                {
                    Directory.CreateDirectory(Path.Combine(Application.dataPath, materialDir.Replace("Assets/", "")));
                    AssetDatabase.Refresh();
                }
                if (!File.Exists(matCopyPath))
                {
                    AssetDatabase.CopyAsset(matPath, matCopyPath);
                    Debug.Log($"Material copied: {matPath} -> {matCopyPath}");
                }
                var matCopy = AssetDatabase.LoadAssetAtPath<Material>(matCopyPath);
                if (matCopy != null)
                {
                    materials[i] = matCopy;
                    changed = true;
                    Debug.Log($"Material replaced on {renderer.name}: {mat.name} -> {matCopy.name}");
                }
            }
            if (changed)
            {
                renderer.sharedMaterials = materials;
                EditorUtility.SetDirty(renderer);
                
                if (PrefabUtility.IsPartOfAnyPrefab(renderer.gameObject))
                {
                    var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(renderer.gameObject);
                    if (prefabRoot != null)
                    {
                        PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                        EditorUtility.SetDirty(prefabRoot);
                    }
                }
            }
        }
    }

    static void ReplaceWithVariant(GameObject original, string variantPath)
    {
        if (!EditorPrefs.GetBool("Setting.AutoVariant_enableAutoVariant", false)) return;
        var variantPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(variantPath);
        if (variantPrefab == null) return;

        GameObject newInstance = (GameObject)PrefabUtility.InstantiatePrefab(variantPrefab, original.scene);
        newInstance.transform.SetPositionAndRotation(original.transform.position, original.transform.rotation);
        newInstance.transform.localScale = original.transform.localScale;
        newInstance.transform.SetSiblingIndex(original.transform.GetSiblingIndex());

        Object.DestroyImmediate(original);
        Debug.Log($"Scene object replaced with variant: {variantPrefab.name}");
    }
}
