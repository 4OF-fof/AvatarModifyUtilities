# 移行ガイド

## 最新の変更履歴

### 2025年6月17日: ローカライゼーション機能の改善

**変更内容:**
- `LocalizationController`にフォールバック機能を追加
- 翻訳がないキーについて、キーの代わりに英語テキストを返すように改善
- `GetFallbackTextCount()`メソッドを追加

**影響:**
- 既存のコードには互換性があり、変更不要
- 翻訳がない部分のユーザビリティが向上
- より一貫性のあるローカライゼーション体験を提供

**新機能:**
```csharp
// フォールバックテキスト数の取得
var fallbackCount = LocalizationController.GetFallbackTextCount();

// 翻訳がない場合の動作例
LocalizationController.LoadLanguage("ja_jp");
var text = LocalizationController.GetText("untranslated_key");
// → 日本語がなければ英語テキストを返す（キーではなく）
```

## 概要

このドキュメントは、旧Helperベースの構造から新しい3層アーキテクチャへの移行手順を説明します。段階的な移行により、既存コードの動作を保ちながら新しいアーキテクチャのメリットを享受できます。

## 移行フェーズ

### Phase 1: 後方互換性期間（現在）

**状況:**
- 旧APIと新APIが両方とも利用可能
- 旧API使用時にObsolete警告が表示
- 既存コードは変更不要で動作

**推奨アクション:**
- 新規開発では新APIを使用
- 既存コードの段階的移行

### Phase 2: 移行推進期間（将来）

**状況:**
- Obsolete警告がより厳しくなる
- 一部の旧APIで制限事項が発生する可能性
- 新機能は新APIでのみ提供

**推奨アクション:**
- 積極的な移行作業
- コードレビューでの移行チェック

### Phase 3: 新アーキテクチャ完全移行（将来）

**状況:**
- 後方互換性エイリアスの削除
- 新APIのみが利用可能

## 具体的な移行手順

### 1. ObjectCaptureHelper → ObjectCaptureAPI

#### 変更前
```csharp
using AMU.Editor.Core.Helper;

public class AvatarExporter
{
    private void CaptureAvatar(GameObject avatar)
    {
        var texture = ObjectCaptureHelper.CaptureObject(avatar, imagePath, 512, 512);
        if (texture != null)
        {
            // 処理継続
        }
    }
}
```

#### 変更後
```csharp
using AMU.Editor.Core.API;

public class AvatarExporter
{
    private void CaptureAvatar(GameObject avatar)
    {
        var texture = ObjectCaptureAPI.CaptureObject(avatar, imagePath, 512, 512);
        if (texture != null)
        {
            // 処理継続
        }
    }
}
```

#### 変更点
- 名前空間: `AMU.Editor.Core.Helper` → `AMU.Editor.Core.API`
- クラス名: `ObjectCaptureHelper` → `ObjectCaptureAPI`
- メソッド名: 変更なし

### 2. PipelineManagerHelper → VRChatAPI

#### 変更前
```csharp
using AMU.Editor.Core.Helper;

public class ConvertVariant
{
    private void ProcessAvatar(GameObject go)
    {
        var blueprintId = PipelineManagerHelper.GetBlueprintId(go);
        if (PipelineManagerHelper.isVRCAvatar(go))
        {
            // VRCアバター処理
        }
    }
}
```

#### 変更後
```csharp
using AMU.Editor.Core.API;

public class ConvertVariant
{
    private void ProcessAvatar(GameObject go)
    {
        var blueprintId = VRChatAPI.GetBlueprintId(go);
        if (VRChatAPI.IsVRCAvatar(go))
        {
            // VRCアバター処理
        }
    }
}
```

#### 変更点
- 名前空間: `AMU.Editor.Core.Helper` → `AMU.Editor.Core.API`
- クラス名: `PipelineManagerHelper` → `VRChatAPI`
- メソッド名: `isVRCAvatar` → `IsVRCAvatar` (Pascal記法)

### 3. LocalizationManager → LocalizationController

#### 変更前
```csharp
using AMU.Data.Lang;

public class SettingWindow
{
    private void OnGUI()
    {
        LocalizationManager.LoadLanguage(lang);
        var text = LocalizationManager.GetText("ui_button_save");
        // UI描画
    }
}
```

#### 変更後
```csharp
using AMU.Editor.Core.Controllers;

public class SettingWindow
{
    private void OnGUI()
    {
        LocalizationController.LoadLanguage(lang);
        var text = LocalizationController.GetText("ui_button_save");
        // UI描画
    }
}
```

#### 変更点
- 名前空間: `AMU.Data.Lang` → `AMU.Editor.Core.Controllers`
- クラス名: `LocalizationManager` → `LocalizationController`
- メソッド名: 変更なし

### 4. AMUInitializer → InitializationService

#### 変更前
```csharp
using AMU.Editor.Initializer;

// 手動初期化（通常は不要）
public class CustomInitializer
{
    [InitializeOnLoad]
    static CustomInitializer()
    {
        AMUInitializer.Initialize();
    }
}
```

#### 変更後
```csharp
using AMU.Editor.Core.Services;

// 手動初期化（通常は不要）
public class CustomInitializer
{
    [InitializeOnLoad]
    static CustomInitializer()
    {
        // 通常は自動実行されるため、手動呼び出し不要
        // 必要に応じて個別コンポーネントの再初期化
        InitializationService.Reinitialize(InitializationComponent.Localization);
    }
}
```

#### 変更点
- 名前空間: `AMU.Editor.Initializer` → `AMU.Editor.Core.Services`
- クラス名: `AMUInitializer` → `InitializationService`
- 初期化は自動実行されるため、通常は手動呼び出し不要

