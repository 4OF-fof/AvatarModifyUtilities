using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using AMU.Editor.Core.Helper;
using AMU.Editor.Core.Controllers;

namespace AMU.Editor.AutoVariant.Services
{
    /// <summary>
    /// Rendererのマテリアル状態を保存するクラス
    /// </summary>
    [System.Serializable]
    public class RendererMaterialState
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

    /// <summary>
    /// マテリアル最適化管理サービス
    /// アクティブなアバターのマテリアル最適化処理を管理
    /// </summary>
    public static class MaterialOptimizationService
    {
        private static readonly List<RendererMaterialState> _materialStates = new List<RendererMaterialState>();

        /// <summary>
        /// アクティブなアバターのマテリアルを最適化する
        /// </summary>
        public static void OptimizeActiveAvatars()
        {
            var avatars = AvatarValidationService.FindActiveAvatars();

            ClearMaterialStates();

            foreach (var avatar in avatars)
            {
                OptimizeAvatarMaterials(avatar);
            }

            RestoreMaterialStates();
        }

        /// <summary>
        /// 指定されたアバターのマテリアルを最適化する
        /// </summary>
        /// <param name="avatar">最適化対象のアバター</param>
        public static void OptimizeAvatar(GameObject avatar)
        {
            if (avatar == null)
            {
                Debug.LogError($"[MaterialOptimizationService] {LocalizationController.GetText("message_error_avatar_null")}");
                return;
            }

            SaveMaterialStates(avatar);
            OptimizeAvatarMaterials(avatar);
            RestoreMaterialStates();
        }

        private static void OptimizeAvatarMaterials(GameObject avatar)
        {
            SaveMaterialStates(avatar);
            MaterialVariantService.OptimizeMaterials(avatar);

            Debug.Log($"[MaterialOptimizationService] {string.Format(LocalizationController.GetText("message_info_optimization_completed"), avatar.name)}");

            OptimizeNestedPrefabs(avatar);
            AvatarExportService.ExportOptimizedAvatar(avatar);
        }

        private static void OptimizeNestedPrefabs(GameObject avatar)
        {
            var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(avatar);
            if (string.IsNullOrEmpty(prefabPath))
                return;

            var visited = new HashSet<string>();
            OptimizeNestedPrefabsRecursive(prefabPath, visited);
        }

        private static void OptimizeNestedPrefabsRecursive(string prefabPath, HashSet<string> visited)
        {
            if (visited.Contains(prefabPath))
                return;

            visited.Add(prefabPath);

            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset == null)
                return;

            var tempInstance = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;
            if (tempInstance == null)
                return;

            try
            {
                OptimizeMaterialsForAllChildren(tempInstance);
                PrefabUtility.ApplyPrefabInstance(tempInstance, InteractionMode.AutomatedAction);
            }
            finally
            {
                Object.DestroyImmediate(tempInstance);
            }

            ProcessDependentPrefabs(prefabPath, visited);
        }

        private static void ProcessDependentPrefabs(string prefabPath, HashSet<string> visited)
        {
            var dependencies = AssetDatabase.GetDependencies(prefabPath, true);
            foreach (var dependency in dependencies)
            {
                if (dependency.EndsWith(".prefab") && dependency != prefabPath)
                {
                    OptimizeNestedPrefabsRecursive(dependency, visited);
                }
            }
        }

        private static void OptimizeMaterialsForAllChildren(GameObject root)
        {
            MaterialVariantService.OptimizeMaterials(root);

            foreach (Transform child in root.transform)
            {
                OptimizeMaterialsForAllChildren(child.gameObject);
            }
        }

        private static void SaveMaterialStates(GameObject avatar)
        {
            var renderers = avatar.GetComponentsInChildren<Renderer>(true);

            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterials != null && renderer.sharedMaterials.Length > 0)
                {
                    _materialStates.Add(new RendererMaterialState(renderer));
                }
            }

            Debug.Log($"[MaterialOptimizationService] {string.Format(LocalizationController.GetText("message_info_material_states_saved"), avatar.name, renderers.Length)}");
        }

        private static void RestoreMaterialStates()
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

            Debug.Log($"[MaterialOptimizationService] {string.Format(LocalizationController.GetText("message_info_materials_restored"), restoredCount)}");
        }

        private static void ClearMaterialStates()
        {
            _materialStates.Clear();
        }
    }
}
