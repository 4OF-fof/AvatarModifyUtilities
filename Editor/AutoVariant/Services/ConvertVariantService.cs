using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using AMU.Editor.Core.Helper;
using AMU.Editor.AutoVariant.Controllers;

namespace AMU.Editor.AutoVariant.Services
{
    /// <summary>
    /// プリファブ変換監視サービス
    /// プリファブが追加された際の自動変換処理を管理
    /// </summary>
    [InitializeOnLoad]
    public static class ConvertVariantService
    {
        private static bool isProcessing = false;
        private static System.Collections.Generic.HashSet<int> processedInstanceIds =
            new System.Collections.Generic.HashSet<int>();
        private static double lastClearTime = 0;

        static ConvertVariantService()
        {
            Initialize();
        }

        /// <summary>
        /// サービスを初期化する
        /// </summary>
        public static void Initialize()
        {
            if (!AutoVariantController.IsAutoVariantEnabled())
                return;

            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            EditorApplication.update += ClearProcessedIds;
        }

        /// <summary>
        /// サービスを停止する
        /// </summary>
        public static void Shutdown()
        {
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            EditorApplication.update -= ClearProcessedIds;
        }

        private static void ClearProcessedIds()
        {
            if (EditorApplication.timeSinceStartup - lastClearTime > 5.0)
            {
                processedInstanceIds.Clear();
                lastClearTime = EditorApplication.timeSinceStartup;
            }
        }

        private static void OnHierarchyChanged()
        {
            if (!AutoVariantController.IsAutoVariantEnabled())
                return;
            if (isProcessing)
                return;
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                return;
            }

            isProcessing = true;
            try
            {
                var addedPrefabs = FindAddedPrefabRoots();
                foreach (var go in addedPrefabs)
                {
                    int instanceId = go.GetInstanceID();
                    if (processedInstanceIds.Contains(instanceId))
                        continue;
                    processedInstanceIds.Add(instanceId);
                    HandlePrefabAddition(go);
                }
            }
            finally
            {
                isProcessing = false;
            }
        }

        private static System.Collections.Generic.List<GameObject> FindAddedPrefabRoots()
        {
            if (!AutoVariantController.IsAutoVariantEnabled())
                return new System.Collections.Generic.List<GameObject>();

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
                if (IsAMUPrefab(go, prefabAsset)) continue;

                result.Add(go);
            }
            return result;
        }

        private static bool IsAMUPrefab(GameObject go, Object prefabAsset)
        {
            return go.name.StartsWith("AMU_") || (prefabAsset != null && prefabAsset.name.StartsWith("AMU_"));
        }

        private static void HandlePrefabAddition(GameObject go)
        {
            if (!AutoVariantController.IsAutoVariantEnabled())
                return;

            var blueprintId = PipelineManagerHelper.GetBlueprintId(go);
            if (!string.IsNullOrEmpty(blueprintId))
                return;

            var prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(go);
            var prefabPath = AssetDatabase.GetAssetPath(prefabAsset);

            Debug.Log($"[ConvertVariantService] Prefab added to scene: {go.name}");

            if (string.IsNullOrEmpty(prefabPath))
                return;

            string variantDir = "Assets/AMU_Variants";
            EnsureVariantDirectoryExists(variantDir);

            string materialDir = Path.Combine(variantDir, "Material").Replace("\\", "/");
            EnsureVariantDirectoryExists(materialDir);

            bool isPrefabChild = go.transform.parent != null &&
                               PrefabUtility.IsPartOfAnyPrefab(go.transform.parent.gameObject);

            if (isPrefabChild)
            {
                CopyAndReplaceMaterials(go, materialDir);
                Debug.Log($"[ConvertVariantService] Materials processed for prefab child: {go.name}");
            }
            else
            {
                CopyAndReplaceMaterials(go, materialDir);

                string variantName = "AMU_" + go.name + ".prefab";
                string variantPath = Path.Combine(variantDir, variantName).Replace("\\", "/");

                if (!File.Exists(variantPath))
                {
                    PrefabUtility.SaveAsPrefabAssetAndConnect(go, variantPath, InteractionMode.UserAction);
                    Debug.Log($"[ConvertVariantService] Prefab Variant created: {variantPath}");
                }

                ReplaceWithVariant(go, variantPath);
            }
        }

        private static void EnsureVariantDirectoryExists(string variantDir)
        {
            if (!AutoVariantController.IsAutoVariantEnabled())
                return;
            if (!AssetDatabase.IsValidFolder(variantDir))
            {
                Directory.CreateDirectory(Path.Combine(Application.dataPath, variantDir.Replace("Assets/", "")));
                AssetDatabase.Refresh();
            }
        }

        private static void CopyAndReplaceMaterials(GameObject go, string materialDir)
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

                    if (matPath.StartsWith("Assets/AMU_Variants/")) continue;

                    string matCopyPath = Path.Combine(materialDir, mat.name + ".mat").Replace("\\", "/");
                    if (!AssetDatabase.IsValidFolder(materialDir))
                    {
                        Directory.CreateDirectory(Path.Combine(Application.dataPath, materialDir.Replace("Assets/", "")));
                        AssetDatabase.Refresh();
                    }
                    if (!File.Exists(matCopyPath))
                    {
                        AssetDatabase.CopyAsset(matPath, matCopyPath);
                        Debug.Log($"[ConvertVariantService] Material copied: {matPath} -> {matCopyPath}");
                    }
                    var matCopy = AssetDatabase.LoadAssetAtPath<Material>(matCopyPath);
                    if (matCopy != null)
                    {
                        materials[i] = matCopy;
                        changed = true;
                        Debug.Log($"[ConvertVariantService] Material replaced on {renderer.name}: {mat.name} -> {matCopy.name}");
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

        private static void ReplaceWithVariant(GameObject original, string variantPath)
        {
            if (!AutoVariantController.IsAutoVariantEnabled())
                return;
            var variantPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(variantPath);
            if (variantPrefab == null)
                return;

            var originalTransform = original.transform;
            var position = originalTransform.position;
            var rotation = originalTransform.rotation;
            var scale = originalTransform.localScale;
            var siblingIndex = originalTransform.GetSiblingIndex();
            var scene = original.scene;

            Object.DestroyImmediate(original);

            GameObject newInstance = (GameObject)PrefabUtility.InstantiatePrefab(variantPrefab, scene);
            newInstance.transform.SetPositionAndRotation(position, rotation);
            newInstance.transform.localScale = scale;
            newInstance.transform.SetSiblingIndex(siblingIndex);

            Debug.Log($"[ConvertVariantService] Scene object replaced with variant: {variantPrefab.name}");
        }
    }
}
