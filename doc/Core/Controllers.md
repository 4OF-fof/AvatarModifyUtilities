# Controllers 層ドキュメント

## 概要

Controllers層は、永続データの管理とアクセス制御を担当します。EditorPrefs、JSONファイル、その他の永続化データへの一元的なアクセスを提供します。

## Controllers一覧

### SettingsController

#### 概要
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

**使用例:**
```csharp
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
var language = SettingsController.GetSetting<string>("Language", "ja_jp");

// 整数設定の取得
var maxItems = SettingsController.GetSetting<int>("MaxDisplayItems", 100);

// ブール設定の取得
var enabled = SettingsController.GetSetting<bool>("FeatureEnabled", true);
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
SettingsController.SetSetting("Language", "en_us");
SettingsController.SetSetting("MaxDisplayItems", 200);
SettingsController.SetSetting("FeatureEnabled", false);
```

##### 設定の存在確認
```csharp
public static bool HasSetting(string settingName)
```

##### 設定の削除
```csharp
public static void DeleteSetting(string settingName)
```

#### 内部実装

設定キーは `"Setting.{settingName}"` の形式でEditorPrefsに保存されます。

```csharp
// 例: "Setting.Language", "Setting.MaxDisplayItems"
```

### LocalizationController

#### 概要
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

**使用例:**
```csharp
using AMU.Editor.Core.Controllers;

// 日本語に切り替え
LocalizationController.LoadLanguage("ja_jp");

// 英語に切り替え
LocalizationController.LoadLanguage("en_us");
```

##### テキストの取得
```csharp
public static string GetText(string key)
```

**パラメータ:**
- `key`: テキストキー

**戻り値:**
- `string`: ローカライズされたテキスト（見つからない場合はキーをそのまま返す）

**使用例:**
```csharp
using AMU.Editor.Core.Controllers;

// UIテキストの取得
var saveButtonText = LocalizationController.GetText("ui_button_save");
var errorMessage = LocalizationController.GetText("error_file_not_found");
```

##### 現在の言語
```csharp
public static string CurrentLanguage { get; private set; }
```

**使用例:**
```csharp
var currentLang = LocalizationController.CurrentLanguage;
Debug.Log($"Current language: {currentLang}");
```

##### その他のユーティリティ
```csharp
public static int GetLoadedTextCount()      // 読み込み済みテキスト数
public static bool HasKey(string key)       // キーの存在確認
```

#### 言語ファイル形式

```json
{
  "ui_button_save": "保存",
  "ui_button_cancel": "キャンセル",
  "error_file_not_found": "ファイルが見つかりません",
  "menu_settings": "設定"
}
```

#### ファイル配置

言語ファイルは以下のパターンで検索されます：
```
AvatarModifyUtilities/Editor/**/ja_jp.json
AvatarModifyUtilities/Editor/**/en_us.json
```

## エラーハンドリング

### SettingsController

```csharp
try
{
    SettingsController.InitializeEditorPrefs();
}
catch (System.Exception ex)
{
    Debug.LogError($"Settings initialization failed: {ex.Message}");
}
```

### LocalizationController

```csharp
// 言語ファイル読み込みエラー
LocalizationController.LoadLanguage("invalid_lang");
// → 警告ログが出力され、既存のテキストはクリアされる

// 存在しないキー
var text = LocalizationController.GetText("nonexistent_key");
// → キー名をそのまま返す（"nonexistent_key"）
```

