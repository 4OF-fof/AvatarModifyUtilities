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
public static bool AddAsset(AssetId assetId, AssetSchema assetData)
```

新しいVRCアセットをキャッシュに追加します。

**パラメータ:**
- `assetId`: アセットの一意識別子
- `assetData`: 追加するアセットデータ

**戻り値:**
- `bool`: 追加に成功した場合true

**使用例:**
```csharp
var assetId = AssetId.NewId();
var assetData = new AssetSchema("MyAvatar", AssetType.Avatar, "Assets/MyAvatar.prefab");
bool success = VrcAssetController.AddAsset(assetId, assetData);
if (success)
{
    Debug.Log($"アセットが正常に追加されました: {assetId}");
}
```

##### アセットの更新
```csharp
public static bool UpdateAsset(AssetId assetId, AssetSchema assetData)
```

既存のVRCアセットを更新します。

**パラメータ:**
- `assetId`: 更新するアセットのID
- `assetData`: 更新するアセットデータ

**戻り値:**
- `bool`: 更新に成功した場合true

**使用例:**
```csharp
var existingAsset = VrcAssetController.GetAsset(assetId);
if (existingAsset != null)
{
    existingAsset.Metadata.Description = "更新された説明";
    VrcAssetController.UpdateAsset(assetId, existingAsset);
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

##### アセット・グループの状態判定
```csharp
public static bool HasBoothItem(AssetId assetId)
public static bool HasBoothItem(AssetSchema asset)
public static bool IsTopLevel(AssetId assetId)
public static bool IsTopLevel(AssetSchema asset)
public static bool IsTopLevel(AssetGroupSchema group)
```

**使用例:**
```csharp
// BoothアイテムがあるかどうかをIDで判定
bool hasBoothItem = VrcAssetController.HasBoothItem(assetId);

// Boothアイテムがあるかどうかをアセットデータで判定
var asset = VrcAssetController.GetAsset(assetId);
bool hasBoothItem = VrcAssetController.HasBoothItem(asset);

// トップレベルアイテムかどうかをIDで判定
bool isTopLevel = VrcAssetController.IsTopLevel(assetId);

// トップレベルアイテムかどうかをアセットデータで判定
bool isTopLevel = VrcAssetController.IsTopLevel(asset);

// トップレベルグループかどうかを判定
bool isTopLevel = VrcAssetController.IsTopLevel(groupData);
```

##### ファイルサイズの文字列変換
```csharp
public static string FormatFileSize(long bytes)
public static string GetFormattedFileSize(AssetId assetId)
public static string GetFormattedFileSize(AssetSchema asset)
```

**詳細:**
- `FormatFileSize`: バイト数を人間が読みやすい形式（B, KB, MB, GB）に変換
- `GetFormattedFileSize(AssetId)`: 指定したアセットIDのファイルサイズを取得してフォーマット
- `GetFormattedFileSize(AssetSchema)`: アセットデータからファイルサイズをフォーマット

**使用例:**
```csharp
// バイト数を直接変換
string size1 = VrcAssetController.FormatFileSize(1024); // "1.0 KB"
string size2 = VrcAssetController.FormatFileSize(1048576); // "1.0 MB"
string size3 = VrcAssetController.FormatFileSize(1073741824); // "1.0 GB"

// アセットのファイルサイズを取得
var asset = VrcAssetController.GetAsset(assetId);
string formattedSize = VrcAssetController.GetFormattedFileSize(asset);
Debug.Log($"ファイルサイズ: {formattedSize}");

// IDから直接取得
string sizeFromId = VrcAssetController.GetFormattedFileSize(assetId);
```

##### グループの状態判定
```csharp
public static bool HasParent(AssetGroupSchema group)
public static bool HasChildren(AssetGroupSchema group)
public static bool IsTopLevel(AssetGroupSchema group)
```

**詳細:**
- `HasParent`: グループが親グループを持っているかを判定（ParentGroupIdの有無をチェック）
- `HasChildren`: グループが子アセットを持っているかを判定（ChildAssetIdsの要素数をチェック）
- `IsTopLevel`: グループがトップレベル（親なし）かを判定（HasParentの逆）

**使用例:**
```csharp
// グループの親子関係を判定
var group = librarySchema.GetGroup(groupId);

bool hasParent = VrcAssetController.HasParent(group);
if (hasParent)
{
    Debug.Log($"親グループID: {group.ParentGroupId}");
}

bool hasChildren = VrcAssetController.HasChildren(group);
Debug.Log($"子アセット数: {group.ChildAssetIds.Count}");

bool isTopLevel = VrcAssetController.IsTopLevel(group);
bool isLeaf = !VrcAssetController.HasChildren(group); // リーフ判定

// 条件分岐での活用
if (VrcAssetController.IsTopLevel(group) && VrcAssetController.HasChildren(group))
{
    Debug.Log("トップレベルで子を持つグループです");
}
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

指定されたファイルパスからVRCアセットデータを作成します。

**使用例:**
```csharp
// ファイルのインポート
var assetId = AssetId.NewId();
var assetData = VrcAssetFileController.ImportAssetFile(@"C:\Assets\MyAvatar.prefab");
if (assetData != null)
{
    VrcAssetController.AddAsset(assetId, assetData);
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

ファイル容量や更新日時の情報を更新します。

**使用例:**

```csharp
var asset = VrcAssetController.GetAsset(assetId);
var refreshedAsset = VrcAssetFileController.RefreshAssetFileInfo(asset);
VrcAssetController.UpdateAsset(assetId, refreshedAsset);
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
public static ValidationResults ValidateAsset(AssetSchema asset, IReadOnlyDictionary<string, AssetGroupSchema> allGroups = null)
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
// メタデータの検証（名前、説明、作者名、タグなど）
public static ValidationResults ValidateMetadata(AssetMetadata metadata)

// ファイル情報の検証（パス、サイズ、サムネイルなど）
public static ValidationResults ValidateFileInfo(AssetFileInfo fileInfo)

// グループ情報の検証（循環参照チェックなど）
public static ValidationResults ValidateGroupSchema(AssetGroupSchema group, IReadOnlyDictionary<string, AssetGroupSchema> allGroups)

// Booth情報の検証
public static ValidationResults ValidateBoothItem(BoothItemSchema boothItem)
```