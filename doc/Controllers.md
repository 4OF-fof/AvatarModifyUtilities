# Controllers 一覧

## Core Controllers

### SettingsController

EditorPrefsを使用した設定データの管理を行います。

#### 名前空間
```csharp
using AMU.Editor.Core.Controllers;
```

#### 主要機能

##### 設定の初期化
```csharp
public static void InitializeEditorPrefs()
```

全ての設定項目を検索し、未設定の項目に対してデフォルト値を設定します。
通常は自動実行されるため、手動呼び出しは不要です。

**使用例:**
```csharp
using AMU.Editor.Core.Controllers;

// 通常は InitializationService により自動実行される
SettingsController.InitializeEditorPrefs();
```

##### 設定値の取得
```csharp
public static T GetSetting<T>(string settingName, T defaultValue = default(T))
```

**パラメータ:**
- `settingName`: 設定項目名
- `defaultValue`: デフォルト値

**戻り値:**
- `T`: 設定値（型安全）

**対応型:**
- `string`
- `int`
- `bool`
- `float`

**使用例:**
```csharp
using AMU.Editor.Core.Controllers;

// 文字列設定の取得
var language = SettingsController.GetSetting<string>("Core_language", "ja_jp");

// 整数設定の取得
var maxItems = SettingsController.GetSetting<int>("AssetManager_maxDisplayItems", 100);

// ブール設定の取得
var autoVariantEnabled = SettingsController.GetSetting<bool>("AutoVariant_enableAutoVariant", false);
var prebuildEnabled = SettingsController.GetSetting<bool>("AutoVariant_enablePrebuild", true);

// フロート設定の取得
var thumbnailSize = SettingsController.GetSetting<float>("AssetManager_thumbnailSize", 128.0f);
```

##### 設定値の保存
```csharp
public static void SetSetting<T>(string settingName, T value)
```

**パラメータ:**
- `settingName`: 設定項目名
- `value`: 保存する値

**使用例:**
```csharp
using AMU.Editor.Core.Controllers;

// 設定値の保存
SettingsController.SetSetting("Core_language", "en_us");
SettingsController.SetSetting("AssetManager_maxDisplayItems", 200);
SettingsController.SetSetting("AutoVariant_enableAutoVariant", true);
SettingsController.SetSetting("AutoVariant_enablePrebuild", false);
```

##### 設定の存在確認
```csharp
public static bool HasSetting(string settingName)
```

**パラメータ:**
- `settingName`: 確認する設定項目名

**戻り値:**
- `bool`: 設定が存在する場合`true`

**使用例:**
```csharp
using AMU.Editor.Core.Controllers;

if (SettingsController.HasSetting("CustomSetting"))
{
    var value = SettingsController.GetSetting<string>("CustomSetting");
    Debug.Log($"カスタム設定: {value}");
}
```

##### 設定の削除
```csharp
public static void DeleteSetting(string settingName)
```

**パラメータ:**
- `settingName`: 削除する設定項目名

**使用例:**
```csharp
using AMU.Editor.Core.Controllers;

// 不要になった設定の削除
SettingsController.DeleteSetting("ObsoleteSetting");
```

#### 設定キーの形式

設定キーは `"Setting.{settingName}"` の形式でEditorPrefsに保存されます。

**例:**
- `"Setting.Core_language"`
- `"Setting.AutoVariant_enableAutoVariant"`
- `"Setting.AssetManager_maxDisplayItems"`

### LocalizationController

多言語化機能の管理を行います。言語ファイルの読み込み、テキストの提供、言語切り替えを担当します。

#### 名前空間
```csharp
using AMU.Editor.Core.Controllers;
```

#### 主要機能

##### 言語の読み込み
```csharp
public static void LoadLanguage(string languageCode)
```

**パラメータ:**
- `languageCode`: 言語コード（例: "ja_jp", "en_us"）

**動作:**
1. 指定された言語コードのJSONファイルを検索
2. 複数のファイルをマージ
3. メモリに読み込み
4. 指定された言語が英語以外の場合、英語のフォールバックテキストも自動的に読み込み

