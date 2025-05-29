# AutoVariant/Watcher ディレクトリの内容と活用方法

## 概要
`AutoVariant/Watcher` ディレクトリには、AutoVariant機能の自動監視・処理システムが格納されています。プレハブの追加を監視してVariantを自動作成する機能と、VRChatアバターのビルド前に最適化処理を実行する機能を提供します。

---

## 各ファイルの説明

### ConvertVariant.cs
- **役割:** プレハブがシーンに追加された際の自動Variant作成を監視・実行するクラス
- **主要クラス:** `PrefabAdditionDetector`（static class）

#### 主な機能
- **自動監視システム:**
  - `EditorApplication.hierarchyChanged`イベントによる監視
  - プレハブ追加の検出と自動処理
  - 重複処理防止機能

- **Variant自動作成プロセス:**
  1. シーンヒエラルキーの変更監視
  2. 新規追加されたプレハブの検出
  3. マテリアルの`Assets/Untitled_Variants/Material/`へのコピー
  4. Variantプレハブの`Assets/Untitled_Variants/`への作成
  5. シーン内オブジェクトのVariantへの置き換え

- **利用例（設定による制御）:**
  ```csharp
  // 自動Variant作成の有効化
  EditorPrefs.SetBool("Setting.AutoVariant_enableAutoVariant", true);
  
  // 無効化（手動Variant作成モード）
  EditorPrefs.SetBool("Setting.AutoVariant_enableAutoVariant", false);
  ```

#### 処理フローの詳細

1. **監視の初期化**
   ```csharp
   static PrefabAdditionDetector()
   {
       if (!EditorPrefs.GetBool("Setting.AutoVariant_enableAutoVariant", false)) return;
       EditorApplication.hierarchyChanged += OnHierarchyChanged;
   }
   ```

2. **プレハブ検出ロジック**
   ```csharp
   static System.Collections.Generic.List<GameObject> FindAddedPrefabRoots()
   {
       // プレハブステージでの処理をスキップ
       // 各GameObjectのプレハブ状態をチェック
       // Untitledプレハブの除外
       // 新規追加されたプレハブのリストを返す
   }
   ```

3. **マテリアル処理**
   ```csharp
   static void CopyAndReplaceMaterials(GameObject go, string materialDir)
   {
       // Rendererコンポーネントの走査
       // マテリアルのコピー作成
       // 既存マテリアルとの置き換え
       // プレハブオーバーライドの記録
   }
   ```

### Prebuild.cs
- **役割:** VRChatアバターのビルド前に最適化とエクスポートを自動実行するクラス
- **主要クラス:** `MyPreBuildProcess`（IVRCSDKBuildRequestedCallback実装）

#### 主な機能
- **ビルド前自動処理:**
  - VRChat SDKのビルドパイプラインとの統合
  - マテリアル最適化の自動実行
  - アバターの自動エクスポート
  - 処理後のマテリアル状態復元

- **エクスポート機能:**
  - BlueprintID別のディレクトリ管理
  - 日付ベースのファイル命名
  - 設定に応じたアセット包含範囲の制御
  - UnityPackage形式での自動保存

#### 処理フローの詳細

1. **ビルド前処理の開始**
   ```csharp
   public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
   {
       if (EditorPrefs.GetBool("Setting.AutoVariant_enablePrebuild", true))
       {
           OptimizeMaterials();
       }
       return true;
   }
   ```

2. **マテリアル最適化**
   ```csharp
   private void OptimizeMaterials()
   {
       // VRCアバターの検出
       // マテリアル状態の保存
       // MaterialVariantOptimizerによる最適化
       // ネストしたプレハブの再帰処理
       // エクスポート処理の実行
       // 元の状態への復元
   }
   ```

3. **エクスポート処理**
   ```csharp
   private void ExportOptimizedAvatar(GameObject avatar)
   {
       // BlueprintIDの取得
       // 保存ディレクトリの作成
       // 一意なファイル名の生成
       // アセット収集と包含範囲の決定
       // UnityPackageとしてエクスポート
   }
   ```

---

## 設定による動作制御

