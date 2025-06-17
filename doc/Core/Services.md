# Services 層ドキュメント

## 概要

Services層は、システムの初期化処理とサービス機能を提供します。アプリケーションのライフサイクル管理、後方互換性の提供、システム全体の協調動作を担当します。

## 設計原則

- **自動化された初期化**
- **依存関係の管理**
- **エラー時の安全性**
- **後方互換性の保証**

## Services一覧

### InitializationService

#### 概要
AMU全体の初期化処理を管理する中核的なサービスです。Unityエディタ起動時に自動実行され、各コンポーネントの初期化を協調的に行います。

#### 名前空間
```csharp
using AMU.Editor.Core.Services;
```

#### 自動初期化

`[InitializeOnLoad]` 属性により、Unityエディタ起動時に自動的に実行されます。

```csharp
[InitializeOnLoad]
public static class InitializationService
{
    static InitializationService()
    {
        // エディター起動時の初期化
        EditorApplication.delayCall += Initialize;
    }
}
```

#### 主要機能

##### 全体初期化
```csharp
public static void Initialize()
```

全てのコンポーネントの初期化を順次実行します。

**初期化順序:**
1. EditorPrefs設定の初期化
2. TagTypeManagerの初期化
3. ローカライゼーションの初期化

**使用例:**
```csharp
// 通常は自動実行されるが、手動での再初期化も可能
InitializationService.Initialize();
```

##### 個別コンポーネントの再初期化
```csharp
public static void Reinitialize(InitializationComponent component)
```

**パラメータ:**
- `component`: 再初期化するコンポーネント

**InitializationComponent 列挙型:**
```csharp
public enum InitializationComponent
{
    EditorPrefs,      // 設定の再初期化
    TagTypeManager,   // TagTypeManagerの再初期化
    Localization,     // ローカライゼーションの再初期化
    All              // 全体の再初期化
}
```

**使用例:**
```csharp
// 特定のコンポーネントのみ再初期化
InitializationService.Reinitialize(InitializationComponent.Localization);

// 全体の再初期化
InitializationService.Reinitialize(InitializationComponent.All);
```

#### 初期化詳細

##### EditorPrefs初期化
```csharp
private static void InitializeEditorPrefs()
{
    SettingsController.InitializeEditorPrefs();
}
```

- 設定項目の初期値設定
- 既存設定の保持
- エラー時のログ出力

##### TagTypeManager初期化
```csharp
private static void InitializeTagTypeManager()
{
    try
    {
        TagTypeManager.LoadData();
        Debug.Log("[InitializationService] TagTypeManager initialized successfully.");
    }
    catch (System.Exception ex)
    {
        Debug.LogError($"[InitializationService] TagTypeManager initialization failed: {ex.Message}");
    }
}
```

##### ローカライゼーション初期化
```csharp
private static void InitializeLocalization()
{
    try
    {
        LocalizationController.LoadLanguage("ja_jp");
        Debug.Log("[InitializationService] Localization initialized successfully.");
    }
    catch (System.Exception ex)
    {
        Debug.LogError($"[InitializationService] Localization initialization failed: {ex.Message}");
    }
}
```

### BackwardCompatibilityAliases

#### 概要
既存コードとの後方互換性を提供するエイリアス集です。旧API名から新API名への橋渡しを行い、段階的な移行を可能にします。

#### 名前空間
複数の名前空間にまたがってエイリアスを提供します。

#### 提供エイリアス

##### ObjectCaptureHelper
```csharp
namespace AMU.Editor.Core.Helper
{
    [System.Obsolete("Use AMU.Editor.Core.API.ObjectCaptureAPI instead", false)]
    public static class ObjectCaptureHelper
    {
        [System.Obsolete("Use AMU.Editor.Core.API.ObjectCaptureAPI.CaptureObject instead", false)]
        public static Texture2D CaptureObject(GameObject targetObject, string savePath, int width = 512, int height = 512)
        {
            return ObjectCaptureAPI.CaptureObject(targetObject, savePath, width, height);
        }
    }
}
```

**移行パス:**
```csharp
// 旧コード（非推奨警告が表示される）
using AMU.Editor.Core.Helper;
var texture = ObjectCaptureHelper.CaptureObject(obj, path);

// 新コード（推奨）
using AMU.Editor.Core.API;
var texture = ObjectCaptureAPI.CaptureObject(obj, path);
```

