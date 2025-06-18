# Controllers

## VrcAssetManager Controllers

### VrcAssetController

VRCアセットの管理を行うメインコントローラです。

#### 名前空間
```csharp
using AMU.Editor.VrcAssetManager.Controllers;
using AMU.Editor.VrcAssetManager.Schema;
```

#### 主要機能

##### アセットの追加
```csharp
public static bool AddAsset(AssetSchema assetData)
```

新しいVRCアセットをキャッシュに追加します。

**パラメータ:**
- `assetData`: 追加するアセットデータ

**戻り値:**
- `bool`: 追加に成功した場合true

**使用例:**
```csharp
var assetData = new AssetSchema("MyAvatar", AssetType.Avatar, "Assets/MyAvatar.prefab");
bool success = VrcAssetController.AddAsset(assetData);
if (success)
{
    Debug.Log("アセットが正常に追加されました");
}
```

##### アセットの更新
```csharp
public static bool UpdateAsset(AssetSchema assetData)
```

既存のVRCアセットを更新します。

**パラメータ:**
- `assetData`: 更新するアセットデータ

**戻り値:**
- `bool`: 更新に成功した場合true

**使用例:**
```csharp
var existingAsset = VrcAssetController.GetAsset(assetId);
if (existingAsset.Id != default(AssetId))
{
    existingAsset.Metadata.Description = "更新された説明";
    VrcAssetController.UpdateAsset(existingAsset);
}
```

##### アセットの削除
```csharp
public static bool RemoveAsset(AssetId assetId)
```

指定されたIDのVRCアセットを削除します。

**パラメータ:**
- `assetId`: 削除するアセットのID

**戻り値:**
- `bool`: 削除に成功した場合true

**使用例:**
```csharp
bool removed = VrcAssetController.RemoveAsset(assetId);
```

##### アセットの取得
```csharp
public static AssetSchema GetAsset(AssetId assetId)
public static List<AssetSchema> GetAllAssets()
public static List<AssetSchema> GetAssetsByCategory(string category)
public static List<AssetSchema> GetAssetsByAuthor(string author)
```

**使用例:**
```csharp
// 特定のアセットを取得
var asset = VrcAssetController.GetAsset(assetId);

// 全アセットを取得
var allAssets = VrcAssetController.GetAllAssets();

// カテゴリ別に取得
var avatars = VrcAssetController.GetAssetsByCategory("Avatar");

// 作者別に取得
var authorAssets = VrcAssetController.GetAssetsByAuthor("AuthorName");
```

##### アセットの検索
```csharp
public static List<AssetSchema> SearchAssets(string searchTerm)
```

名前、説明、作者名で部分一致検索を行います。

**パラメータ:**
- `searchTerm`: 検索文字列

**戻り値:**
- `List<AssetSchema>`: 検索条件に一致するアセットのリスト

**使用例:**
```csharp
var searchResults = VrcAssetController.SearchAssets("avatar");
```

##### キャッシュ管理
```csharp
public static void ClearCache()
public static int GetCachedAssetCount()
public static List<string> GetAvailableCategories()
public static List<string> GetAvailableAuthors()
```

**使用例:**
```csharp
// キャッシュをクリア
VrcAssetController.ClearCache();

// キャッシュされているアセット数を取得
int count = VrcAssetController.GetCachedAssetCount();

// 利用可能なカテゴリを取得
var categories = VrcAssetController.GetAvailableCategories();

// 利用可能な作者を取得
var authors = VrcAssetController.GetAvailableAuthors();
```

### VrcAssetFileController

VRCアセットファイルの操作を管理するコントローラです。

#### 名前空間
```csharp
using AMU.Editor.VrcAssetManager.Controllers;
using AMU.Editor.VrcAssetManager.Schema;
```

#### 主要機能

##### ファイルの検証
```csharp
public static bool IsValidVrcAssetFile(string filePath)
public static bool ValidateAssetFile(AssetSchema assetData)
```

**サポートファイル形式:**
- Prefabs: `.prefab`
- Scenes: `.unity`
- Packages: `.unitypackage`
- Models: `.fbx`, `.obj`
- Textures: `.png`, `.jpg`, `.jpeg`, `.tga`, `.psd`
- Materials: `.mat`
- Shaders: `.shader`, `.hlsl`, `.cginc`
- Scripts: `.cs`, `.dll`, `.asmdef`

**使用例:**
```csharp
bool isValid = VrcAssetFileController.IsValidVrcAssetFile(@"C:\Assets\MyAvatar.prefab");
if (isValid)
{
    Debug.Log("有効なVRCアセットファイルです");
}
```

##### ファイルのインポート
```csharp
public static AssetSchema ImportAssetFile(string filePath)
public static List<AssetSchema> ImportMultipleAssetFiles(IEnumerable<string> filePaths)
```

**使用例:**
```csharp
// 単一ファイルのインポート
var assetData = VrcAssetFileController.ImportAssetFile(@"C:\Assets\MyAvatar.prefab");
if (assetData.Id != default(AssetId))
{
    VrcAssetController.AddAsset(assetData);
}

// 複数ファイルの一括インポート
var filePaths = new[] { "file1.prefab", "file2.fbx", "file3.png" };
var importedAssets = VrcAssetFileController.ImportMultipleAssetFiles(filePaths);
foreach (var asset in importedAssets)
{
    VrcAssetController.AddAsset(asset);
}
```

##### ディレクトリスキャン
```csharp
public static List<string> ScanDirectory(string directoryPath, bool recursive = true)
```

**パラメータ:**
- `directoryPath`: スキャンするディレクトリパス
- `recursive`: サブディレクトリも含めるかどうか（デフォルト: true）

**使用例:**
```csharp
// 再帰的スキャン
var files = VrcAssetFileController.ScanDirectory(@"C:\VRCAssets", true);

// 単一ディレクトリのみスキャン
var filesInRoot = VrcAssetFileController.ScanDirectory(@"C:\VRCAssets", false);
```

##### ファイルのエクスポート
```csharp
public static bool ExportAsset(AssetSchema assetData, string destinationPath)
```

**使用例:**
```csharp
var asset = VrcAssetController.GetAsset(assetId);
bool exported = VrcAssetFileController.ExportAsset(asset, @"C:\Export");
```

##### ファイル情報の更新
```csharp
public static AssetSchema RefreshAssetFileInfo(AssetSchema assetData)
```

**使用例:**
```csharp
var asset = VrcAssetController.GetAsset(assetId);
var refreshedAsset = VrcAssetFileController.RefreshAssetFileInfo(asset);
VrcAssetController.UpdateAsset(refreshedAsset);
```