# Migration ガイド - AutoVariant

## 概要

このドキュメントは、AutoVariantモジュールのリファクタリング（旧Helper/Watcher構造から新しい5層アーキテクチャへ）による変更点と移行方法を説明します。

## リファクタリング前後の対応表

### ディレクトリ構造の変更

| 旧構造 | 新構造 | 変更理由 |
|--------|--------|----------|
| `Helper/MaterialHelper.cs` | `Api/MaterialVariantAPI.cs` | 外部公開APIとして整理 |
| `Watcher/AvatarExporter.cs` | `Api/AvatarExportAPI.cs` | 外部公開APIとして整理 |
| なし | `Controllers/AutoVariantController.cs` | 設定管理の専用コントローラ作成 |
| `Watcher/ConvertVariant.cs` | `Services/ConvertVariantService.cs` | サービス層として整理 |
| `Watcher/MaterialOptimizationManager.cs` | `Services/MaterialOptimizationService.cs` | サービス層として整理 |
| `Watcher/AvatarValidator.cs` | `Services/AvatarValidationService.cs` | サービス層として整理 |
| `Watcher/Prebuild.cs` | `Services/PrebuildService.cs` | サービス層として整理 |
| `Watcher/PrebuildSettings.cs` | `Schema/PrebuildSettings.cs` | スキーマ層として整理 |
| `Data/Setting.cs` | `Data/Setting.cs` | 変更なし |

### クラス名とメソッドの変更

#### MaterialHelper → MaterialVariantAPI
| 旧メソッド | 新メソッド | 変更点 |
|------------|------------|--------|
| `MaterialVariantOptimizer.OptimizeMaterials()` | `MaterialVariantAPI.OptimizeMaterials()` | クラス名変更、機能は同等 |
| `MaterialHashCalculator.Calculate()` | `MaterialVariantAPI.MaterialHashCalculator.Calculate()` | ネストクラスとして整理 |

#### AvatarExporter → AvatarExportAPI
| 旧メソッド | 新メソッド | 変更点 |
|------------|------------|--------|
| `AvatarExporter.ExportOptimizedAvatar()` | `AvatarExportAPI.ExportOptimizedAvatar()` | 戻り値がboolに変更 |
| なし | `AvatarExportAPI.GetAvatarAssets()` | 新規追加：アセット一覧取得 |

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
using AMU.Editor.AutoVariant.Api;

// 使用例
bool optimized = MaterialVariantAPI.OptimizeMaterials(avatar);
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
using AMU.Editor.AutoVariant.Api;

// 使用例
bool exported = AvatarExportAPI.ExportOptimizedAvatar(avatar);
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
```

#### 移行後
```csharp
using AMU.Editor.AutoVariant.Controllers;
using AMU.Editor.AutoVariant.Schema;

// Controllerを通したアクセス（推奨）
bool enabled = AutoVariantController.IsAutoVariantEnabled();

// または、Schemaを通したアクセス
bool enabled = PrebuildSettings.IsAutoVariantEnabled;
```

## 破壊的変更

### 1. 戻り値の変更
- `AvatarExportAPI.ExportOptimizedAvatar()`: `void` → `bool`
- 成功/失敗を判定できるように変更

### 2. クラス名の変更
- `MaterialVariantOptimizer` → `MaterialVariantAPI`
- `PrefabAdditionDetector` → `ConvertVariantService`
- `MyPreBuildProcess` → `PrebuildService`

### 3. 名前空間の変更
- すべてのクラスが新しい名前空間に移動
- `using`文の更新が必要

### 4. メソッドアクセスの変更
- 一部のprivateメソッドがinternalに変更
- 一部のpublicメソッドが整理・統合

## 新機能

### 1. Controllers層の追加
```csharp
using AMU.Editor.AutoVariant.Controllers;

// 設定の初期化
AutoVariantController.InitializeSettings();

// 設定の変更
AutoVariantController.SetAutoVariantEnabled(true);

// 設定の検証
if (AutoVariantController.ValidateSettings())
{
    Debug.Log("設定は正常です");
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
using AMU.Editor.AutoVariant.Schema;

// 型安全な設定値取得
bool optimization = PrebuildSettings.IsOptimizationEnabled;
string language = PrebuildSettings.CurrentLanguage;
```

## 移行チェックリスト

### コード更新
- [ ] `using`文の更新
- [ ] クラス名の変更対応
- [ ] メソッド名の変更対応
- [ ] 戻り値のチェック追加
- [ ] エラーハンドリングの追加

### 機能確認
- [ ] プリファブ自動変換の動作確認
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
   - `AutoVariantController.InitializeSettings()`の実行確認

2. **機能が動作しない**
   - 設定の有効性を`AutoVariantController`で確認

3. **エクスポートが失敗する**
   - `AvatarExportAPI`の戻り値を確認してエラー原因を特定

## サポート

### ログの確認
リファクタリング後のログプレフィックス：
- `[MaterialVariantAPI]`
- `[AvatarExportAPI]`
- `[ConvertVariantService]`
- `[MaterialOptimizationService]`
- `[AvatarValidationService]`
- `[PrebuildService]`
- `[AutoVariantController]`

### デバッグ情報の取得
```csharp
// 設定状態の確認
Debug.Log($"AutoVariant Enabled: {AutoVariantController.IsAutoVariantEnabled()}");
Debug.Log($"Prebuild Enabled: {AutoVariantController.IsPrebuildEnabled()}");
Debug.Log($"Include All Assets: {AutoVariantController.GetIncludeAllAssets()}");

// 設定の検証
if (!AutoVariantController.ValidateSettings())
{
    Debug.LogWarning("設定に問題があります");
}
```

このリファクタリングにより、コードの保守性、拡張性、テスト容易性が大幅に向上しました。移行作業が完了したら、旧ディレクトリ（Helper/、Watcher/）の削除を推奨します。