**使用例:**
```csharp
using AMU.Editor.Core.Controllers;

// 日本語に切り替え
LocalizationController.LoadLanguage("ja_jp");

// 英語に切り替え
LocalizationController.LoadLanguage("en_us");

// 設定から現在の言語を読み込み
var currentLang = SettingsController.GetSetting<string>("Core_language", "ja_jp");
LocalizationController.LoadLanguage(currentLang);
```

##### テキストの取得
```csharp
public static string GetText(string key)
```

**パラメータ:**
- `key`: テキストキー

**戻り値:**
- `string`: ローカライズされたテキスト

**フォールバック動作:**
1. 現在の言語のテキストを検索
2. 見つからない場合は英語のフォールバックテキストを検索
3. それでも見つからない場合はキーをそのまま返す

**使用例:**
```csharp
using AMU.Editor.Core.Controllers;

// UIテキストの取得
var saveButtonText = LocalizationController.GetText("ui_button_save");
var cancelButtonText = LocalizationController.GetText("ui_button_cancel");
var errorMessage = LocalizationController.GetText("error_file_not_found");

// AutoVariant設定項目のテキスト
var autoVariantLabel = LocalizationController.GetText("AutoVariant_enableAutoVariant");
var prebuildLabel = LocalizationController.GetText("AutoVariant_enablePrebuild");
```

##### 現在の言語
```csharp
public static string CurrentLanguage { get; private set; }
```

現在読み込まれている言語コードを取得します。

**使用例:**
```csharp
using AMU.Editor.Core.Controllers;

var currentLang = LocalizationController.CurrentLanguage;
Debug.Log($"現在の言語: {currentLang}");

// 言語に応じた処理
if (currentLang == "ja_jp")
{
    Debug.Log("日本語モードです");
}
```

##### ユーティリティメソッド
```csharp
public static int GetLoadedTextCount()      // 読み込み済みテキスト数
public static int GetFallbackTextCount()   // フォールバックテキスト数
public static bool HasKey(string key)      // キーの存在確認
```

**使用例:**
```csharp
using AMU.Editor.Core.Controllers;

// デバッグ情報の表示
Debug.Log($"読み込み済みテキスト数: {LocalizationController.GetLoadedTextCount()}");
Debug.Log($"フォールバックテキスト数: {LocalizationController.GetFallbackTextCount()}");

// キーの存在確認
if (LocalizationController.HasKey("ui_button_save"))
{
    var text = LocalizationController.GetText("ui_button_save");
    Debug.Log($"保存ボタンテキスト: {text}");
}
```

## VrcAssetManager Controllers

VRCアセット管理に特化したコントローラ群です。VRChatアバターやワールド開発に必要なアセットの管理機能を提供します。

### VrcAssetController

VRCアセットの管理を行うメインコントローラです。AssetLibraryControllerと連携してアセットの CRUD 操作を提供します。

#### 名前空間
```csharp
using AMU.Editor.VrcAssetManager.Controllers;
using AMU.Editor.VrcAssetManager.Schema;
```

#### アーキテクチャ

- すべてのアセット操作はAssetLibraryControllerを通じて実行
- ライブラリレベルでのキャッシュとファイルIO最適化
- アセット個別のキャッシュは廃止（ライブラリ統一キャッシュに移行）

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
- AssetLibraryControllerを通じてライブラリに保存
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
bool hasBoothItem = VrcAssetController.HasBoothItem(assetData);

// トップレベルアイテムかどうかをIDで判定
bool isTopLevel = VrcAssetController.IsTopLevel(assetId);

// トップレベルアイテムかどうかをアセットデータで判定
bool isTopLevel = VrcAssetController.IsTopLevel(assetData);


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

AssetLibraryのJSONファイルの読み書きを担当するコントローラです。ライブラリの永続化を管理します。

#### 名前空間
```csharp
using AMU.Editor.VrcAssetManager.Controllers;
using AMU.Editor.VrcAssetManager.Schema;
```

#### 主要機能

##### キャッシュ管理
```csharp
public static void ClearCache()
public static bool IsCached(string filePath = null)
public static AssetLibrarySchema ForceReloadLibrary(string filePath = null)
```

