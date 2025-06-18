using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AMU.Editor.VrcAssetManager.Schema
{
    /// <summary>
    /// アセットグループのスキーマ
    /// </summary>
    [Serializable]
    public class AssetGroupSchema
    {
        [SerializeField] private string _parentGroupId;
        [SerializeField] private List<AssetId> _childAssetIds;
        [SerializeField] private int _groupLevel;
        [SerializeField] private string _groupName;

        public string ParentGroupId
        {
            get => _parentGroupId ?? string.Empty;
            set => _parentGroupId = value?.Trim() ?? string.Empty;
        }

        public IReadOnlyList<AssetId> ChildAssetIds => _childAssetIds ?? new List<AssetId>();

        public int GroupLevel
        {
            get => _groupLevel;
            set => _groupLevel = Math.Max(0, value);
        }

        public string GroupName
        {
            get => _groupName ?? string.Empty;
            set => _groupName = value?.Trim() ?? string.Empty;
        }

        public AssetGroupSchema()
        {
            _parentGroupId = string.Empty;
            _childAssetIds = new List<AssetId>();
            _groupLevel = 0;
            _groupName = string.Empty;
        }

        public void AddChildAsset(AssetId childId)
        {
            if (string.IsNullOrEmpty(childId.Value)) return;

            _childAssetIds ??= new List<AssetId>();
            if (!_childAssetIds.Contains(childId))
            {
                _childAssetIds.Add(childId);
            }
        }

        public void RemoveChildAsset(AssetId childId)
        {
            if (string.IsNullOrEmpty(childId.Value)) return;
            _childAssetIds?.Remove(childId);
        }
        public void SetParentGroup(string parentId, int level = 1)
        {
            _parentGroupId = parentId?.Trim() ?? string.Empty;
            _groupLevel = Math.Max(0, level);
        }

        public void RemoveFromParentGroup()
        {
            _parentGroupId = string.Empty;
            _groupLevel = 0;
        }
        public bool IsVisibleInList()
        {
            // 親グループが存在するアセットは通常非表示
            return string.IsNullOrEmpty(_parentGroupId);
        }

        public void ClearChildren()
        {
            _childAssetIds?.Clear();
        }
        public bool IsDescendantOf(string ancestorId)
        {
            if (string.IsNullOrEmpty(ancestorId)) return false;
            return _parentGroupId == ancestorId;
        }

        public IEnumerable<AssetId> GetAllDescendants()
        {
            return _childAssetIds ?? Enumerable.Empty<AssetId>();
        }
    }

    /// <summary>
    /// グループ階層の管理ユーティリティ
    /// </summary>
    public static class AssetGroupHierarchy
    {
        /// <summary>
        /// 循環参照をチェックする
        /// </summary>
        public static bool WouldCreateCycle(AssetId parentId, AssetId childId,
            IReadOnlyDictionary<AssetId, AssetGroupSchema> groups)
        {
            if (parentId == childId) return true;

            var visited = new HashSet<AssetId>();
            var current = parentId;

            while (!string.IsNullOrEmpty(current.Value) && visited.Add(current))
            {
                if (!groups.TryGetValue(current, out var group)) break;

                if (string.IsNullOrEmpty(group.ParentGroupId)) break;
                if (!AssetId.TryParse(group.ParentGroupId, out var parentAssetId)) break;

                current = parentAssetId;
                if (current == childId) return true;
            }

            return false;
        }

        /// <summary>
        /// 指定されたアセットの最大深度を計算する
        /// </summary>
        public static int CalculateMaxDepth(AssetId assetId,
            IReadOnlyDictionary<AssetId, AssetGroupSchema> groups, int maxDepth = 100)
        {
            if (!groups.TryGetValue(assetId, out var group)) return 0;

            var depth = 0;
            var visited = new HashSet<AssetId>();

            foreach (var childId in group.ChildAssetIds)
            {
                if (visited.Add(childId))
                {
                    var childDepth = CalculateMaxDepth(childId, groups, maxDepth - 1);
                    depth = Math.Max(depth, childDepth + 1);
                }

                if (depth >= maxDepth) break;
            }

            return depth;
        }

        /// <summary>
        /// ルートアセット（親を持たないアセット）を取得する
        /// </summary>
        public static IEnumerable<AssetId> GetRootAssets(IReadOnlyDictionary<AssetId, AssetGroupSchema> groups)
        {
            return groups.Where(kvp => string.IsNullOrEmpty(kvp.Value.ParentGroupId)).Select(kvp => kvp.Key);
        }

        /// <summary>
        /// 指定されたアセットの全ての子孫を取得する
        /// </summary>
        public static IEnumerable<AssetId> GetAllDescendants(AssetId rootId,
            IReadOnlyDictionary<AssetId, AssetGroupSchema> groups)
        {
            if (!groups.TryGetValue(rootId, out var rootGroup)) yield break;

            var queue = new Queue<AssetId>(rootGroup.ChildAssetIds);
            var visited = new HashSet<AssetId>();

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!visited.Add(current)) continue;

                yield return current;

                if (groups.TryGetValue(current, out var group))
                {
                    foreach (var child in group.ChildAssetIds)
                    {
                        if (!visited.Contains(child))
                        {
                            queue.Enqueue(child);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 指定されたアセットの親の階層パスを取得する
        /// </summary>
        public static IEnumerable<AssetId> GetAncestorPath(AssetId assetId,
            IReadOnlyDictionary<AssetId, AssetGroupSchema> groups)
        {
            var path = new List<AssetId>();
            var current = assetId;
            var visited = new HashSet<AssetId>();

            while (!string.IsNullOrEmpty(current.Value) && visited.Add(current))
            {
                if (!groups.TryGetValue(current, out var group)) break;
                if (string.IsNullOrEmpty(group.ParentGroupId)) break;

                if (AssetId.TryParse(group.ParentGroupId, out var parentAssetId))
                {
                    path.Add(parentAssetId);
                    current = parentAssetId;
                }
                else
                {
                    break;
                }
            }

            path.Reverse();
            return path;
        }
    }
}
