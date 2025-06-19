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
public static List<AssetSchema> GetAssetsByName(string searchTerm)
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

//名前で取得（リアルタイム検索）
var searchResults = VrcAssetController.GetAssetsByName("AwesomeAvatar");
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
public static AssetSchema UpdateAssetFileInfo(AssetSchema assetData)
```

ファイル容量や更新日時の情報を更新します。

**使用例:**

```csharp
var asset = VrcAssetController.GetAsset(assetId);
var refreshedAsset = VrcAssetFileController.UpdateAssetFileInfo(asset);
VrcAssetController.UpdateAsset(assetId, refreshedAsset);
```

##### ファイルサイズの文字列変換
```csharp
public static string ConvertBytesToString(long bytes)
public static string GetFormattedFileSize(AssetSchema asset)
public static string GetFormattedFileSize(AssetId assetId)
```

**詳細:**
- `ConvertBytesToString`: バイト数を人間が読みやすい形式（B, KB, MB, GB）に変換
- `GetFormattedFileSize(AssetSchema)`: アセットデータからファイルサイズをフォーマット
- `GetFormattedFileSize(AssetId)`: 指定したアセットIDのファイルサイズを取得してフォーマット

**使用例:**
```csharp
// バイト数を直接変換
string size1 = VrcAssetFileController.ConvertBytesToString(1024); // "1.0 KB"
string size2 = VrcAssetFileController.ConvertBytesToString(1048576); // "1.0 MB"
string size3 = VrcAssetFileController.ConvertBytesToString(1073741824); // "1.0 GB"

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

#### 設定連携

AssetLibraryControllerは以下の設定システムと連携します：

**EditorPrefs設定:**
- `Setting.Core_dirPath`: ライブラリファイルの保存先ディレクトリ
- デフォルト値: `%USERPROFILE%\Documents\AvatarModifyUtilities`
- ファイルパス: `{CoreDir}/VrcAssetManager/VrcAssetLibrary.json`

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
```

**SaveLibrary():**
AssetLibraryをJSONファイルに非同期で保存します。キャッシュは即座に更新され、ファイル書き込みはバックグラウンドで実行されます。

**パラメータ:**
- `library`: 保存するAssetLibrarySchema
- `filePath`: 保存先ファイルパス（nullの場合はDefaultLibraryPathを使用）

**戻り値:**
- `bool`: 保存処理を開始できた場合true

**特徴:**
- **非同期処理**: UIをブロックせずにバックグラウンドで保存
- **即座にキャッシュ更新**: 保存処理開始時にキャッシュを即座に更新
- **自動ディレクトリ作成**: ディレクトリが存在しない場合は自動作成
- **適切なJSON設定**: 整形済みファイルを出力
- **自動LastUpdated更新**: 保存時に最終更新日時を自動設定

**使用例:**
```csharp
var library = AssetLibraryController.CreateNewLibrary();
// ... ライブラリにアセットを追加

// 非同期保存
bool saveStarted = AssetLibraryController.SaveLibrary(library);

// 指定パスに保存
bool savedToCustomPath = AssetLibraryController.SaveLibrary(library, @"C:\MyLibrary.json");

if (saveStarted)
{
    Debug.Log("保存処理を開始しました");
}
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

##### タグ管理
```csharp
public static bool AddTag(string tag, string filePath = null)
public static bool RemoveTag(string tag, string filePath = null)
public static bool ClearTags(string filePath = null)
```

**AddTag():**
ライブラリにタグを追加します。重複する場合は追加されません。

**RemoveTag():**
ライブラリからタグを削除します。

**ClearTags():**
ライブラリのすべてのタグをクリアします。

**パラメータ:**
- `tag`: 対象のタグ（AddTag/RemoveTagのみ）
- `filePath`: 対象ライブラリファイルパス（nullの場合はDefaultLibraryPathを使用）

**戻り値:**
- `bool`: 操作に成功した場合true

**使用例:**
```csharp
// タグを追加
bool tagAdded = AssetLibraryController.AddTag("Avatar");
bool clothingAdded = AssetLibraryController.AddTag("Clothing");

// タグを削除
bool tagRemoved = AssetLibraryController.RemoveTag("Avatar");

// すべてのタグをクリア
bool tagsCleared = AssetLibraryController.ClearTags();
```

