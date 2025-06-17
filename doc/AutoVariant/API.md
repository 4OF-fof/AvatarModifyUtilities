# API層 - AutoVariant

## 概要

API層は、AutoVariantモジュールの外部公開機能を提供します。他のモジュールや外部からの呼び出しに対して、統一されたインターフェースを提供し、内部実装の詳細を隠蔽します。

## クラス構成

### MaterialVariantAPI

#### 目的
プリファブバリアントのマテリアル最適化機能を提供する公開APIです。

#### 主要メソッド

##### `OptimizeMaterials(GameObject targetObject)`
指定されたGameObjectのマテリアルを最適化します。

**パラメータ:**
- `targetObject`: 最適化対象のGameObject

**戻り値:**
- `bool`: 最適化が実行されたかどうか

**処理フロー:**
1. 入力の検証（プリファブインスタンスかどうか）
2. 親プリファブの取得
3. 再帰的なマテリアル処理
4. 最適化されたマテリアルの適用

**使用例:**
```csharp
using AMU.Editor.AutoVariant.Api;

GameObject avatar = // アバターオブジェクト
bool optimized = MaterialVariantAPI.OptimizeMaterials(avatar);
if (optimized)
{
    Debug.Log("マテリアルが最適化されました");
}
```

#### ユーティリティクラス

##### MaterialHashCalculator
マテリアルの内容を基にMD5ハッシュを計算するユーティリティです。

**主要メソッド:**
- `Calculate(Material material)`: マテリアルのハッシュ値を計算

**ハッシュ計算対象:**
- シェーダー情報
- シェーダープロパティ（Color, Vector, Float, Texture, Int）
- テクスチャのオフセット・スケール
- シェーダーキーワード

### AvatarExportAPI

#### 目的
最適化されたアバターのエクスポート機能を提供する公開APIです。

#### 主要メソッド

##### `ExportOptimizedAvatar(GameObject avatar)`
最適化されたアバターをUnityPackageとしてエクスポートします。

**パラメータ:**
- `avatar`: エクスポート対象のアバター

**戻り値:**
- `bool`: エクスポートが成功したかどうか

**エクスポート内容:**
- アバタープリファブとその依存関係
- 関連するアセット（設定に応じて全アセットまたは限定アセット）
- アバターのプレビュー画像（512x512 PNG）

**出力先:**
- ベースディレクトリ/AutoVariant/[BlueprintID]/
- Blueprint IDがない場合: ベースディレクトリ/AutoVariant/local/

**ファイル命名規則:**
- UnityPackage: `[YYMMDD]-[アバター名]-[連番].unitypackage`
- プレビュー画像: `[YYMMDD]-[アバター名]-[連番].png`

##### `GetAvatarAssets(GameObject avatar)`
アバターに関連するアセットパスのリストを取得します。

**パラメータ:**
- `avatar`: 対象のアバター

**戻り値:**
- `List<string>`: アセットパスのリスト

**使用例:**
```csharp
using AMU.Editor.AutoVariant.Api;

GameObject avatar = // アバターオブジェクト
bool exported = AvatarExportAPI.ExportOptimizedAvatar(avatar);
if (exported)
{
    Debug.Log("アバターがエクスポートされました");
}

// アセット一覧の取得
var assets = AvatarExportAPI.GetAvatarAssets(avatar);
Debug.Log($"関連アセット数: {assets.Count}");
```

## エラーハンドリング

### MaterialVariantAPI
- `targetObject`がnullの場合: エラーログを出力してfalseを返す
- プリファブインスタンスでない場合: エラーログを出力してfalseを返す
- 親プリファブが見つからない場合: 警告ログを出力してfalseを返す

### AvatarExportAPI
- `avatar`がnullの場合: エラーログを出力してfalseを返す
- エクスポート対象アセットが見つからない場合: 警告ログを出力してfalseを返す
- エクスポート処理中の例外: エラーログを出力してfalseを返す

## 設定依存

### MaterialVariantAPI
- 設定への直接依存なし

### AvatarExportAPI
- `Setting.Core_dirPath`: エクスポート先ベースディレクトリ
- `Setting.AutoVariant_includeAllAssets`: 全アセット含有フラグ

## パフォーマンス考慮事項

### MaterialVariantAPI
- ハッシュ計算は重い処理のため、結果をキャッシュすることを推奨
- 大きなプリファブでは再帰処理が深くなる可能性

### AvatarExportAPI
- アセット依存関係の収集は時間がかかる場合がある
- 画像キャプチャ処理は同期的に実行される

## 今後の改善予定

1. **非同期処理対応**: 重い処理の非同期化
2. **プログレス表示**: 長時間処理のプログレス表示
3. **キャッシュ機能**: ハッシュ計算結果のキャッシュ
4. **バッチ処理**: 複数アバターの一括処理対応
