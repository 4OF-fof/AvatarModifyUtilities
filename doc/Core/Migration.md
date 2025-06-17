# 移行ガイド

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

## 移行チェックリスト

### コード変更
- [ ] using文の更新
- [ ] クラス名の更新
- [ ] メソッド名の更新（該当する場合）
- [ ] パラメータの確認

### 動作確認
- [ ] コンパイルエラーの解消
- [ ] 警告メッセージの確認
- [ ] 実行時動作の確認
- [ ] UI表示の確認

### ドキュメント更新
- [ ] 内部ドキュメントの更新
- [ ] コメントの更新
- [ ] README等の更新

## よくある問題と解決策

### 1. 名前空間の競合

**問題:**
```csharp
using AMU.Data.Lang;
using AMU.Editor.Core.Controllers;

// LocalizationManager と LocalizationController の競合
```

**解決策:**
```csharp
using AMU.Editor.Core.Controllers;

// 旧名前空間は削除し、新しいControllerのみ使用
var text = LocalizationController.GetText("key");
```

### 2. Obsolete警告

**問題:**
```
CS0618: 'ObjectCaptureHelper.CaptureObject(GameObject, string, int, int)' is obsolete: 'Use AMU.Editor.Core.API.ObjectCaptureAPI.CaptureObject instead'
```

**解決策:**
- 警告メッセージに従って新しいAPIに移行
- 一時的に警告を抑制する場合: `#pragma warning disable CS0618`

### 3. 初期化タイミング

**問題:**
- 手動初期化が動作しない
- 初期化順序の問題

**解決策:**
```csharp
// 個別コンポーネントの再初期化
InitializationService.Reinitialize(InitializationComponent.Localization);

// 全体の再初期化
InitializationService.Reinitialize(InitializationComponent.All);
```

## 段階的移行の推奨手順

### 1. 新規開発
- 必ず新APIを使用
- 旧APIは使用しない

### 2. 機能追加・修正時
- 関連する既存コードも新APIに移行
- 影響範囲を考慮した段階的移行

### 3. 定期的なリファクタリング
- プロジェクト全体のObsolete警告を定期的にチェック
- 優先度の高い箇所から順次移行

### 4. 移行完了の確認
- 全てのObsolete警告の解消
- 新アーキテクチャでの動作確認
- パフォーマンステストの実施
