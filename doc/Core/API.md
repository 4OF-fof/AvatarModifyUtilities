# API 層ドキュメント

## 概要

API層は、外部モジュールから呼び出される公開機能を提供する層です。他のモジュール（AssetManager、AutoVariantなど）からアクセスされる機能はここに配置されます。

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
