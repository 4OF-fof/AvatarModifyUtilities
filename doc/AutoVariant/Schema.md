# Schema層 - AutoVariant

## 概要

Schema層は、AutoVariantモジュールのデータ構造とスキーマ定義を担当します。設定値へのアクセス、データの型定義、設定項目の構造化を提供し、他の層からの安全なデータアクセスを保証します。

## クラス構成

### PrebuildSettings

#### 目的
Prebuildプロセスで使用される設定値への統一アクセスインターフェースを提供します。

#### 特徴
- 静的プロパティによる簡潔なアクセス
- EditorPrefsの直接アクセスをカプセル化
- デフォルト値の一元管理
- 型安全なアクセス保証

#### プロパティ一覧

##### `IsOptimizationEnabled`
最適化処理が有効かどうかを取得します。

**型:** `bool`  
**EditorPrefsキー:** `Setting.AutoVariant_enablePrebuild`  
**デフォルト値:** `true`

**使用場面:**
- PrebuildServiceでの最適化実行判定
- MaterialOptimizationServiceでの処理制御

##### `IncludeAllAssets`
エクスポート時にすべてのアセットを含めるかどうかを取得します。

**型:** `bool`  
**EditorPrefsキー:** `Setting.AutoVariant_includeAllAssets`  
**デフォルト値:** `true`

**使用場面:**
- AvatarExportAPIでのアセット収集
- エクスポート範囲の制御

**動作差分:**
- `true`: プロジェクト内のすべての依存アセットを含める
- `false`: AMU_Variants/以下のアセットのみ含める

##### `CurrentLanguage`
現在の言語設定を取得します。

**型:** `string`  
**EditorPrefsキー:** `Setting.Core_language`  
**デフォルト値:** `"en_us"`

**使用場面:**
- AvatarValidationServiceでのエラーメッセージ言語選択
- 多言語対応UIでの表示言語制御

**サポート言語:**
- `"ja_jp"`: 日本語
- `"en_us"`: 英語（デフォルト）

##### `BaseDirectoryPath`
ベースディレクトリパスを取得します。

**型:** `string`  
**EditorPrefsキー:** `Setting.Core_dirPath`  
**デフォルト値:** `マイドキュメント/AvatarModifyUtilities`

**使用場面:**
- AvatarExportAPIでのエクスポート先ディレクトリ決定
- ファイル出力処理での基準パス

**パス構造:**
```
{BaseDirectoryPath}/
├── AutoVariant/
│   ├── local/           # Blueprint IDなしのアバター
│   └── {BlueprintID}/   # Blueprint ID別ディレクトリ
```

##### `IsAutoVariantEnabled`
AutoVariant機能が有効かどうかを取得します。

**型:** `bool`  
**EditorPrefsキー:** `Setting.AutoVariant_enableAutoVariant`  
**デフォルト値:** `false`

**使用場面:**
- ConvertVariantServiceの動作制御
- プリファブ自動変換機能の有効性判定

#### 使用例

```csharp
using AMU.Editor.AutoVariant.Schema;

// 最適化が有効かチェック
if (PrebuildSettings.IsOptimizationEnabled)
{
    // 最適化処理を実行
    MaterialOptimizationService.OptimizeActiveAvatars();
}

// 言語設定に基づくメッセージ表示
string language = PrebuildSettings.CurrentLanguage;
string message = language == "ja_jp" ? "処理完了" : "Processing completed";

// エクスポート先ディレクトリの取得
string baseDir = PrebuildSettings.BaseDirectoryPath;
string exportDir = Path.Combine(baseDir, "AutoVariant");

// アセット含有設定に基づく処理
bool includeAll = PrebuildSettings.IncludeAllAssets;
var assets = includeAll ? GetAllAssets() : GetVariantAssets();
```

## データの整合性

### 型安全性
- すべてのプロパティは適切な型で定義
- EditorPrefs.GetBool/GetStringの型変換エラーを防止
- nullチェックとデフォルト値の適用

### デフォルト値の保証
- EditorPrefsキーが存在しない場合の適切なフォールバック
- 初回起動時の安全なデフォルト値提供

### 設定値の検証
現在は基本的な型チェックのみ実装。将来的に以下の検証を追加予定：
- パス形式の検証
- 言語コードの検証
- 論理的整合性のチェック

## パフォーマンス考慮事項

### EditorPrefs アクセス最適化
- プロパティアクセスごとにEditorPrefsを読み取り
- 頻繁なアクセスが予想される場合はキャッシュを検討

### メモリ使用量
- 静的プロパティのため、インスタンス生成コストなし
- 設定値のキャッシュは未実装（メモリ効率重視）

## 他の層との連携

### Controllers層との関係
- AutoVariantControllerが設定値を変更
- PrebuildSettingsが最新の設定値を取得
- 書き込みと読み取りの責任分離

### Services層との関係
- 各Serviceクラスが設定値を参照
- リアルタイムな設定反映
- 設定変更時の即座な動作変更

### API層との関係
- 間接的な依存（Services層経由）
- 設定に基づく動作制御

## 今後の拡張予定

### 設定項目の追加
1. **キャッシュ設定**: マテリアルハッシュキャッシュの有効性
2. **ログレベル設定**: デバッグ情報の詳細度制御
3. **エクスポート設定**: 画像サイズ、圧縮設定など

### バリデーション機能
1. **パス検証**: ディレクトリの存在確認、アクセス権限チェック
2. **設定整合性**: 設定値間の論理的整合性検証
3. **設定マイグレーション**: 古い設定形式からの移行

### パフォーマンス最適化
1. **設定キャッシュ**: 頻繁にアクセスされる設定値のキャッシュ
2. **変更通知**: 設定変更時のイベント通知システム
3. **遅延読み込み**: 必要時のみ設定値を読み込み

### 型安全性の向上
1. **列挙型の活用**: 言語設定などの定数値を列挙型で管理
2. **カスタムクラス**: 複雑な設定項目のカスタムクラス化
3. **設定検証属性**: 設定値の範囲やフォーマット検証

## セキュリティ考慮事項

### パス関連設定
- ディレクトリトラバーサル攻撃の防止
- 相対パス・絶対パスの適切な処理
- 書き込み権限のないディレクトリでのエラーハンドリング

### 設定値の検証
- 外部からの不正な設定値の防止
- 設定ファイルの改ざん検出（将来実装予定）

## デバッグ・トラブルシューティング

### ログ出力
現在は基本的なエラーログのみ。将来的に追加予定：
- 設定値の読み取りログ
- デフォルト値使用時の警告
- 設定値の変更履歴

### 設定値の確認方法
```csharp
// すべての設定値を確認
Debug.Log($"AutoVariant Enabled: {PrebuildSettings.IsAutoVariantEnabled}");
Debug.Log($"Optimization Enabled: {PrebuildSettings.IsOptimizationEnabled}");
Debug.Log($"Include All Assets: {PrebuildSettings.IncludeAllAssets}");
Debug.Log($"Language: {PrebuildSettings.CurrentLanguage}");
Debug.Log($"Base Directory: {PrebuildSettings.BaseDirectoryPath}");
```
