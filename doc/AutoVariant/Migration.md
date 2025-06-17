# Migration ガイド - AutoVariant

## 概要

このドキュメントは、AutoVariantモジュールのリファクタリング（旧Helper/Watcher構造から新しい2層アーキテクチャへ）による変更点と移行方法を説明します。

## 重要な変更点

### API層の削除
- **API層全体が削除されました**
- AutoVariantは内部処理に特化し、外部公開APIが不要と判断
- MaterialVariantAPIとAvatarExportAPIはServices層に統合

### Controllers層とSchema層の削除
- **AutoVariantController.cs が削除されました**
- **PrebuildSettings.cs が削除されました**
- 設定管理は Core.Controllers.SettingsController に完全統合
- 設定の初期化は Core システムで自動実行
- 冗長なラッパークラスを排除し、直接的な設定アクセスに統一

## リファクタリング前後の対応表

### ディレクトリ構造の変更

| 旧構造 | 新構造 | 変更理由 |
|--------|--------|----------|
| `Helper/MaterialHelper.cs` | `Services/MaterialVariantService.cs` | 内部サービスとして整理 |
| `Watcher/AvatarExporter.cs` | `Services/AvatarExportService.cs` | 内部サービスとして整理 |
| `Controllers/AutoVariantController.cs` | **削除** | **設定管理はCoreに統合** |
| `Watcher/ConvertVariant.cs` | `Services/ConvertVariantService.cs` | サービス層として整理 |
| `Watcher/MaterialOptimizationManager.cs` | `Services/MaterialOptimizationService.cs` | サービス層として整理 |
| `Watcher/AvatarValidator.cs` | `Services/AvatarValidationService.cs` | サービス層として整理 |
| `Watcher/Prebuild.cs` | `Services/PrebuildService.cs` | サービス層として整理 |
| `Schema/PrebuildSettings.cs` | **削除** | **設定管理はCoreに統合** |
| `Data/Setting.cs` | `Data/Setting.cs` | 変更なし |

### クラス名とメソッドの変更

#### MaterialHelper → MaterialVariantService
| 旧メソッド | 新メソッド | 変更点 |
|------------|------------|--------|
| `MaterialVariantOptimizer.OptimizeMaterials()` | `MaterialVariantService.OptimizeMaterials()` | クラス名変更、機能は同等 |
| `MaterialHashCalculator.Calculate()` | `MaterialVariantService.MaterialHashCalculator.Calculate()` | ネストクラスとして整理 |

#### AvatarExporter → AvatarExportService
| 旧メソッド | 新メソッド | 変更点 |
|------------|------------|--------|
| `AvatarExporter.ExportOptimizedAvatar()` | `AvatarExportService.ExportOptimizedAvatar()` | 戻り値がboolに変更 |
| なし | `AvatarExportService.GetAvatarAssets()` | 新規追加：アセット一覧取得 |

#### AvatarValidator → AvatarValidationService
| 旧メソッド | 新メソッド | 変更点 |
|------------|------------|--------|
| `AvatarValidator.ValidateAvatarCount()` | `AvatarValidationService.ValidateAvatarCount()` | 機能は同等 |
| `AvatarValidator.FindActiveAvatars()` | `AvatarValidationService.FindActiveAvatars()` | 機能は同等 |
| なし | `AvatarValidationService.GetSingleActiveAvatar()` | 新規追加 |
| なし | `AvatarValidationService.IsVRCAvatar()` | 新規追加 |

#### MaterialOptimizationManager → MaterialOptimizationService
| 旧メソッド | 新メソッド | 変更点 |
|------------|------------|--------|
| `MaterialOptimizationManager.OptimizeActiveAvatars()` | `MaterialOptimizationService.OptimizeActiveAvatars()` | 機能は同等 |
| なし | `MaterialOptimizationService.OptimizeAvatar()` | 新規追加：個別アバター最適化 |

#### ConvertVariant → ConvertVariantService
| 旧クラス | 新クラス | 変更点 |
|----------|----------|--------|
| `PrefabAdditionDetector` | `ConvertVariantService` | 名前変更、機能は同等 |
| なし | `ConvertVariantService.Initialize()` | 新規追加：明示的初期化 |
| なし | `ConvertVariantService.Shutdown()` | 新規追加：明示的終了処理 |

