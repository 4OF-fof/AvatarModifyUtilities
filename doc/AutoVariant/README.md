# AutoVariant モジュール ドキュメント

## 概要

AutoVariantモジュールは、VRChatアバター用のプレハブバリアント自動生成と最適化機能を提供するモジュールです。以下の3つの明確な層に分離されています：

- **API層**: 外部から呼び出される公開機能
- **Services層**: 初期化処理とサービス機能
- **Data層**: 具体的なデータ定義

設定管理は、Coreモジュールの統一システム（Core.Controllers.SettingsController）を使用します。

## ディレクトリ構造

```
AutoVariant/
├── Api/                            # 外部公開API
│   ├── MaterialVariantAPI.cs       # マテリアル最適化API
│   └── AvatarExportAPI.cs          # アバターエクスポートAPI
├── Services/                       # サービス層
│   ├── ConvertVariantService.cs    # プレハブ変換監視サービス
│   ├── MaterialOptimizationService.cs # マテリアル最適化サービス
│   ├── AvatarValidationService.cs  # アバター検証サービス
│   └── PrebuildService.cs          # ビルド前処理サービス
└── Data/                           # データ定義
    ├── Setting.cs                  # 設定データ定義
    └── lang/                       # 言語ファイル
```

## 主要機能

### 1. プレハブバリアント自動生成
- シーンにプレハブが追加された際の自動検出
- AMU_Variantsフォルダへの自動バリアント作成
- マテリアルの自動コピーと置換

### 2. マテリアル最適化
- バリアントと親プレハブ間のマテリアル比較
- 同一マテリアルの自動最適化
- MD5ハッシュベースの重複検出

### 3. アバターエクスポート
- 最適化されたアバターのUnityPackageエクスポート
- アバター画像の自動キャプチャ
- Blueprint ID別のディレクトリ管理

### 4. ビルド前最適化
- VRCSDKビルドプロセスとの統合
- アクティブアバターの自動検証
- ビルド前の自動最適化処理

## 層の詳細

### API層 (`AutoVariant/Api/`)

外部モジュールから呼び出される公開機能を提供します。

#### MaterialVariantAPI
- **目的**: マテリアルバリアントの最適化
- **主要メソッド**:
  - `OptimizeMaterials(GameObject)`: マテリアル最適化実行
- **ユーティリティクラス**:
  - `MaterialHashCalculator`: マテリアルハッシュ計算

#### AvatarExportAPI
- **目的**: 最適化されたアバターのエクスポート
- **主要メソッド**:
  - `ExportOptimizedAvatar(GameObject)`: アバターエクスポート
  - `GetAvatarAssets(GameObject)`: アセット収集

## 設定管理

AutoVariantの設定は、Coreモジュールの統一設定システムを使用して管理されます：

- **Core.Controllers.SettingsController**: 設定の初期化、取得、保存
- **Core.UI.SettingWindow**: 設定UIでの表示・編集
- **AutoVariant.Data.AutoVariantSettingData**: 設定項目の定義

### 設定アクセス方法

```csharp
using AMU.Editor.Core.Controllers;

// 設定値の取得
bool enabled = SettingsController.GetSetting<bool>("AutoVariant_enableAutoVariant", false);

// 設定値の変更
SettingsController.SetSetting("AutoVariant_enableAutoVariant", true);
```

### Services層 (`AutoVariant/Services/`)

初期化処理とサービス機能を担当します。

#### ConvertVariantService
- **目的**: プレハブ変換の監視と自動処理
- **機能**:
  - Hierarchyの変更監視
  - プレハブ追加の自動検出
  - バリアント生成とマテリアル処理
- **主要メソッド**:
  - `Initialize()`: サービス初期化
  - `Shutdown()`: サービス停止

#### MaterialOptimizationService
- **目的**: マテリアル最適化の管理
- **機能**:
  - アクティブアバターの最適化
  - マテリアル状態の保存・復元
  - ネストされたプレハブの処理
- **主要メソッド**:
  - `OptimizeActiveAvatars()`: 全アクティブアバター最適化
  - `OptimizeAvatar(GameObject)`: 個別アバター最適化

#### AvatarValidationService
- **目的**: アバターの検証とチェック
- **機能**:
  - アクティブアバターの検索
  - アバター数の検証
  - VRCアバターの判定
- **主要メソッド**:
  - `ValidateAvatarCount()`: アバター数検証
  - `FindActiveAvatars()`: アクティブアバター検索
  - `GetSingleActiveAvatar()`: 単一アクティブアバター取得
  - `IsVRCAvatar(GameObject)`: VRCアバター判定

#### PrebuildService
- **目的**: VRCSDKビルド前処理
- **機能**:
  - ビルド前の検証
  - 自動最適化の実行
- **VRCSDKコールバック**:
  - `OnBuildRequested(VRCSDKRequestedBuildType)`: ビルド要求時処理

### Schema層 (`AutoVariant/Schema/`)


### Data層 (`AutoVariant/Data/`)

具体的なデータ定義を担当します。

#### AutoVariantSettingData
- **目的**: AutoVariant設定項目の定義
- **設定項目**:
  - `AutoVariant_enableAutoVariant`: AutoVariant機能の有効/無効（デフォルト: false）
  - `AutoVariant_enablePrebuild`: Prebuild処理の有効/無効（デフォルト: true）
  - `AutoVariant_includeAllAssets`: 全アセット含有の有効/無効（デフォルト: true）

## ワークフロー

### 1. プレハブ追加からバリアント生成まで
1. ConvertVariantServiceがHierarchy変更を監視
2. 新しいプレハブが検出される
3. AMU_Variantsディレクトリが作成される
4. マテリアルがコピーされ、バリアント用マテリアルに置換される
5. プレハブバリアントが作成される
6. シーン内のオブジェクトがバリアントに置換される

### 2. ビルド前最適化プロセス
1. PrebuildServiceがVRCSDKビルド要求を受信
2. AvatarValidationServiceでアバター数を検証
3. MaterialOptimizationServiceで最適化を実行
4. AvatarExportAPIで最適化されたアバターをエクスポート

### 3. マテリアル最適化プロセス
1. MaterialVariantAPIがバリアントと親プレハブを比較
2. MaterialHashCalculatorでマテリアルハッシュを計算
3. 同一ハッシュのマテリアルを親プレハブのマテリアルに置換
4. 変更をプレハブに適用

## 依存関係

- **Core.Controllers**: SettingsController（設定管理）
- **Core.Helper**: PipelineManagerHelper, ObjectCaptureHelper
- **Core.Schema**: SettingItem
- **VRChatSDK**: IVRCSDKBuildRequestedCallback
- **Unity Editor**: PrefabUtility, AssetDatabase, EditorPrefs
