using UnityEngine;
using UnityEditor;
using System.IO;
using Untitled.Editor.Core.Helper;

[InitializeOnLoad]
public static class PrefabAdditionDetector
{
    static PrefabAdditionDetector()
    {
        if (!EditorPrefs.GetBool("Setting.AutoVariant_enableAutoVariant", false)) return;
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
    }

    static void OnHierarchyChanged()
    {
        if (!EditorPrefs.GetBool("Setting.AutoVariant_enableAutoVariant", false)) return;
        var addedPrefabs = FindAddedPrefabRoots();
        foreach (var go in addedPrefabs)
        {
            HandlePrefabAddition(go);
        }
    }

    static System.Collections.Generic.List<GameObject> FindAddedPrefabRoots()
    {
        if (!EditorPrefs.GetBool("Setting.AutoVariant_enableAutoVariant", false)) return new System.Collections.Generic.List<GameObject>();
        var result = new System.Collections.Generic.List<GameObject>();
        foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (!go.scene.IsValid()) continue;
            if (go.transform.parent != null) continue;
            if ((go.hideFlags & HideFlags.HideInHierarchy) != 0) continue;
            if (!PrefabUtility.IsAnyPrefabInstanceRoot(go)) continue;

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

        string variantName = "Untitled_" + go.name + ".prefab";
        string variantPath = Path.Combine(variantDir, variantName).Replace("\\", "/");

        if (!File.Exists(variantPath))
        {
            PrefabUtility.SaveAsPrefabAssetAndConnect(go, variantPath, InteractionMode.UserAction);
            Debug.Log($"Prefab Variant created: {variantPath}");
        }

        ReplaceWithVariant(go, variantPath);
    }

    static void EnsureVariantDirectoryExists(string variantDir)
    {
        if (!EditorPrefs.GetBool("Setting.AutoVariant_enableAutoVariant", false)) return;
        if (!AssetDatabase.IsValidFolder(variantDir))
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Untitled_Variants"));
            AssetDatabase.Refresh();
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
