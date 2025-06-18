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

新しいVRCアセットをライブラリに追加します。

**パラメータ:**
- `assetId`: アセットの一意識別子
- `assetData`: 追加するアセットデータ

**戻り値:**
- `bool`: 追加に成功した場合true

**特徴:**
- ライブラリに自動保存（非同期）
- 重複チェック機能付き

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

**特徴:**
- ライブラリに自動保存（非同期）
- 存在チェック機能付き

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

**特徴:**
- ライブラリから自動削除（非同期保存）
- 存在チェック機能付き

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

**特徴:**
- ライブラリキャッシュを活用した高速取得
- リアルタイムフィルタリング（インデックス不要）

**使用例:**
```csharp
// 特定のアセットを取得
var asset = VrcAssetController.GetAsset(assetId);

// 全アセットを取得
var allAssets = VrcAssetController.GetAllAssets();

// カテゴリ別に取得（リアルタイムフィルタリング）
var avatars = VrcAssetController.GetAssetsByCategory("Avatar");

// 作者別に取得（リアルタイムフィルタリング）
var authorAssets = VrcAssetController.GetAssetsByAuthor("AuthorName");

//名前で検索（リアルタイム検索）
var searchResults = VrcAssetController.SearchAssets("AwesomeAvatar");
```

##### アセットの状態判定
```csharp
public static bool HasBoothItem(AssetId assetId)
public static bool HasBoothItem(AssetSchema asset)
public static bool IsTopLevel(AssetId assetId)
public static bool IsTopLevel(AssetSchema asset)
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
```

##### グループの状態判定
```csharp
public static bool HasParent(AssetGroupSchema group)
public static bool HasChildren(AssetGroupSchema group)
```

**詳細:**
- `HasParent`: グループが親グループを持っているかを判定（ParentGroupIdの有無をチェック）
- `HasChildren`: グループが子アセットを持っているかを判定（ChildAssetIdsの要素数をチェック）

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

##### ファイルサイズの文字列変換
```csharp
public static string FormatFileSize(long bytes)
public static string GetFormattedFileSize(AssetSchema asset)
public static string GetFormattedFileSize(AssetId assetId)
```

**詳細:**
- `FormatFileSize`: バイト数を人間が読みやすい形式（B, KB, MB, GB）に変換
- `GetFormattedFileSize(AssetSchema)`: アセットデータからファイルサイズをフォーマット
- `GetFormattedFileSize(AssetId)`: 指定したアセットIDのファイルサイズを取得してフォーマット

**使用例:**
```csharp
// バイト数を直接変換
string size1 = VrcAssetFileController.FormatFileSize(1024); // "1.0 KB"
string size2 = VrcAssetFileController.FormatFileSize(1048576); // "1.0 MB"
string size3 = VrcAssetFileController.FormatFileSize(1073741824); // "1.0 GB"

// アセットのファイルサイズを取得
var asset = VrcAssetController.GetAsset(assetId);
string formattedSize = VrcAssetFileController.GetFormattedFileSize(asset);
Debug.Log($"ファイルサイズ: {formattedSize}");

// IDから直接取得
string sizeFromId = VrcAssetFileController.GetFormattedFileSize(assetId);
Debug.Log($"ファイルサイズ: {sizeFromId}");
```

##### サポートされているファイル拡張子の取得
```csharp
public static string[] GetSupportedFileExtensions()
```

VRCアセットとしてサポートされているファイル拡張子の一覧を取得します。

**使用例:**
```csharp
var extensions = VrcAssetFileController.GetSupportedFileExtensions();
foreach (var ext in extensions)
{
    Debug.Log($"サポートされている拡張子: {ext}");
}
```

### AssetLibraryController

AssetLibraryのJSONファイルの読み書きを担当するコントローラです。

#### 名前空間
```csharp
using AMU.Editor.VrcAssetManager.Controllers;
using AMU.Editor.VrcAssetManager.Schema;
```

#### キャッシュシステム

AssetLibraryControllerは内部でライブラリをメモリにキャッシュし、ファイルの最終更新時刻を監視してキャッシュの有効性を管理します。これにより頻繁なアクセスでもファイルIOを最小限に抑えます。

**キャッシュの特徴:**
- ライブラリ全体をメモリに保存
- ファイル更新時刻の監視による自動無効化
- スレッドセーフな実装
- 明示的なキャッシュクリア機能

#### 主要機能

##### キャッシュ管理
```csharp
public static void ClearCache()
public static bool IsCached(string filePath = null)
```

**ClearCache():**
キャッシュをクリアして次回アクセス時に強制的にファイルから読み込みます。

**IsCached():**
指定されたファイルがキャッシュされているかを確認します。

**使用例:**
```csharp
// キャッシュが有効かチェック
if (AssetLibraryController.IsCached())
{
    Debug.Log("ライブラリはキャッシュから読み込まれます");
}

// キャッシュをクリア
AssetLibraryController.ClearCache();
```

##### 強制再読み込み
```csharp
public static AssetLibrarySchema ForceReloadLibrary(string filePath = null)
```

キャッシュを無視してライブラリを強制的に再読み込みします。

