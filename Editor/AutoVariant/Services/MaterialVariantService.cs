using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using AMU.Editor.Core.Controllers;

namespace AMU.Editor.AutoVariant.Services
{
    /// <summary>
    /// Material Variant最適化サービス
    /// マテリアルの最適化処理を提供
    /// </summary>
    public static class MaterialVariantService
    {
        /// <summary>
        /// 指定されたGameObjectのマテリアルを最適化する
        /// </summary>
        /// <param name="targetObject">最適化対象のGameObject</param>
        /// <returns>最適化が実行されたかどうか</returns>
        public static bool OptimizeMaterials(GameObject targetObject)
        {
            if (!ValidateInput(targetObject, out var parentPrefab))
                return false;

            return ProcessMaterialsRecursive(targetObject, parentPrefab);
        }

        private static bool ValidateInput(GameObject targetObject, out GameObject parentPrefab)
        {
            parentPrefab = null; if (targetObject == null)
            {
                Debug.LogError($"[MaterialVariantService] {LocalizationController.GetText("message_error_avatar_null")}");
                return false;
            }

            if (PrefabUtility.GetPrefabInstanceStatus(targetObject) != PrefabInstanceStatus.Connected)
            {
                Debug.LogError($"[MaterialVariantService] {LocalizationController.GetText("message_error_not_prefab_instance")}");
                return false;
            }

            var prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(targetObject);
            if (prefabAsset == null)
            {
                Debug.LogError($"[MaterialVariantService] {LocalizationController.GetText("message_error_prefab_asset_not_found")}");
                return false;
            }

            parentPrefab = PrefabUtility.GetCorrespondingObjectFromSource(prefabAsset);
            if (parentPrefab == null)
            {
                Debug.LogWarning($"[MaterialVariantService] {LocalizationController.GetText("message_warning_not_variant")}");
                return false;
            }

            return true;
        }

        private static bool ProcessMaterialsRecursive(GameObject variant, GameObject parent)
        {
            bool hasChanges = false;

            // 現在のオブジェクトのRendererを処理
            var variantRenderer = variant.GetComponent<Renderer>();
            var parentRenderer = parent.GetComponent<Renderer>();

            if (variantRenderer != null && parentRenderer != null)
            {
                if (ProcessRendererMaterials(variantRenderer, parentRenderer, variant.name))
                {
                    hasChanges = true;
                }
            }

            // 子オブジェクトを再帰的に処理
            int childCount = Mathf.Min(variant.transform.childCount, parent.transform.childCount);
            for (int i = 0; i < childCount; i++)
            {
                var variantChild = variant.transform.GetChild(i).gameObject;
                var parentChild = parent.transform.GetChild(i).gameObject;

                if (ProcessMaterialsRecursive(variantChild, parentChild))
                {
                    hasChanges = true;
                }
            }

            return hasChanges;
        }

        private static bool ProcessRendererMaterials(Renderer variantRenderer, Renderer parentRenderer, string objectName)
        {
            var variantMaterials = variantRenderer.sharedMaterials;
            var parentMaterials = parentRenderer.sharedMaterials; if (variantMaterials.Length != parentMaterials.Length)
            {
                Debug.LogWarning($"[MaterialVariantService] {string.Format(LocalizationController.GetText("message_warning_material_count_mismatch"), objectName)}");
                return false;
            }

            var optimizedMaterials = new Material[variantMaterials.Length];
            bool hasOptimizations = false;

            for (int i = 0; i < variantMaterials.Length; i++)
            {
                if (TryOptimizeMaterial(variantMaterials[i], parentMaterials[i], out var optimizedMaterial))
                {
                    optimizedMaterials[i] = optimizedMaterial;
                    hasOptimizations = true;
                    Debug.Log($"[MaterialVariantService] {string.Format(LocalizationController.GetText("message_info_material_optimized"), objectName, i)}");
                }
                else
                {
                    optimizedMaterials[i] = variantMaterials[i];
                }
            }

            if (hasOptimizations)
            {
                ApplyMaterialChanges(variantRenderer, optimizedMaterials, objectName);
            }

            return hasOptimizations;
        }

        private static bool TryOptimizeMaterial(Material variantMaterial, Material parentMaterial, out Material optimizedMaterial)
        {
            optimizedMaterial = variantMaterial;

            if (variantMaterial == null || parentMaterial == null)
                return false;

            if (variantMaterial == parentMaterial)
                return false;

            var variantHash = MaterialHashCalculator.Calculate(variantMaterial);
            var parentHash = MaterialHashCalculator.Calculate(parentMaterial);

            if (variantHash == parentHash)
            {
                optimizedMaterial = parentMaterial;
                return true;
            }

            return false;
        }

