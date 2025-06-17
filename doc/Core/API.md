# API 層ドキュメント

## 概要

API層は、外部モジュールから呼び出される公開機能を提供する層です。他のモジュール（AssetManager、AutoVariantなど）からアクセスされる機能はここに配置されます。

## 設計原則

- **シンプルで直感的なインターフェース**
- **エラーハンドリングの徹底**
- **ドキュメント化の充実**
- **テスタビリティの確保**

## API一覧

### ObjectCaptureAPI

#### 概要
GameObjectのスクリーンキャプチャ機能を提供します。

#### 名前空間
```csharp
using AMU.Editor.Core.API;
```

#### メソッド

##### CaptureObject
```csharp
public static Texture2D CaptureObject(
    GameObject targetObject, 
    string savePath, 
    int width = 512, 
    int height = 512
)
```

**パラメータ:**
- `targetObject`: キャプチャ対象のGameObject
- `savePath`: 保存先のファイルパス
- `width`: キャプチャ画像の幅（デフォルト: 512）
- `height`: キャプチャ画像の高さ（デフォルト: 512）

**戻り値:**
- `Texture2D`: キャプチャされたテクスチャ。失敗時は`null`

**使用例:**
```csharp
using AMU.Editor.Core.API;

var avatar = GameObject.Find("MyAvatar");
var savePath = "Assets/Thumbnails/avatar_thumbnail.png";
var texture = ObjectCaptureAPI.CaptureObject(avatar, savePath, 1024, 1024);

if (texture != null)
{
    Debug.Log("キャプチャ成功");
}
```

#### 内部実装
1. 一時的なカメラを作成
2. オブジェクトの境界を計算
3. カメラ位置とパラメータを設定
4. レンダリング実行
5. テクスチャ保存
6. リソースのクリーンアップ

### VRChatAPI

#### 概要
VRChat関連の機能を提供します。

#### 名前空間
```csharp
using AMU.Editor.Core.API;
```

#### メソッド

##### GetBlueprintId
```csharp
public static string GetBlueprintId(GameObject go)
```

**パラメータ:**
- `go`: PipelineManagerコンポーネントを持つGameObject

**戻り値:**
- `string`: Blueprint ID（"avtr_"で始まる場合のみ）。取得できない場合は`null`

**使用例:**
```csharp
using AMU.Editor.Core.API;

var avatar = Selection.activeGameObject;
var blueprintId = VRChatAPI.GetBlueprintId(avatar);

if (!string.IsNullOrEmpty(blueprintId))
{
    Debug.Log($"Blueprint ID: {blueprintId}");
}
```

##### IsVRCAvatar
```csharp
public static bool IsVRCAvatar(GameObject obj)
```

**パラメータ:**
- `obj`: 判定対象のGameObject

**戻り値:**
- `bool`: VRCアバターの場合`true`、そうでなければ`false`

**使用例:**
```csharp
using AMU.Editor.Core.API;

var selectedObject = Selection.activeGameObject;
if (VRChatAPI.IsVRCAvatar(selectedObject))
{
    Debug.Log("これはVRCアバターです");
}
```

## エラーハンドリング

### 共通のエラーパターン

1. **null参照エラー**
   - 入力パラメータのnullチェック
   - 適切なエラーメッセージの出力

2. **ファイルI/Oエラー**
   - ディレクトリの存在確認
   - 書き込み権限の確認

3. **Unity固有のエラー**
   - コンポーネントの存在確認
   - AssetDatabase操作のエラー

### エラー処理の例

```csharp
public static Texture2D CaptureObject(GameObject targetObject, string savePath, int width = 512, int height = 512)
{
    if (targetObject == null)
    {
        Debug.LogError("Target object is null");
        return null;
    }

    if (string.IsNullOrEmpty(savePath))
    {
        Debug.LogError("Save path is required");
        return null;
    }

    try
    {
        // メイン処理
    }
    catch (System.Exception e)
    {
        Debug.LogError($"Failed to capture object: {e.Message}");
        return null;
    }
}
```

## パフォーマンス考慮事項

### ObjectCaptureAPI
- **メモリ使用量**: レンダーテクスチャのサイズに比例
- **処理時間**: オブジェクトの複雑さとキャプチャサイズに依存
- **リソース管理**: 一時的なGameObjectとテクスチャの適切な破棄

### VRChatAPI
- **リフレクション使用**: PipelineManagerコンポーネントへのアクセスにリフレクションを使用
- **キャッシュ検討**: 頻繁にアクセスされる場合はタイプ情報のキャッシュを検討

## 拡張ガイド

### 新しいAPIの追加手順

1. **ファイル作成**: `Core/API/` 以下に新しいAPIクラスを作成
2. **名前空間**: `AMU.Editor.Core.API` を使用
3. **クラス命名**: `{機能名}API` の形式
4. **メソッド設計**: staticメソッドで公開
5. **ドキュメント**: XMLドキュメンテーションを追加
6. **エラーハンドリング**: 適切なエラー処理を実装
7. **テスト**: 基本的なテストケースを確認

### API設計のベストプラクティス

1. **シンプルなインターフェース**: 複雑なパラメータを避ける
2. **デフォルト値**: よく使用される値をデフォルトパラメータに
3. **戻り値**: 成功/失敗が明確にわかる戻り値
4. **命名**: 動詞で始まる明確なメソッド名
5. **非同期処理**: 長時間の処理は非同期パターンを検討
