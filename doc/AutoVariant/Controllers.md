# Controllers層 - AutoVariant

## 概要

Controllers層は、AutoVariantモジュールの永続データ管理とアクセス制御を担当します。EditorPrefsを使用した設定の管理、データの整合性確保、型安全なアクセスを提供します。

## クラス構成

### AutoVariantController

#### 目的
AutoVariant機能の設定データを管理し、統一されたアクセスインターフェースを提供します。

#### 主要メソッド

##### 初期化・検証メソッド

###### `InitializeSettings()`
AutoVariant設定を初期化します。初回起動時やリセット時に使用されます。

**処理内容:**
- 各設定項目のデフォルト値設定
- 未設定項目の初期化

**デフォルト値:**
- `AutoVariant_enableAutoVariant`: false
- `AutoVariant_enablePrebuild`: true
- `AutoVariant_includeAllAssets`: true

###### `ValidateSettings()`
すべての設定値の整合性を検証します。

**戻り値:**
- `bool`: 設定が有効かどうか

**検証項目:**
- ベースディレクトリパスの存在確認
- 設定値の論理的整合性

##### AutoVariant機能制御

###### `SetAutoVariantEnabled(bool enabled)`
AutoVariant機能の有効/無効を設定します。

**パラメータ:**
- `enabled`: 有効にするかどうか

**影響範囲:**
- ConvertVariantServiceの動作制御
- プリファブ自動変換の有効/無効

###### `IsAutoVariantEnabled()`
AutoVariant機能が有効かどうかを取得します。

**戻り値:**
- `bool`: 有効かどうか

##### Prebuild処理制御

###### `SetPrebuildEnabled(bool enabled)`
Prebuild処理の有効/無効を設定します。

**パラメータ:**
- `enabled`: 有効にするかどうか

**影響範囲:**
- PrebuildServiceでの最適化処理
- VRCSDKビルド前の自動最適化

###### `IsPrebuildEnabled()`
Prebuild処理が有効かどうかを取得します。

**戻り値:**
- `bool`: 有効かどうか

##### アセット含有制御

###### `SetIncludeAllAssets(bool include)`
エクスポート時にすべてのアセットを含める設定を変更します。

**パラメータ:**
- `include`: 含めるかどうか

**動作差分:**
- `true`: プロジェクト内のすべての依存アセットを含める
- `false`: AMU_Variants/以下のアセットのみ含める

###### `GetIncludeAllAssets()`
すべてのアセットを含める設定を取得します。

**戻り値:**
- `bool`: 含めるかどうか

##### ユーティリティメソッド

###### `ResetToDefaults()`
すべての設定をデフォルト値にリセットします。

**処理内容:**
- 全設定項目をデフォルト値に戻す
- InitializeSettings()の呼び出し

#### 使用例

```csharp
using AMU.Editor.AutoVariant.Controllers;

// 初期化
AutoVariantController.InitializeSettings();

// AutoVariant機能を有効化
AutoVariantController.SetAutoVariantEnabled(true);

// 設定の確認
if (AutoVariantController.IsAutoVariantEnabled())
{
    Debug.Log("AutoVariant機能が有効です");
}

// Prebuild処理を無効化
AutoVariantController.SetPrebuildEnabled(false);

// 設定の検証
if (AutoVariantController.ValidateSettings())
{
    Debug.Log("設定は正常です");
}

// 設定のリセット
AutoVariantController.ResetToDefaults();
```

## EditorPrefs キー一覧

| キー | 型 | デフォルト値 | 説明 |
|------|----|-----------|----- |
| `Setting.AutoVariant_enableAutoVariant` | bool | false | AutoVariant機能の有効/無効 |
| `Setting.AutoVariant_enablePrebuild` | bool | true | Prebuild最適化の有効/無効 |
| `Setting.AutoVariant_includeAllAssets` | bool | true | エクスポート時の全アセット含有 |

## データ整合性

### 検証項目
1. **ベースディレクトリパス**: `Setting.Core_dirPath`の存在確認
2. **論理的整合性**: 各設定値の妥当性チェック

### エラーハンドリング
- 設定値が不正な場合: 警告ログを出力し、デフォルト値にフォールバック
- ディレクトリが存在しない場合: 警告ログを出力し、ValidateSettings()でfalseを返す

## 他のコンポーネントとの連携

### Services層との連携
- **ConvertVariantService**: `IsAutoVariantEnabled()`で動作制御
- **PrebuildService**: `IsPrebuildEnabled()`で最適化処理制御
- **MaterialOptimizationService**: 設定に基づく処理制御

### Schema層との連携
- **PrebuildSettings**: 設定値の読み取りにSchemaクラスを使用
- リアルタイムな設定値アクセスを提供

### Data層との連携
- **AutoVariantSettingData**: 設定項目の定義に基づく管理
- 設定項目の追加時の拡張ポイント

## パフォーマンス考慮事項

### EditorPrefs アクセス最適化
- 頻繁にアクセスされる設定値はキャッシュを検討
- 設定変更時のみEditorPrefsに書き込み

### メモリ使用量
- 静的クラスのため、メモリ使用量は最小限
- 設定値のキャッシュ時は適切なライフサイクル管理が必要

## セキュリティ考慮事項

### 設定値の検証
- 外部からの不正な設定値を防ぐため、設定時に検証を実施
- パス関連の設定では、ディレクトリトラバーサル攻撃を防止

### アクセス制御
- 公開メソッドでのみ設定変更を許可
- 内部実装の詳細は非公開

## 今後の拡張予定

1. **設定プロファイル**: 複数の設定セットの管理
2. **設定のインポート/エクスポート**: 設定の共有機能
3. **リアルタイム設定同期**: 複数のエディターインスタンス間での設定同期
4. **設定変更イベント**: 設定変更時のコールバック機能
5. **設定のバージョン管理**: 設定スキーマのマイグレーション機能
