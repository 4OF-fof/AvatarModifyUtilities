# Data層 - AutoVariant

## 概要

Data層は、AutoVariantモジュールの具体的なデータ定義を担当します。設定項目の構造化、言語ファイルの管理、データの型定義を行い、設定システムの基盤を提供します。

## クラス構成

### AutoVariantSettingData

#### 目的
AutoVariantモジュールの設定項目を構造化して定義し、設定システムとの統合を提供します。

#### 特徴
- Core.Schema.SettingItemとの統合
- 設定項目の一元管理
- 型安全な設定定義
- デフォルト値の明示的定義

#### 設定項目定義

##### SettingItems Dictionary
AutoVariantモジュールの全設定項目を`Dictionary<string, SettingItem[]>`形式で定義します。

**構造:**
```csharp
public static readonly Dictionary<string, SettingItem[]> SettingItems = new Dictionary<string, SettingItem[]>
{
    { "AutoVariant", new SettingItem[] {
        // 設定項目の配列
    } },
};
```

##### 個別設定項目

###### AutoVariant_enableAutoVariant
AutoVariant機能の有効/無効を制御する設定項目です。

**型:** `BoolSettingItem`  
**キー:** `"AutoVariant_enableAutoVariant"`  
**デフォルト値:** `true`  
**UI表示名:** ローカライゼーションキーから取得

**説明:**
- プリファブ自動変換機能の制御
- ConvertVariantServiceの動作制御
- シーンへのプリファブ追加監視の有効性

###### AutoVariant_enablePrebuild
Prebuild処理の有効/無効を制御する設定項目です。

**型:** `BoolSettingItem`  
**キー:** `"AutoVariant_enablePrebuild"`  
**デフォルト値:** `true`  
**UI表示名:** ローカライゼーションキーから取得

**説明:**
- VRCSDKビルド前の自動最適化制御
- MaterialOptimizationServiceの実行制御
- アバターエクスポートの自動実行制御

###### AutoVariant_includeAllAssets
エクスポート時のアセット含有範囲を制御する設定項目です。

**型:** `BoolSettingItem`  
**キー:** `"AutoVariant_includeAllAssets"`  
**デフォルト値:** `true`  
**UI表示名:** ローカライゼーションキーから取得

**説明:**
- UnityPackageエクスポート時の依存アセット範囲制御
- `true`: 全依存アセットを含める
- `false`: AMU_Variants/以下のアセットのみ含める

#### 使用例

```csharp
using AMU.Editor.Setting;
using AMU.Editor.Core.Schema;

// 設定項目の取得
var autoVariantSettings = AutoVariantSettingData.SettingItems["AutoVariant"];

// 個別設定項目へのアクセス
foreach (var setting in autoVariantSettings)
{
    if (setting.Key == "AutoVariant_enableAutoVariant")
    {
        var boolSetting = setting as BoolSettingItem;
        Debug.Log($"AutoVariant機能: {boolSetting.DefaultValue}");
    }
}

// Core.UI.SettingWindowでの統合表示
// （設定ウィンドウから自動的に読み込まれ、UIに表示される）
```

## 言語ファイル (lang/)

### 構造
多言語対応のための言語ファイルが格納されます。

```
lang/
├── ja_jp.json    # 日本語ローカライゼーション
└── en_us.json    # 英語ローカライゼーション
```

### ローカライゼーションキー

AutoVariant設定項目用のキーが定義されています：

#### 設定項目名
- `AutoVariant_enableAutoVariant`: "Enable Auto Variant" / "自動バリアント有効化"
- `AutoVariant_enablePrebuild`: "Enable Prebuild Optimization" / "ビルド前最適化有効化"
- `AutoVariant_includeAllAssets`: "Include All Assets" / "全アセット含有"

#### 説明文
- `AutoVariant_enableAutoVariant_desc`: 機能の詳細説明
- `AutoVariant_enablePrebuild_desc`: Prebuild処理の詳細説明
- `AutoVariant_includeAllAssets_desc`: アセット含有設定の詳細説明

#### エラーメッセージ
- `AutoVariant_multipleAvatars_title`: "Build Cancelled" / "ビルド中止"
- `AutoVariant_multipleAvatars_message`: エラーメッセージ本文

## データの整合性