        private static void ApplyMaterialChanges(Renderer renderer, Material[] newMaterials, string objectName)
        {
            Undo.RecordObject(renderer, "Optimize Variant Materials");
            renderer.sharedMaterials = newMaterials;
            EditorUtility.SetDirty(renderer);

            ApplyPrefabOverride(renderer, objectName);
        }

        private static void ApplyPrefabOverride(Renderer renderer, string objectName)
        {
            var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(renderer.gameObject);
            if (prefabRoot == null) return;

            try
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);

                var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefabRoot);
                if (!string.IsNullOrEmpty(prefabPath))
                {
                    PrefabUtility.ApplyPrefabInstance(prefabRoot, InteractionMode.AutomatedAction);
                    Debug.Log($"[MaterialVariantService] {string.Format(LocalizationController.GetText("message_info_override_applied"), objectName)}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MaterialVariantService] {string.Format(LocalizationController.GetText("message_error_override_failed"), objectName, e.Message)}");
            }
        }

        /// <summary>
        /// マテリアルのハッシュ値計算ユーティリティ
        /// </summary>
        public static class MaterialHashCalculator
        {
            public static string Calculate(Material material)
            {
                if (material == null)
                    return string.Empty;

                var hashBuilder = new StringBuilder();

                AppendShaderInfo(material, hashBuilder);
                AppendShaderProperties(material, hashBuilder);
                AppendShaderKeywords(material, hashBuilder);

                return ComputeMD5Hash(hashBuilder.ToString());
            }

            private static void AppendShaderInfo(Material material, StringBuilder hashBuilder)
            {
                if (material.shader != null)
                {
                    hashBuilder.Append($"shader:{material.shader.name};");
                }
            }

            private static void AppendShaderProperties(Material material, StringBuilder hashBuilder)
            {
                var shader = material.shader;
                if (shader == null) return;

                for (int i = 0; i < shader.GetPropertyCount(); i++)
                {
                    var propName = shader.GetPropertyName(i);
                    var propType = shader.GetPropertyType(i);

                    try
                    {
                        AppendPropertyValue(material, propName, propType, hashBuilder);
                    }
                    catch (System.Exception)
                    {
                        // プロパティが存在しない場合はスキップ
                        continue;
                    }
                }
            }

            private static void AppendPropertyValue(Material material, string propName, UnityEngine.Rendering.ShaderPropertyType propType, StringBuilder hashBuilder)
            {
                switch (propType)
                {
                    case UnityEngine.Rendering.ShaderPropertyType.Color:
                        var color = material.GetColor(propName);
                        hashBuilder.Append($"{propName}:c{color.r:F6},{color.g:F6},{color.b:F6},{color.a:F6};");
                        break;

                    case UnityEngine.Rendering.ShaderPropertyType.Vector:
                        var vector = material.GetVector(propName);
                        hashBuilder.Append($"{propName}:v{vector.x:F6},{vector.y:F6},{vector.z:F6},{vector.w:F6};");
                        break;

                    case UnityEngine.Rendering.ShaderPropertyType.Float:
                    case UnityEngine.Rendering.ShaderPropertyType.Range:
                        var floatVal = material.GetFloat(propName);
                        hashBuilder.Append($"{propName}:f{floatVal:F6};");
                        break;

                    case UnityEngine.Rendering.ShaderPropertyType.Texture:
                        AppendTextureInfo(material, propName, hashBuilder);
                        break;

                    case UnityEngine.Rendering.ShaderPropertyType.Int:
                        var intVal = material.GetInt(propName);
                        hashBuilder.Append($"{propName}:i{intVal};");
                        break;
                }
            }

            private static void AppendTextureInfo(Material material, string propName, StringBuilder hashBuilder)
            {
                var texture = material.GetTexture(propName);
                if (texture != null)
                {
                    hashBuilder.Append($"{propName}:t{texture.GetInstanceID()};");

                    var offset = material.GetTextureOffset(propName);
                    var scale = material.GetTextureScale(propName);
                    hashBuilder.Append($"{propName}_ST:{offset.x:F6},{offset.y:F6},{scale.x:F6},{scale.y:F6};");
                }
                else
                {
                    hashBuilder.Append($"{propName}:tnull;");
                }
            }

            private static void AppendShaderKeywords(Material material, StringBuilder hashBuilder)
            {
                var keywords = material.shaderKeywords;
                if (keywords?.Length > 0)
                {
                    var sortedKeywords = keywords.OrderBy(k => k);
                    hashBuilder.Append($"keywords:{string.Join(",", sortedKeywords)};");
                }
            }

            private static string ComputeMD5Hash(string input)
            {
                using (var md5 = MD5.Create())
                {
                    var inputBytes = Encoding.UTF8.GetBytes(input);
                    var hashBytes = md5.ComputeHash(inputBytes);

                    var result = new StringBuilder();
                    foreach (var b in hashBytes)
                    {
                        result.Append(b.ToString("x2"));
                    }
                    return result.ToString();
                }
            }
        }
    }
}
