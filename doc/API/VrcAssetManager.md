# API

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