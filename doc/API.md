# 公開API一覧

## 概要

AvatarModifyUtilities（AMU）で公開されている全てのAPIの使用方法を説明します。

## Core API

### ObjectCaptureAPI

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

### VRChatAPI

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
