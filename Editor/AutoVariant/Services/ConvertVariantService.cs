using System.IO;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

using AMU.Editor.Core.Api;
using AMU.Editor.AutoVariant.Helper;
using AMU.AutoVariant.Data;

namespace AMU.Editor.AutoVariant.Services
{
    [InitializeOnLoad]
    public static class ConvertVariantService
    {
        private static bool isProcessing = false;
        private static HashSet<int> processedInstanceIds =
            new HashSet<int>();
        private static double lastClearTime = 0;

        static ConvertVariantService()
        {
            Initialize();
        }

        public static void Initialize()
        {
            if (!SettingAPI.GetSetting<bool>("AutoVariant_enableAutoVariant"))
                return;

            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            EditorApplication.update += ClearProcessedIds;
        }

        public static void Shutdown()
        {
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            EditorApplication.update -= ClearProcessedIds;
        }

        private static void ClearProcessedIds()
        {
            if (EditorApplication.timeSinceStartup - lastClearTime > 1.0)
            {
                processedInstanceIds.Clear();
                lastClearTime = EditorApplication.timeSinceStartup;
            }
        }

        private static void OnHierarchyChanged()
        {
            if (!SettingAPI.GetSetting<bool>("AutoVariant_enableAutoVariant"))
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

        private static List<GameObject> FindAddedPrefabRoots()
        {
            if (!SettingAPI.GetSetting<bool>("AutoVariant_enableAutoVariant"))
                return new List<GameObject>();

            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                return new List<GameObject>();
            }

            var result = new List<GameObject>();
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
            return go.GetComponent<AMUAutoVariantComponent>() != null;
        }

        private static void HandlePrefabAddition(GameObject go)
        {
            if (!SettingAPI.GetSetting<bool>("AutoVariant_enableAutoVariant"))
                return;

            if (!VRCObjectHelper.IsVRCAvatar(go)) return;

            var blueprintId = VRCObjectHelper.GetBlueprintId(go);
            if (!string.IsNullOrEmpty(blueprintId))
                return;

            var prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(go);
            var prefabPath = AssetDatabase.GetAssetPath(prefabAsset);

            Debug.Log($"[ConvertVariantService] {string.Format(LocalizationAPI.GetText("AutoVariant_message_info_prefab_added"), go.name)}");

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
                Debug.Log($"[ConvertVariantService] {string.Format(LocalizationAPI.GetText("AutoVariant_message_info_materials_processed"), go.name)}");
            }
            else
            {
                CopyAndReplaceMaterials(go, materialDir);

                string variantName = go.name + ".prefab";
                string variantPath = Path.Combine(variantDir, variantName).Replace("\\", "/");

                if (!File.Exists(variantPath))
                {
                    if (go.GetComponent<AMUAutoVariantComponent>() == null)
                    {
                        go.AddComponent<AMUAutoVariantComponent>();
                    }
                    
                    PrefabUtility.SaveAsPrefabAssetAndConnect(go, variantPath, InteractionMode.UserAction);
                    Debug.Log($"[ConvertVariantService] {string.Format(LocalizationAPI.GetText("AutoVariant_message_info_variant_created"), variantPath)}");
                }
                else
                {
                    if (go.GetComponent<AMUAutoVariantComponent>() == null)
                    {
                        go.AddComponent<AMUAutoVariantComponent>();
                    }
                }

                ReplaceWithVariant(go, variantPath);
            }
        }

        private static void EnsureVariantDirectoryExists(string variantDir)
        {
            if (!SettingAPI.GetSetting<bool>("AutoVariant_enableAutoVariant"))
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
                        Debug.Log($"[ConvertVariantService] {string.Format(LocalizationAPI.GetText("AutoVariant_message_info_material_copied"), matPath, matCopyPath)}");
                    }
                    var matCopy = AssetDatabase.LoadAssetAtPath<Material>(matCopyPath);
                    if (matCopy != null)
                    {
                        materials[i] = matCopy;
                        changed = true;
                        Debug.Log($"[ConvertVariantService] {string.Format(LocalizationAPI.GetText("AutoVariant_message_info_material_replaced"), renderer.name, mat.name, matCopy.name)}");
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
            if (!SettingAPI.GetSetting<bool>("AutoVariant_enableAutoVariant"))
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

            if (newInstance.GetComponent<AMUAutoVariantComponent>() == null)
            {
                newInstance.AddComponent<AMUAutoVariantComponent>();
            }

            processedInstanceIds.Add(newInstance.GetInstanceID());

            Debug.Log($"[ConvertVariantService] {string.Format(LocalizationAPI.GetText("AutoVariant_message_info_scene_object_replaced"), variantPrefab.name)}");
        }
    }
}