#### MyPreBuildProcess → PrebuildService
| 旧クラス | 新クラス | 変更点 |
|----------|----------|--------|
| `MyPreBuildProcess` | `PrebuildService` | 名前変更、機能は同等 |

## 名前空間の変更

### 新しい名前空間構造
```csharp
// API層
namespace AMU.Editor.AutoVariant.Api

// Controllers層
namespace AMU.Editor.AutoVariant.Controllers

// Services層
namespace AMU.Editor.AutoVariant.Services

// Schema層
namespace AMU.Editor.AutoVariant.Schema

// Data層（既存）
namespace AMU.Editor.Setting
```

### 旧名前空間からの移行
```csharp
// 旧
using AMU.Editor.AutoVariant.Watcher;

// 新
using AMU.Editor.AutoVariant.Api;
using AMU.Editor.AutoVariant.Services;
```

## コード移行例

### MaterialHelper の使用箇所

#### 移行前
```csharp
using AMU.Editor.AutoVariant.Watcher;

// 使用例
MaterialVariantOptimizer.OptimizeMaterials(avatar);
```

#### 移行後
```csharp
using AMU.Editor.AutoVariant.Services;

// 使用例
bool optimized = MaterialVariantService.OptimizeMaterials(avatar);
if (optimized)
{
    Debug.Log("最適化が完了しました");
}
```

### AvatarExporter の使用箇所

#### 移行前
```csharp
using AMU.Editor.AutoVariant.Watcher;

// 使用例
AvatarExporter.ExportOptimizedAvatar(avatar);
```

#### 移行後
```csharp
using AMU.Editor.AutoVariant.Services;

// 使用例
bool exported = AvatarExportService.ExportOptimizedAvatar(avatar);
if (!exported)
{
    Debug.LogError("エクスポートに失敗しました");
}
```

### 設定値アクセス

#### 移行前
```csharp
// EditorPrefsに直接アクセス
bool enabled = EditorPrefs.GetBool("Setting.AutoVariant_enableAutoVariant", false);

// AutoVariantControllerを使用
bool enabled = AutoVariantController.IsAutoVariantEnabled();

// PrebuildSettingsを使用
bool enabled = PrebuildSettings.IsAutoVariantEnabled;
```

#### 移行後
```csharp
using AMU.Editor.Core.Controllers;

// SettingsControllerを直接使用（推奨）
bool enabled = SettingsController.GetSetting<bool>("AutoVariant_enableAutoVariant", false);

// 設定値の変更
SettingsController.SetSetting("AutoVariant_enableAutoVariant", true);
```

### AutoVariantControllerとPrebuildSettingsの削除対応

#### 移行前
```csharp
using AMU.Editor.AutoVariant.Controllers;
using AMU.Editor.AutoVariant.Schema;

// AutoVariantControllerを使用
bool enabled = AutoVariantController.IsAutoVariantEnabled();
AutoVariantController.SetAutoVariantEnabled(true);
AutoVariantController.InitializeSettings();

// PrebuildSettingsを使用
bool optimization = PrebuildSettings.IsOptimizationEnabled;
string language = PrebuildSettings.CurrentLanguage;
```

#### 移行後
```csharp
using AMU.Editor.Core.Controllers;

// SettingsController経由での直接アクセス
bool enabled = SettingsController.GetSetting<bool>("AutoVariant_enableAutoVariant", false);
SettingsController.SetSetting("AutoVariant_enableAutoVariant", true);

// その他の設定項目も同様に
bool optimization = SettingsController.GetSetting<bool>("AutoVariant_enablePrebuild", true);
string language = SettingsController.GetSetting<string>("Core_language", "ja");

// 初期化はCore.Controllers.SettingsControllerが自動実行
// 手動初期化は不要
```

## 破壊的変更

### 1. API層の削除
- **API層全体が削除されました**
- `MaterialVariantAPI` → `MaterialVariantService`
- `AvatarExportAPI` → `AvatarExportService`
- 名前空間が `AMU.Editor.AutoVariant.Api` から `AMU.Editor.AutoVariant.Services` に変更

### 2. 戻り値の変更
- `AvatarExportService.ExportOptimizedAvatar()`: `void` → `bool`
- 成功/失敗を判定できるように変更