**パラメータ:**
- `filePath`: 読み込み元ファイルパス（nullの場合はDefaultLibraryPathを使用）

**戻り値:**
- `AssetLibrarySchema`: 読み込んだライブラリ（失敗時はnull）

**使用例:**
```csharp
// 外部でファイルが変更された可能性がある場合
var library = AssetLibraryController.ForceReloadLibrary();
```

##### 新しいライブラリの作成
```csharp
public static AssetLibrarySchema CreateNewLibrary()
```

新しい空のAssetLibrarySchemaを作成します。

**戻り値:**
- `AssetLibrarySchema`: 新しいライブラリインスタンス（失敗時はnull）

**使用例:**
```csharp
var newLibrary = AssetLibraryController.CreateNewLibrary();
if (newLibrary != null)
{
    Debug.Log("新しいライブラリが作成されました");
}
```

##### ライブラリの保存
```csharp
public static bool SaveLibrary(AssetLibrarySchema library, string filePath = null)
public static bool SaveLibraryAsync(AssetLibrarySchema library, string filePath = null)
```

**SaveLibrary():**
AssetLibraryをJSONファイルに同期的に保存します。

**SaveLibraryAsync():**
AssetLibraryを非同期で保存します。キャッシュは即座に更新され、ファイル書き込みはバックグラウンドで実行されます。

**パラメータ:**
- `library`: 保存するAssetLibrarySchema
- `filePath`: 保存先ファイルパス（nullの場合はDefaultLibraryPathを使用）

**戻り値:**
- `bool`: 保存に成功した場合true

**特徴:**
- 保存時に自動的にLastUpdatedとキャッシュを更新
- ディレクトリが存在しない場合は自動作成
- 適切なJSON設定で整形済みファイルを出力

**使用例:**
```csharp
var library = AssetLibraryController.CreateNewLibrary();
// ... ライブラリにアセットを追加

// 同期保存（処理完了まで待機）
bool saved = AssetLibraryController.SaveLibrary(library);

// 非同期保存（高速、UIをブロックしない）
bool saveStarted = AssetLibraryController.SaveLibraryAsync(library);

// 指定パスに保存
bool savedToCustomPath = AssetLibraryController.SaveLibrary(library, @"C:\MyLibrary.json");
```

##### ライブラリの読み込み
```csharp
public static AssetLibrarySchema LoadLibrary(string filePath = null)
```

JSONファイルからAssetLibraryを読み込みます。キャッシュが有効な場合はキャッシュから返します。

**パラメータ:**
- `filePath`: 読み込み元ファイルパス（nullの場合はDefaultLibraryPathを使用）

**戻り値:**
- `AssetLibrarySchema`: 読み込んだライブラリ（失敗時は新しい空のライブラリを返す）

**特徴:**
- **キャッシュ機能**: 同一ファイルで変更がない場合はキャッシュから高速取得
- ファイルが存在しない場合は新しいライブラリを作成
- JSON解析エラー時は新しいライブラリを作成してエラーログを出力
- 読み込み成功時はアセット数とグループ数をログ出力

**使用例:**
```csharp
// デフォルトパスから読み込み（キャッシュ有効）
var library = AssetLibraryController.LoadLibrary();

// 指定パスから読み込み
var customLibrary = AssetLibraryController.LoadLibrary(@"C:\MyLibrary.json");

// 強制再読み込み（キャッシュ無視）
var reloadedLibrary = AssetLibraryController.ForceReloadLibrary();

Debug.Log($"読み込み完了: アセット数={library.AssetCount}, グループ数={library.GroupCount}");
```

##### ファイル存在確認
```csharp
public static bool LibraryFileExists(string filePath = null)
```

ライブラリファイルが存在するかを確認します。

**パラメータ:**
- `filePath`: 確認するファイルパス（nullの場合はDefaultLibraryPathを使用）

**戻り値:**
- `bool`: ファイルが存在する場合true

**使用例:**
```csharp
if (AssetLibraryController.LibraryFileExists())
{
    var library = AssetLibraryController.LoadLibrary();
}
else
{
    var library = AssetLibraryController.CreateNewLibrary();
}
```

##### ファイル情報の取得
```csharp
public static FileInfo GetLibraryFileInfo(string filePath = null)
```

ライブラリファイルの詳細情報を取得します。

**パラメータ:**
- `filePath`: 対象ファイルパス（nullの場合はDefaultLibraryPathを使用）

**戻り値:**
- `FileInfo`: ファイル情報（存在しない場合はnull）

**使用例:**
```csharp
var fileInfo = AssetLibraryController.GetLibraryFileInfo();
if (fileInfo != null)
{
    Debug.Log($"ファイルサイズ: {fileInfo.Length:N0} bytes");
    Debug.Log($"最終更新: {fileInfo.LastWriteTime:yyyy/MM/dd HH:mm:ss}");
}
```

##### デフォルトパスの取得
```csharp
public static string DefaultLibraryPath { get; }
```

デフォルトのライブラリファイルパスを取得します。

**戻り値:**
- `string`: `{Application.dataPath}/AssetLibrary.json`の絶対パス

**使用例:**
```csharp
Debug.Log($"デフォルトライブラリパス: {AssetLibraryController.DefaultLibraryPath}");
```