### 自動Variant作成（ConvertVariant.cs）
- **設定キー:** `Setting.AutoVariant_enableAutoVariant`
- **デフォルト値:** `false`
- **影響範囲:**
  - 有効時：プレハブ追加の自動監視とVariant作成
  - 無効時：手動でのVariant作成が必要

### ビルド前保存（Prebuild.cs）
- **設定キー:** `Setting.AutoVariant_enablePrebuild`
- **デフォルト値:** `true`
- **影響範囲:**
  - 有効時：ビルド前の自動最適化とエクスポート
  - 無効時：ビルド前処理をスキップ

### アセット包含範囲（Prebuild.cs）
- **設定キー:** `Setting.AutoVariant_includeAllAssets`
- **デフォルト値:** `true`
- **影響範囲:**
  - `true`：全依存関係を含む完全なパッケージ
  - `false`：Untitled_Variants配下のみの軽量パッケージ

---

## ディレクトリ構造と命名規則

### Variantファイルの配置
```
Assets/
├── Untitled_Variants/
│   ├── Material/           # コピーされたマテリアル
│   ├── Untitled_*.prefab   # 作成されたVariantプレハブ
│   └── ...
```

### エクスポートファイルの配置
```
{Core_dirPath}/
└── AutoVariant/
    ├── {blueprintId}/      # アバター固有のディレクトリ
    │   └── YYMMDD-001.unitypackage
    └── local/              # BlueprintIDなしのローカルアバター
        └── YYMMDD-{avatarName}-001.unitypackage
```

---

## パフォーマンスとメモリ最適化

### 重複処理防止
```csharp
static System.Collections.Generic.HashSet<int> recentlyHandled = new System.Collections.Generic.HashSet<int>();
static double lastClearTime = 0;
static double clearInterval = 1.0;
```

### マテリアル状態の管理
```csharp
[System.Serializable]
private class RendererMaterialState
{
    public Renderer renderer;
    public Material[] originalMaterials;
    
    public void RestoreMaterials()
    {
        if (renderer != null)
        {
            renderer.sharedMaterials = originalMaterials;
        }
    }
}
```

---

## トラブルシューティング

### ConvertVariant.cs関連

1. **Variantが自動作成されない**
   - `Setting.AutoVariant_enableAutoVariant`の設定確認
   - プレハブステージでの作業でないかチェック
   - 既にUntitledプレハブでないかの確認

2. **マテリアルがコピーされない**
   - `Assets/Untitled_Variants/`ディレクトリの権限確認
   - 元マテリアルの存在とパス確認
   - プレハブのRenderer構成確認

### Prebuild.cs関連

1. **ビルド前処理が実行されない**
   - `Setting.AutoVariant_enablePrebuild`の設定確認
   - VRC SDK Buildパイプラインとの統合確認
   - PipelineManagerコンポーネントの存在確認

2. **エクスポートファイルが作成されない**
   - 保存ディレクトリの書き込み権限確認
   - アセットの依存関係確認
   - BlueprintIDの妥当性確認

---

## 拡張・カスタマイズのポイント

### 監視対象の拡張
```csharp
// 特定のプレハブタイプのみを監視する場合
static bool ShouldProcessPrefab(GameObject go)
{
    // カスタム条件を追加
    return go.name.Contains("Avatar") && !IsUntitled(go, prefabAsset);
}
```

### エクスポート形式の拡張
```csharp
// 追加のエクスポート形式
private void ExportAsJSON(GameObject avatar, string path)
{
    // JSONシリアライゼーション
    // メタデータの追加
    // 圧縮オプションの適用
}
```

### ビルドパイプライン統合の拡張
```csharp
// 追加のビルド前処理
public class CustomPreBuildProcess : IVRCSDKBuildRequestedCallback
{
    public int callbackOrder => -1; // 実行順序の制御
    
    public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
    {
        // カスタム前処理
        return true;
    }
}
```

---

## まとめ
- `Watcher`ディレクトリは自動化システムの中核
- `ConvertVariant.cs`でプレハブ追加の監視とVariant作成を自動化
- `Prebuild.cs`でビルド前の最適化とエクスポートを自動化
- 設定による詳細な動作制御が可能
- 効率的な監視システムとメモリ管理を実装
- VRC SDKとの統合によりシームレスなワークフローを提供
- 拡張性を考慮した設計でカスタマイズが容易