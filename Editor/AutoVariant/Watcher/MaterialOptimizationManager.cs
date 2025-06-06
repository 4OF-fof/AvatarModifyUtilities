using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using AMU.Editor.Core.Helper;

namespace AMU.Editor.AutoVariant.Watcher
{
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

    public static class MaterialOptimizationManager
    {
        private static readonly List<RendererMaterialState> _materialStates = new List<RendererMaterialState>();

        public static void OptimizeActiveAvatars()
        {
            var avatars = AvatarValidator.FindActiveAvatars();
            
            ClearMaterialStates();
            
            foreach (var avatar in avatars)
            {
                OptimizeAvatarMaterials(avatar);
            }
            
            RestoreMaterialStates();
        }

        private static void OptimizeAvatarMaterials(GameObject avatar)
        {
            SaveMaterialStates(avatar);
            MaterialVariantOptimizer.OptimizeMaterials(avatar);
            
            Debug.Log($"[MaterialOptimizationManager] Optimized materials for VRC Avatar: {avatar.name}");

            OptimizeNestedPrefabs(avatar);
            AvatarExporter.ExportOptimizedAvatar(avatar);
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
            MaterialVariantOptimizer.OptimizeMaterials(root);
            
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
            
            Debug.Log($"[MaterialOptimizationManager] Saved material states for {renderers.Length} renderers in {avatar.name}");
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
            
            Debug.Log($"[MaterialOptimizationManager] Restored materials for {restoredCount} renderers");
        }

        private static void ClearMaterialStates()
        {
            _materialStates.Clear();
        }
    }
}