##### PipelineManagerHelper
```csharp
namespace AMU.Editor.Core.Helper
{
    [System.Obsolete("Use AMU.Editor.Core.API.VRChatAPI instead", false)]
    public static class PipelineManagerHelper
    {
        [System.Obsolete("Use AMU.Editor.Core.API.VRChatAPI.GetBlueprintId instead", false)]
        public static string GetBlueprintId(GameObject go)
        {
            return VRChatAPI.GetBlueprintId(go);
        }

        [System.Obsolete("Use AMU.Editor.Core.API.VRChatAPI.IsVRCAvatar instead", false)]
        public static bool isVRCAvatar(GameObject obj)
        {
            return VRChatAPI.IsVRCAvatar(obj);
        }
    }
}
```

**移行パス:**
```csharp
// 旧コード（非推奨警告が表示される）
using AMU.Editor.Core.Helper;
var blueprintId = PipelineManagerHelper.GetBlueprintId(avatar);
bool isVRC = PipelineManagerHelper.isVRCAvatar(obj);

// 新コード（推奨）
using AMU.Editor.Core.API;
var blueprintId = VRChatAPI.GetBlueprintId(avatar);
bool isVRC = VRChatAPI.IsVRCAvatar(obj);
```

##### AMUInitializer
```csharp
namespace AMU.Editor.Initializer
{
    [System.Obsolete("Use AMU.Editor.Core.Services.InitializationService instead", false)]
    public static class AMUInitializer
    {
        [System.Obsolete("Initialization is now handled automatically by InitializationService", false)]
        public static void Initialize()
        {
            InitializationService.Initialize();
        }
    }
}
```

**移行パス:**
```csharp
// 旧コード（非推奨警告が表示される）
using AMU.Editor.Initializer;
AMUInitializer.Initialize();

// 新コード（通常は自動実行されるため手動呼び出し不要）
using AMU.Editor.Core.Services;
InitializationService.Initialize(); // 必要に応じて
```

## エラーハンドリング

### 初期化エラー

```csharp
try
{
    Debug.Log("[InitializationService] Starting AMU initialization...");
    
    InitializeEditorPrefs();
    InitializeTagTypeManager();
    InitializeLocalization();
    
    Debug.Log("[InitializationService] AMU initialization completed successfully.");
}
catch (System.Exception ex)
{
    Debug.LogError($"[InitializationService] AMU initialization failed: {ex.Message}");
}
```

### 部分的な失敗への対応

各コンポーネントの初期化は独立しており、一部の失敗が全体に影響しないように設計されています。

```csharp
// TagTypeManagerの初期化が失敗しても、他の初期化は継続される
private static void InitializeTagTypeManager()
{
    try
    {
        TagTypeManager.LoadData();
        Debug.Log("[InitializationService] TagTypeManager initialized successfully.");
    }
    catch (System.Exception ex)
    {
        Debug.LogError($"[InitializationService] TagTypeManager initialization failed: {ex.Message}");
        // 他の初期化処理は継続
    }
}
```

## 依存関係

### 初期化順序の重要性

1. **EditorPrefs** → 他のコンポーネントの設定に必要
2. **TagTypeManager** → データ構造の基盤
3. **Localization** → UI表示に必要

### 循環依存の回避

各サービスは以下のルールに従って依存関係を管理します：

- **Services** → **Controllers** → **Data**
- **API** → **Controllers**
- **UI** → **API** + **Controllers**

## 拡張ガイド

### 新しい初期化処理の追加

1. **メソッド追加**: `InitializationService` に新しい初期化メソッドを追加
2. **順序考慮**: 依存関係を考慮した初期化順序
3. **エラーハンドリング**: 失敗時の適切な処理
4. **ログ出力**: 成功/失敗の適切なログ
5. **再初期化対応**: `Reinitialize` メソッドへの対応

### 後方互換性エイリアスの追加

1. **非推奨マーク**: `[System.Obsolete]` 属性の使用
2. **適切なメッセージ**: 移行先の明確な指示
3. **機能維持**: 既存の動作を完全に保持
4. **段階的廃止**: 将来的な削除計画

### サービスの拡張

1. **独立性**: 他のサービスへの依存を最小化
2. **テスタビリティ**: 単体テストが可能な設計
3. **設定可能性**: 動作をカスタマイズ可能に
4. **モニタリング**: 適切なログとメトリクス