### 設定項目の一貫性
- 各設定項目は一意のキーで識別
- デフォルト値は機能的に適切な値を設定
- 型定義は実際の使用方法と一致

### ローカライゼーションの整合性
- すべての設定項目に対応する翻訳キーが存在
- 各言語ファイルで同一のキー構造を維持
- フォールバック言語（英語）での完全なカバレッジ

### バージョン互換性
- 設定キーの変更時は後方互換性を維持
- 新設定項目の追加時は適切なデフォルト値を提供

## Core.Schemaとの統合

### SettingItemクラスの活用
AutoVariantSettingDataは、Core.SchemaのSettingItemクラスを使用して設定項目を定義します。

#### BoolSettingItem
ブール値設定用のクラスです。

**プロパティ:**
- `Key`: 設定キー
- `DefaultValue`: デフォルト値
- `Description`: 設定の説明（ローカライゼーション対応）

#### 将来の拡張
必要に応じて他のSettingItemタイプも使用可能：
- `IntSettingItem`: 整数値設定
- `StringSettingItem`: 文字列設定
- `FloatSettingItem`: 浮動小数点設定

### 設定システムとの連携
1. **Core.UI.SettingWindow**: 自動的にUI表示
2. **Core.Controllers.SettingsController**: 設定値の管理
3. **AutoVariant.Controllers.AutoVariantController**: 専用設定管理

## パフォーマンス考慮事項

### メモリ使用量
- 静的readonly Dictionaryのため、初期化時のみメモリ確保
- 設定項目数に比例したメモリ使用
- ガベージコレクションの対象外（静的参照）

### アクセスパターン
- 設定項目の定義は起動時のみ読み込み
- 実行時の動的変更は不要
- 言語ファイルは必要時のみ読み込み

## 今後の拡張予定

### 設定項目の追加
1. **詳細最適化設定**:
   - `AutoVariant_optimizationLevel`: 最適化レベル（Int設定）
   - `AutoVariant_cacheEnabled`: キャッシュ有効性（Bool設定）

2. **エクスポート詳細設定**:
   - `AutoVariant_imageSize`: エクスポート画像サイズ（Int設定）
   - `AutoVariant_compressionLevel`: 圧縮レベル（Int設定）

3. **ログ設定**:
   - `AutoVariant_logLevel`: ログレベル（String設定）
   - `AutoVariant_verboseOutput`: 詳細出力（Bool設定）

### 言語サポートの拡張
1. **追加言語**:
   - 韓国語 (`ko_kr`)
   - 中国語簡体字 (`zh_cn`)
   - 中国語繁体字 (`zh_tw`)

2. **動的言語切り替え**:
   - 実行時の言語変更対応
   - 設定UIのリアルタイム更新

### データ検証機能
1. **設定値検証**:
   - 設定値の範囲チェック
   - 論理的整合性検証
   - 必須設定項目のチェック

2. **マイグレーション機能**:
   - 古い設定形式からの移行
   - 設定スキーマのバージョン管理
   - 設定のバックアップ・復元

## トラブルシューティング

### 設定項目が表示されない場合
1. AutoVariantSettingDataの定義確認
2. ローカライゼーションキーの存在確認
3. Core.UI.SettingWindowでの読み込み確認

### 言語が切り替わらない場合
1. 言語ファイルの存在確認
2. キー名の一致確認
3. Core.Controllers.LocalizationControllerの状態確認

### デフォルト値が反映されない場合
1. BoolSettingItemのDefaultValue確認
2. AutoVariantController.InitializeSettings()の実行確認
3. EditorPrefsの状態確認

## デバッグ支援

### 設定項目の確認方法
```csharp
// 全設定項目の表示
foreach (var category in AutoVariantSettingData.SettingItems)
{
    Debug.Log($"Category: {category.Key}");
    foreach (var item in category.Value)
    {
        Debug.Log($"  {item.Key}: {item.DefaultValue}");
    }
}
```

### 言語ファイルの確認方法
```csharp
// 現在の言語設定での翻訳確認
string currentLang = EditorPrefs.GetString("Setting.Core_language", "en_us");
Debug.Log($"Current Language: {currentLang}");

// 特定キーの翻訳確認
string translatedText = LocalizationController.GetText("AutoVariant_enableAutoVariant");
Debug.Log($"Translated: {translatedText}");
```