##### アセットタイプ管理
```csharp
public static bool AddAssetType(string assetType, string filePath = null)
public static bool RemoveAssetType(string assetType, string filePath = null)
public static bool ClearAssetTypes(string filePath = null)
```

**AddAssetType():**
ライブラリにアセットタイプを追加します。重複する場合は追加されません。

**RemoveAssetType():**
ライブラリからアセットタイプを削除します。

**ClearAssetTypes():**
ライブラリのすべてのアセットタイプをクリアします。

**パラメータ:**
- `assetType`: 対象のアセットタイプ（AddAssetType/RemoveAssetTypeのみ）
- `filePath`: 対象ライブラリファイルパス（nullの場合はDefaultLibraryPathを使用）

**戻り値:**
- `bool`: 操作に成功した場合true

**使用例:**
```csharp
// アセットタイプを追加
bool avatarTypeAdded = AssetLibraryController.AddAssetType("Avatar");
bool prefabTypeAdded = AssetLibraryController.AddAssetType("Prefab");

// アセットタイプを削除
bool typeRemoved = AssetLibraryController.RemoveAssetType("Avatar");

// すべてのアセットタイプをクリア
bool typesCleared = AssetLibraryController.ClearAssetTypes();
```

##### 同期・最適化機能
```csharp
public static bool SynchronizeTagsFromAssets(string filePath = null)
public static bool SynchronizeAssetTypesFromAssets(string filePath = null)
public static bool CleanupUnusedTags(string filePath = null)
public static bool CleanupUnusedAssetTypes(string filePath = null)
public static bool OptimizeLibrary(string filePath = null)
```

**SynchronizeTagsFromAssets():**
アセット内で使用されているタグを収集してライブラリのタグリストに自動追加します。

**SynchronizeAssetTypesFromAssets():**
アセット内で使用されているアセットタイプを収集してライブラリのアセットタイプリストに自動追加します。

**CleanupUnusedTags():**
アセットで使用されていないタグをライブラリから削除します。

**CleanupUnusedAssetTypes():**
アセットで使用されていないアセットタイプをライブラリから削除します。

**OptimizeLibrary():**
上記の同期・クリーンアップ処理をすべて実行してライブラリを最適化します。

**パラメータ:**
- `filePath`: 対象ライブラリファイルパス（nullの場合はDefaultLibraryPathを使用）

**戻り値:**
- `bool`: 操作に成功した場合true

**使用例:**
```csharp
// アセットからタグを同期
bool tagsSynced = AssetLibraryController.SynchronizeTagsFromAssets();

// アセットからアセットタイプを同期
bool typesSynced = AssetLibraryController.SynchronizeAssetTypesFromAssets();

// 未使用のタグを削除
bool tagsCleanedUp = AssetLibraryController.CleanupUnusedTags();

// 未使用のアセットタイプを削除
bool typesCleanedUp = AssetLibraryController.CleanupUnusedAssetTypes();

// ライブラリ全体を最適化
bool optimized = AssetLibraryController.OptimizeLibrary();

if (optimized)
{
    Debug.Log("ライブラリが最適化されました");
}
```

##### ファイル存在確認
```csharp
public static bool HasLibraryFile(string filePath = null)
```

ライブラリファイルが存在するかを確認します。

**パラメータ:**
- `filePath`: 確認するファイルパス（nullの場合はDefaultLibraryPathを使用）

**戻り値:**
- `bool`: ファイルが存在する場合true

**使用例:**
```csharp
if (AssetLibraryController.HasLibraryFile())
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
- `string`: `{CoreDir}/VrcAssetManager/VrcAssetLibrary.json`の絶対パス

**パス構成:**
- CoreDir: EditorPrefsの`Setting.Core_dirPath`から取得（デフォルト: `%USERPROFILE%\Documents\AvatarModifyUtilities`）
- ファイル名: `VrcAssetLibrary.json`

**使用例:**
```csharp
Debug.Log($"デフォルトライブラリパス: {AssetLibraryController.DefaultLibraryPath}");
// 出力例: C:\Users\YourName\Documents\AvatarModifyUtilities\VrcAssetManager\VrcAssetLibrary.json
```