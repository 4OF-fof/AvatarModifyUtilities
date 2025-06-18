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

VRCアセットの基本的なCRUD操作とキャッシュ管理を行います。

#### 名前空間
```csharp
using AMU.Editor.VrcAssetManager.Controllers;
using AMU.Editor.VrcAssetManager.Schema;
```

#### 主要機能

##### アセット管理
```csharp
public static bool AddAsset(AssetSchema assetData)
public static bool UpdateAsset(AssetSchema assetData)
public static bool RemoveAsset(AssetId assetId)
```

##### アセット取得・検索
```csharp
public static AssetSchema GetAsset(AssetId assetId)
public static List<AssetSchema> GetAllAssets()
public static List<AssetSchema> GetAssetsByCategory(string category)
public static List<AssetSchema> GetAssetsByAuthor(string author)
public static List<AssetSchema> SearchAssets(string searchTerm)
```

**使用例:**
```csharp
// アセットの追加
var asset = new AssetSchema("MyAvatar", AssetType.Avatar, "Assets/MyAvatar.prefab");
VrcAssetController.AddAsset(asset);

// カテゴリ別検索
var avatars = VrcAssetController.GetAssetsByCategory("Avatar");

// テキスト検索
var searchResults = VrcAssetController.SearchAssets("kawaii");
```

### VrcAssetFileController

VRCアセットファイルのインポート・エクスポート操作を管理します。

#### 名前空間
```csharp
using AMU.Editor.VrcAssetManager.Controllers;
```

#### 主要機能

##### ファイル操作
```csharp
public static bool IsValidVrcAssetFile(string filePath)
public static AssetSchema ImportAssetFile(string filePath)
public static List<AssetSchema> ImportMultipleAssetFiles(IEnumerable<string> filePaths)
public static bool ExportAsset(AssetSchema assetData, string destinationPath)
```

##### ディレクトリスキャン
```csharp
public static List<string> ScanDirectory(string directoryPath, bool recursive = true)
```

**サポートファイル形式:**
- Prefabs (`.prefab`), Scenes (`.unity`), Packages (`.unitypackage`)
- Models (`.fbx`, `.obj`)
- Textures (`.png`, `.jpg`, `.jpeg`, `.tga`, `.psd`)
- Materials (`.mat`), Shaders (`.shader`, `.hlsl`, `.cginc`)
- Scripts (`.cs`, `.dll`, `.asmdef`)

**使用例:**
```csharp
// ファイルインポート
var asset = VrcAssetFileController.ImportAssetFile(@"C:\VRCAssets\MyAvatar.prefab");

// ディレクトリスキャン
var files = VrcAssetFileController.ScanDirectory(@"C:\VRCAssets", true);

// 一括インポート
var assets = VrcAssetFileController.ImportMultipleAssetFiles(files);
```

### AssetValidationController

アセットのバリデーション機能を提供します。

#### 名前空間
```csharp
using AMU.Editor.VrcAssetManager.Controllers;
```

#### 主要機能

##### アセット全体の検証
```csharp
public static ValidationResults ValidateAsset(AssetSchema asset, IReadOnlyDictionary<AssetId, AssetGroupSchema> allGroups = null)
```

**使用例:**
```csharp
using AMU.Editor.VrcAssetManager.Controllers;
using AMU.Editor.VrcAssetManager.Schema;

// アセットの検証
var validationResults = AssetValidationController.ValidateAsset(asset);

if (validationResults.HasErrors)
{
    foreach (var result in validationResults.Results)
    {
        Debug.LogError($"[{result.Level}] {result.Message}");
    }
}
```

##### 個別コンポーネントの検証
```csharp
// アセットIDの検証
public static ValidationResults ValidateAssetId(AssetId assetId)

// メタデータの検証
public static ValidationResults ValidateMetadata(AssetMetadata metadata)

// ファイル情報の検証
public static ValidationResults ValidateFileInfo(AssetFileInfo fileInfo)

// アセットタイプの検証
public static ValidationResults ValidateAssetType(AssetType assetType)

// グループ情報の検証
public static ValidationResults ValidateGroupSchema(AssetGroupSchema group, IReadOnlyDictionary<AssetId, AssetGroupSchema> allGroups)

// Booth情報の検証
public static ValidationResults ValidateBoothItem(BoothItemSchema boothItem)
```

##### ライブラリ全体の検証
```csharp
public static ValidationResults ValidateLibrary(AssetLibrarySchema library)
```

### SearchCriteriaController

検索条件の生成と管理を行います。

#### 名前空間
```csharp
using AMU.Editor.VrcAssetManager.Controllers;
```

#### 主要機能

##### 日付範囲の生成
```csharp
// 過去N日間
var lastWeek = SearchCriteriaController.DateRangeFactory.LastDays(7);

// 過去N月間
var lastMonth = SearchCriteriaController.DateRangeFactory.LastMonths(1);

// 今日
var today = SearchCriteriaController.DateRangeFactory.Today();

// 今週
var thisWeek = SearchCriteriaController.DateRangeFactory.ThisWeek();

// 無効化
var disabled = SearchCriteriaController.DateRangeFactory.Disabled;
```

##### ファイルサイズ範囲の生成
```csharp
// 小さなファイル（1MB未満）
var small = SearchCriteriaController.FileSizeRangeFactory.Small();

// 大きなファイル（10MB-100MB）
var large = SearchCriteriaController.FileSizeRangeFactory.Large();

// カスタム範囲
var custom = SearchCriteriaController.FileSizeRangeFactory.CustomMB(5, 50);

// 指定サイズ以下
var upTo10MB = SearchCriteriaController.FileSizeRangeFactory.UpTo(new FileSize(10 * 1024 * 1024));
```

## 詳細ドキュメント

各コントローラの詳細な使用方法については、以下の専用ドキュメントを参照してください ：

- [Core Controllers詳細](Controllers/Core.md)
- [VrcAssetManager Controllers詳細](Controllers/VrcAssetManager.md)