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
bool hasBoothItem = VrcAssetController.HasBoothItem(assetData);

// トップレベルアイテムかどうかをIDで判定
bool isTopLevel = VrcAssetController.IsTopLevel(assetId);

// トップレベルアイテムかどうかをアセットデータで判定
bool isTopLevel = VrcAssetController.IsTopLevel(assetData);

// トップレベルグループかどうかを判定
bool isTopLevel = VrcAssetController.IsTopLevel(groupData);
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