**使用例:**
```csharp
// キャッシュの状態確認
if (AssetLibraryController.IsCached())
{
    Debug.Log("ライブラリはキャッシュ済みです");
}

// キャッシュをクリア
AssetLibraryController.ClearCache();

// キャッシュを無視して強制再読み込み
var library = AssetLibraryController.ForceReloadLibrary();
```

##### ライブラリ管理とタグ・アセットタイプ操作
```csharp
public static bool AddTag(string tag, string filePath = null)
public static bool RemoveTag(string tag, string filePath = null)
public static bool AddAssetType(string assetType, string filePath = null)
public static bool RemoveAssetType(string assetType, string filePath = null)
public static bool ClearTags(string filePath = null)
public static bool ClearAssetTypes(string filePath = null)
```

**使用例:**
```csharp
// タグの追加と削除
bool tagAdded = AssetLibraryController.AddTag("VRChat");
bool tagRemoved = AssetLibraryController.RemoveTag("Deprecated");

// アセットタイプの追加と削除
bool typeAdded = AssetLibraryController.AddAssetType("CustomAvatar");
bool typeRemoved = AssetLibraryController.RemoveAssetType("OldType");

// 全タグ・アセットタイプの削除
bool tagsCleared = AssetLibraryController.ClearTags();
bool typesCleared = AssetLibraryController.ClearAssetTypes();
```

##### ライブラリの同期と最適化
```csharp
public static bool SynchronizeTagsFromAssets(string filePath = null)
public static bool SynchronizeAssetTypesFromAssets(string filePath = null)
public static bool CleanupUnusedTags(string filePath = null)
public static bool CleanupUnusedAssetTypes(string filePath = null)
public static bool OptimizeLibrary(string filePath = null)
```

**使用例:**
```csharp
// アセットから使用されているタグとアセットタイプを同期
bool tagsSynced = AssetLibraryController.SynchronizeTagsFromAssets();
bool typesSynced = AssetLibraryController.SynchronizeAssetTypesFromAssets();

// 未使用のタグとアセットタイプを削除
bool unusedTagsRemoved = AssetLibraryController.CleanupUnusedTags();
bool unusedTypesRemoved = AssetLibraryController.CleanupUnusedAssetTypes();

// ライブラリ全体を最適化（未使用要素の削除と同期を一括実行）
bool optimized = AssetLibraryController.OptimizeLibrary();
```

##### ライブラリの作成・保存・読み込み
```csharp
public static AssetLibrarySchema CreateNewLibrary()
public static bool SaveLibrary(AssetLibrarySchema library, string filePath = null)
public static AssetLibrarySchema LoadLibrary(string filePath = null)
```

**使用例:**
```csharp
// 新しいライブラリを作成
var library = AssetLibraryController.CreateNewLibrary();

// アセットを追加
var assetId = AssetId.NewId();
var asset = new AssetSchema("MyAsset", AssetType.Avatar, "path/to/asset.prefab");
library.AddAsset(assetId, asset);

// ライブラリを非同期で保存
bool saveStarted = AssetLibraryController.SaveLibrary(library);

// ライブラリを読み込み
var loadedLibrary = AssetLibraryController.LoadLibrary();
```

##### ファイル管理機能
```csharp
public static bool LibraryFileExists(string filePath = null)
public static FileInfo GetLibraryFileInfo(string filePath = null)
public static string DefaultLibraryPath { get; }
```

**使用例:**
```csharp
// ファイルの存在確認
if (AssetLibraryController.LibraryFileExists())
{
    // ファイル情報を取得
    var fileInfo = AssetLibraryController.GetLibraryFileInfo();
    Debug.Log($"ファイルサイズ: {fileInfo.Length} bytes");
    
    // ライブラリを読み込み
    var library = AssetLibraryController.LoadLibrary();
}

// デフォルトパスを取得
Debug.Log($"デフォルトパス: {AssetLibraryController.DefaultLibraryPath}");
// 出力例: C:\Users\YourName\Documents\AvatarModifyUtilities\VrcAssetManager\VrcAssetLibrary.json
```