## 新しい機能の活用

### 設定管理の改善

#### 変更前（EditorPrefs直接使用）
```csharp
public class MyFeature
{
    private void SaveSettings()
    {
        EditorPrefs.SetString("MyFeature.Language", "ja_jp");
        EditorPrefs.SetInt("MyFeature.MaxItems", 100);
        EditorPrefs.SetBool("MyFeature.Enabled", true);
    }
    
    private void LoadSettings()
    {
        var language = EditorPrefs.GetString("MyFeature.Language", "ja_jp");
        var maxItems = EditorPrefs.GetInt("MyFeature.MaxItems", 100);
        var enabled = EditorPrefs.GetBool("MyFeature.Enabled", true);
    }
}
```

#### 変更後（SettingsController使用）
```csharp
using AMU.Editor.Core.Controllers;

public class MyFeature
{
    private void SaveSettings()
    {
        SettingsController.SetSetting("MyFeature.Language", "ja_jp");
        SettingsController.SetSetting("MyFeature.MaxItems", 100);
        SettingsController.SetSetting("MyFeature.Enabled", true);
    }
    
    private void LoadSettings()
    {
        var language = SettingsController.GetSetting<string>("MyFeature.Language", "ja_jp");
        var maxItems = SettingsController.GetSetting<int>("MyFeature.MaxItems", 100);
        var enabled = SettingsController.GetSetting<bool>("MyFeature.Enabled", true);
    }
}
```

#### メリット
- 型安全性の確保
- 一貫したキー管理
- エラーハンドリングの統一

## Core/UI モジュールのリファクタリング（2025年6月）

### 変更概要

Core/UIモジュールは2025年6月のリファクタリングで新しい層構造に完全対応しました。

#### 主な変更点

1. **名前空間の追加**
   ```csharp
   // 旧
   public class SettingWindow : EditorWindow
   
   // 新
   namespace AMU.Editor.Core.UI
   {
       public class SettingWindow : EditorWindow
   }
   ```

2. **Controllers層の活用**
   ```csharp
   // 旧（直接EditorPrefs操作）
   string lang = EditorPrefs.GetString("Setting.Core_language", "en_us");
   EditorPrefs.SetString(key, newValue);
   
   // 新（SettingsController使用）
   string lang = SettingsController.GetSetting("Core_language", "en_us");
   SettingsController.SetSetting(item.Name, newValue);
   ```

3. **ローカライゼーションAPIの更新**
   ```csharp
   // 旧（後方互換性API）
   LocalizationManager.LoadLanguage(lang);
   LocalizationManager.GetText(key);
   
   // 新（新しいController）
   LocalizationController.LoadLanguage(lang);
   LocalizationController.GetText(key);
   ```

4. **重複コードの削除**
   - 設定項目取得ロジックをSettingsControllerに統合
   - 初期値設定ロジックをSettingsControllerに委譲
   - SettingDataHelperクラスの削除

#### 削除された機能

1. **InitializeDefaultValues()メソッド**
   - SettingsController.InitializeEditorPrefs()で代替

2. **SetDefaultValue()メソッド**
   - SettingsControllerのSetDefaultValue()で代替

3. **SettingDataHelperクラス**
   - SettingsController.GetAllSettingItems()で代替

#### 移行の影響

- **既存UIコード**: 自動的に新しい層構造を活用
- **パフォーマンス**: 重複処理の削除により改善
- **保守性**: 責務の明確化により向上
- **拡張性**: 新しい設定項目の追加が容易に

### UI開発者向けの移行ガイド

#### 新しいUIコンポーネント開発時

```csharp
using AMU.Editor.Core.UI;
using AMU.Editor.Core.Controllers;

namespace AMU.Editor.Core.UI
{
    public class MyCustomWindow : EditorWindow
    {
        private void Initialize()
        {
            // 設定の取得
            var setting = SettingsController.GetSetting("MySetting", "defaultValue");
            
            // ローカライゼーション
            var text = LocalizationController.GetText("my_text_key");
        }
    }
}
```

#### 注意事項

- UIコンポーネントは**UI専用の責務**に集中する
- データ管理は**Controllers層**に委譲する
- 外部公開機能（API層）は使用しない
- 新しい名前空間 `AMU.Editor.Core.UI` を使用する

## 既存コードの移行手順

### 名前空間の更新

1. **Helper → API層**:
   - `AMU.Editor.Core.Helper` → `AMU.Editor.Core.API`

2. **Initializer → Services層**:
   - `AMU.Editor.Initializer` → `AMU.Editor.Core.Services`

### クラス名の更新

- `ObjectCaptureHelper` → `ObjectCaptureAPI`
- `PipelineManagerHelper` → `VRChatAPI`
- `AMUInitializer` → `InitializationService`

### メソッド名の更新

- `isVRCAvatar()` → `IsVRCAvatar()`

### 移行フェーズ

1. **Phase 1**: 後方互換性エイリアスを使用（現在）
2. **Phase 2**: Obsolete警告を確認し、新しいAPIに移行
3. **Phase 3**: 後方互換性エイリアスの削除（将来）

### 後方互換性の例

既存のコードは引き続き動作しますが、新しいAPIの使用が推奨されます：

```csharp
// 古い方法（非推奨だが動作する）
using AMU.Editor.Core.Helper;
var texture = ObjectCaptureHelper.CaptureObject(gameObject, "path.png");

// 新しい方法（推奨）
using AMU.Editor.Core.API;
var texture = ObjectCaptureAPI.CaptureObject(gameObject, "path.png");
```
