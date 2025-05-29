# AutoVariant/Helper ディレクトリの内容と活用方法

## 概要
`AutoVariant/Helper` ディレクトリには、AutoVariant機能の核となるマテリアル最適化とVariant管理のためのヘルパークラスが格納されています。これらのクラスは、マテリアルの重複除去、効率的な比較、プレハブVariantの最適化処理を提供します。

---

## 各ファイルの説明

### MaterialHelper.cs
- **役割:** マテリアルの最適化とVariant管理のための中核機能を提供する静的クラス群
- **主要クラス:**
  - `MaterialVariantOptimizer`: メインの最適化エンジン
  - `MaterialHashCalculator`: マテリアルの比較とハッシュ化を担当

#### MaterialVariantOptimizer クラス
- **主な機能:**
  - `OptimizeMaterials(GameObject targetObject): void`
    - 指定されたGameObjectとその子オブジェクトのマテリアルを最適化
    - Variantプレハブと親プレハブのマテリアルを比較し、同一のものを統合
  
- **処理フロー:**
  1. 入力検証（プレハブインスタンスかチェック）
  2. 親プレハブとの関係性確認
  3. 再帰的にRendererコンポーネントを走査
  4. マテリアルハッシュ比較による最適化判定
  5. 最適化されたマテリアルの適用とプレハブオーバーライド

- **利用例:**
  ```csharp
  GameObject avatarObject = Selection.activeGameObject;
  MaterialVariantOptimizer.OptimizeMaterials(avatarObject);
  ```

#### MaterialHashCalculator クラス
- **役割:** マテリアルの内容を詳細に分析し、同等性を判定するためのハッシュ値を生成
- **主な機能:**
  - `Calculate(Material material): string`
    - マテリアルの全プロパティからMD5ハッシュを生成
    - シェーダー、色、テクスチャ、キーワードなど全要素を考慮
  
- **ハッシュ化対象:**
  - シェーダー情報（名前、タイプ）
  - マテリアルプロパティ（Color、Vector、Float、Int、Texture）
  - テクスチャのオフセット・スケール設定
  - シェーダーキーワード（ソート済み）

- **利用例:**
  ```csharp
  Material mat1 = AssetDatabase.LoadAssetAtPath<Material>("path/to/material1.mat");
  Material mat2 = AssetDatabase.LoadAssetAtPath<Material>("path/to/material2.mat");
  
  string hash1 = MaterialHashCalculator.Calculate(mat1);
  string hash2 = MaterialHashCalculator.Calculate(mat2);
  
  if (hash1 == hash2)
  {
      Debug.Log("Materials are identical");
  }
  ```

---

## 最適化アルゴリズムの詳細

### 1. Variantプレハブ検証
```csharp
private static bool ValidateInput(GameObject targetObject, out GameObject parentPrefab)
{
    // プレハブインスタンスかチェック
    // 対応する親プレハブの存在確認
    // Variantの妥当性検証
}
```

### 2. 再帰的マテリアル処理
```csharp
private static bool ProcessMaterialsRecursive(GameObject variant, GameObject parent)
{
    // 現在オブジェクトのRenderer処理
    // 子オブジェクトへの再帰処理
    // 変更フラグの管理
}
```

### 3. マテリアル最適化判定
```csharp
private static bool TryOptimizeMaterial(Material variantMaterial, Material parentMaterial, out Material optimizedMaterial)
{
    // ハッシュ値による同等性判定
    // 最適化可能な場合は親マテリアルを返す
    // 不可能な場合は元のマテリアルを保持
}
```

---

## パフォーマンス最適化のポイント

### ハッシュ計算の効率化
- **MD5ハッシュ使用:** 高速で衝突確率の低いハッシュアルゴリズム
- **プロパティ順序:** 一貫した順序でプロパティを処理
- **キーワードソート:** 順序に依存しない比較のためのソート

### メモリ管理
- **一時オブジェクト最小化:** StringBuilder使用でGC負荷軽減
- **リフレクション最適化:** 必要最小限のリフレクション使用
- **例外処理:** 存在しないプロパティへの安全なアクセス

---

## 拡張・運用のポイント

### カスタムシェーダー対応
```csharp
// 新しいプロパティタイプの追加例
case UnityEngine.Rendering.ShaderPropertyType.CustomType:
    var customValue = material.GetCustomValue(propName);
    hashBuilder.Append($"{propName}:custom{customValue};");
    break;
```

### デバッグ機能の追加
```csharp
// ハッシュ計算の詳細ログ
#if UNITY_EDITOR && DEBUG_MATERIAL_HASH
private static void LogHashDetails(Material material, string hash)
{
    Debug.Log($"Material: {material.name}, Hash: {hash}");
}
#endif
```

### エラーハンドリングの強化
- プレハブが見つからない場合の適切な処理
- 破損したマテリアルへの対処
- ネストしたVariantの処理改善

---

## トラブルシューティング

### よくある問題と解決策

1. **最適化が適用されない**
   - プレハブがVariantかどうか確認
   - 親プレハブの存在チェック
   - マテリアルの実際の差異確認

2. **ハッシュ値が一致しない**
   - テクスチャのオフセット・スケール確認
   - シェーダーキーワードの状態確認
   - 小数点精度の問題（F6フォーマット使用）

3. **プレハブオーバーライドエラー**
   - プレハブの編集権限確認
   - ネストしたプレハブの構造確認
   - Undoシステムとの競合回避

---

## まとめ
- `MaterialHelper.cs`はAutoVariantの最適化エンジンの中核
- マテリアルの詳細比較により効率的な最適化を実現
- 拡張性を考慮した設計でカスタムシェーダーにも対応可能
- パフォーマンスとメモリ効率を重視した実装
- デバッグとトラブルシューティング機能も充実