### 3. クラス名の変更
- `MaterialVariantOptimizer` → `MaterialVariantService`
- `AvatarExporter` → `AvatarExportService`
- `PrefabAdditionDetector` → `ConvertVariantService`
- `MyPreBuildProcess` → `PrebuildService`
- **`AutoVariantController` → 削除（Coreに統合）**
- **`PrebuildSettings` → 削除（Coreに統合）**

### 4. 名前空間の変更
- API層削除により、すべてのクラスがServices層に統合
- `using`文の更新が必要
- **API層、Controllers層、Schema層が削除**

### 5. メソッドアクセスの変更
- 一部のprivateメソッドがinternalに変更
- 一部のpublicメソッドが整理・統合
- **設定関連メソッドは Core.Controllers.SettingsController に移行**

## 新機能

### 1. 統一された設定管理
```csharp
using AMU.Editor.Core.Controllers;

// 自動初期化（手動初期化不要）
// SettingsController.InitializeEditorPrefs(); // 自動実行

// 統一された設定アクセス
bool enabled = SettingsController.GetSetting<bool>("AutoVariant_enableAutoVariant", false);
SettingsController.SetSetting("AutoVariant_enableAutoVariant", true);
}
```

### 2. 統一されたエラーハンドリング
```csharp
// API呼び出しの結果チェック
bool result = MaterialVariantAPI.OptimizeMaterials(avatar);
if (!result)
{
    Debug.LogError("最適化に失敗しました");
}
```

### 3. 型安全な設定アクセス
```csharp
using AMU.Editor.Core.Controllers;

// 直接的な設定値取得
bool autoVariant = SettingsController.GetSetting<bool>("AutoVariant_enableAutoVariant", false);
bool prebuild = SettingsController.GetSetting<bool>("AutoVariant_enablePrebuild", true);
bool includeAssets = SettingsController.GetSetting<bool>("AutoVariant_includeAllAssets", true);
string language = SettingsController.GetSetting<string>("Core_language", "ja");
```

## 移行チェックリスト

### コード更新
- [ ] `using`文の更新
- [ ] クラス名の変更対応
- [ ] メソッド名の変更対応
- [ ] 戻り値のチェック追加
- [ ] エラーハンドリングの追加

### 機能確認
- [ ] プレハブ自動変換の動作確認
- [ ] マテリアル最適化の動作確認
- [ ] アバターエクスポートの動作確認
- [ ] ビルド前処理の動作確認

### 設定確認
- [ ] 既存設定値の保持確認
- [ ] 新しい設定項目の初期化確認
- [ ] 設定UIでの表示確認

## トラブルシューティング

### コンパイルエラー
1. **型または名前空間が見つからない**
   - `using`文を新しい名前空間に更新

2. **メソッドが存在しない**
   - 移行表を確認して新しいメソッド名に変更

3. **戻り値の型が一致しない**
   - 戻り値がvoidからboolに変更されたメソッドを確認

### 実行時エラー
1. **設定値が反映されない**
   - `SettingsController.GetSetting`/`SetSetting`の使用を確認

2. **機能が動作しない**
   - 設定の有効性を`SettingsController`で確認

3. **エクスポートが失敗する**
   - `AvatarExportService`の戻り値を確認してエラー原因を特定

## サポート

### ログの確認
リファクタリング後のログプレフィックス：
- `[MaterialVariantService]`
- `[AvatarExportService]`
- `[ConvertVariantService]`
- `[MaterialOptimizationService]`
- `[AvatarValidationService]`
- `[PrebuildService]`

### デバッグ情報の取得
```csharp
using AMU.Editor.Core.Controllers;

// 設定状態の確認
Debug.Log($"AutoVariant Enabled: {SettingsController.GetSetting<bool>("AutoVariant_enableAutoVariant", false)}");
Debug.Log($"Prebuild Enabled: {SettingsController.GetSetting<bool>("AutoVariant_enablePrebuild", true)}");
Debug.Log($"Include All Assets: {SettingsController.GetSetting<bool>("AutoVariant_includeAllAssets", true)}");

// 設定の保存確認
SettingsController.SaveSettings();
```

このリファクタリングにより、コードの保守性、拡張性、テスト容易性が大幅に向上しました。移行作業が完了したら、旧ディレクトリ（Helper/、Watcher/、Controllers/、Schema/）の削除を推奨します。

注意: AutoVariantControllerとPrebuildSettingsは完全に削除され、全ての設定管理はCore.Controllers.SettingsControllerに統合されました。
