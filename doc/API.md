# 公開API一覧

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

## VrcAssetManager API

### VrcAssetFileAPI

VRCアセットファイルのAPI機能を提供します。

#### 名前空間
```csharp
using AMU.Editor.VrcAssetManager.API;
```

#### メソッド

##### ScanDirectory
```csharp
public static List<string> ScanDirectory(string directoryPath, bool recursive = true)
```

指定されたディレクトリ内のすべてのファイルをスキャンします。ファイル形式による制限はありません。

**パラメータ:**
- `directoryPath`: スキャンするディレクトリパス
- `recursive`: サブディレクトリも含めるかどうか（デフォルト: true）

**戻り値:**
- `List<string>`: 発見されたファイルのパスリスト

**使用例:**
```csharp
using AMU.Editor.VrcAssetManager.API;

// 再帰的スキャン（すべてのファイル）
var files = VrcAssetFileAPI.ScanDirectory(@"C:\VRCAssets", true);
foreach (var file in files)
{
    Debug.Log($"発見されたファイル: {file}");
}

// 単一ディレクトリのみスキャン
var filesInRoot = VrcAssetFileAPI.ScanDirectory(@"C:\VRCAssets", false);
Debug.Log($"ルートディレクトリ内のファイル数: {filesInRoot.Count}");
```
