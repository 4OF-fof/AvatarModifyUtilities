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
public static List<AssetSchema> SearchAssets(string searchTerm)
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

//名前で取得
var searchResults = VrcAssetController.SearchAssets("AwesomeAvatar");
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

##### ファイルのインポート
```csharp
public static AssetSchema ImportAssetFile(string filePath)
```

指定されたファイルパスからVRCアセットデータを作成します。あらゆるファイル形式をインポート可能です。

**主要カテゴリ（自動判定）:**
- Prefabs: `.prefab`
- Scenes: `.unity`
- Packages: `.unitypackage`
- Models: `.fbx`, `.obj`
- Textures: `.png`, `.jpg`, `.jpeg`, `.tga`, `.psd`
- Materials: `.mat`
- Shaders: `.shader`, `.hlsl`, `.cginc`
- Scripts: `.cs`, `.dll`, `.asmdef`
- Other: 上記以外のすべてのファイル

**使用例:**
```csharp
// ファイルのインポート
var assetData = VrcAssetFileController.ImportAssetFile(@"C:\Assets\MyAvatar.prefab");
if (assetData.Id != default(AssetId))
{
    VrcAssetController.AddAsset(assetData);
}
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

### VrcAssetFileAPI

VRCアセットファイルのAPI機能を提供します。

#### 名前空間
```csharp
using AMU.Editor.VrcAssetManager.API;
```

#### 主要機能

##### ディレクトリスキャン
```csharp
public static List<string> ScanDirectory(string directoryPath, bool recursive = true)
```

指定されたディレクトリ内のすべてのファイルをスキャンします。ファイル形式による制限はありません。

**パラメータ:**
- `directoryPath`: スキャンするディレクトリパス
- `recursive`: サブディレクトリも含めるかどうか（デフォルト: true）

**使用例:**
```csharp
// 再帰的スキャン（すべてのファイル）
var files = VrcAssetFileAPI.ScanDirectory(@"C:\VRCAssets", true);

// 単一ディレクトリのみスキャン
var filesInRoot = VrcAssetFileAPI.ScanDirectory(@"C:\VRCAssets", false);
```

### AssetValidationController

アセットの包括的なバリデーション機能を提供します。

#### 名前空間
```csharp
using AMU.Editor.VrcAssetManager.Controllers;
using AMU.Editor.VrcAssetManager.Schema;
```

#### 主要機能

##### アセット全体の検証
```csharp
public static ValidationResults ValidateAsset(AssetSchema asset, IReadOnlyDictionary<AssetId, AssetGroupSchema> allGroups = null)
```

アセット全体の包括的な検証を実行します。

**パラメータ:**
- `asset`: 検証対象のアセット
- `allGroups`: 全グループ情報（グループ検証用、オプション）

**戻り値:**
- `ValidationResults`: 検証結果

**使用例:**
```csharp
var asset = new AssetSchema();
// ... アセット情報を設定

var results = AssetValidationController.ValidateAsset(asset);

if (results.HasCritical)
{
    Debug.LogError("Critical validation errors found!");
    foreach (var error in results.Results.Where(r => r.Level == ValidationLevel.Critical))
    {
        Debug.LogError($"Field: {error.FieldName}, Message: {error.Message}");
    }
}
```

##### ライブラリ全体の検証
```csharp
public static ValidationResults ValidateLibrary(AssetLibrarySchema library)
```

ライブラリ全体の検証を実行します。重複チェックや整合性チェックを含みます。

##### 個別コンポーネント検証
```csharp
// アセットIDの検証
public static ValidationResults ValidateAssetId(AssetId assetId)

// メタデータの検証（名前、説明、作者名、タグなど）
public static ValidationResults ValidateMetadata(AssetMetadata metadata)

// ファイル情報の検証（パス、サイズ、サムネイルなど）
public static ValidationResults ValidateFileInfo(AssetFileInfo fileInfo)

// アセットタイプの検証
public static ValidationResults ValidateAssetType(AssetType assetType)

// グループ情報の検証（循環参照チェックなど）
public static ValidationResults ValidateGroupSchema(AssetGroupSchema group, IReadOnlyDictionary<AssetId, AssetGroupSchema> allGroups)

// Booth情報の検証
public static ValidationResults ValidateBoothItem(BoothItemSchema boothItem)
```

### SearchCriteriaController

検索条件の生成と管理を行う統合コントローラです。

#### 名前空間
```csharp
using AMU.Editor.VrcAssetManager.Controllers;
using AMU.Editor.VrcAssetManager.Schema;
```

#### 主要機能

##### 日付範囲の生成
```csharp
// 基本的な範囲
var last7Days = SearchCriteriaController.DateRangeFactory.LastDays(7);
var lastMonth = SearchCriteriaController.DateRangeFactory.LastMonths(1);
var lastYear = SearchCriteriaController.DateRangeFactory.LastYears(1);

// 特定期間
var today = SearchCriteriaController.DateRangeFactory.Today();
var thisWeek = SearchCriteriaController.DateRangeFactory.ThisWeek();
var thisMonth = SearchCriteriaController.DateRangeFactory.ThisMonth();
var thisYear = SearchCriteriaController.DateRangeFactory.ThisYear();

// 無効化
var disabled = SearchCriteriaController.DateRangeFactory.Disabled;
```

##### ファイルサイズ範囲の生成
```csharp
// 定義済みサイズ
var small = SearchCriteriaController.FileSizeRangeFactory.Small();      // 1MB未満
var medium = SearchCriteriaController.FileSizeRangeFactory.Medium();    // 1MB-10MB
var large = SearchCriteriaController.FileSizeRangeFactory.Large();      // 10MB-100MB
var veryLarge = SearchCriteriaController.FileSizeRangeFactory.VeryLarge(); // 100MB以上

// カスタム範囲
var customMB = SearchCriteriaController.FileSizeRangeFactory.CustomMB(5, 50);
var upTo10MB = SearchCriteriaController.FileSizeRangeFactory.UpTo(new FileSize(10 * 1024 * 1024));
var atLeast100MB = SearchCriteriaController.FileSizeRangeFactory.AtLeast(new FileSize(100 * 1024 * 1024));
```