# Data 層ドキュメント

## 概要

Data層は、AMU Core モジュールにおける具体的なデータ定義を管理します。設定項目の実際の定義、言語ファイル、その他の静的データを提供します。

## ディレクトリ構造

```
Core/Data/
├── Setting.cs              # 設定データ定義
└── lang/                   # 言語ファイル
    ├── ja_jp.json         # 日本語ローカライゼーション
    └── en_us.json         # 英語ローカライゼーション
```

## 依存関係

Data層は以下の層に依存します：

- **Schema層**: データ構造の型定義
- **System.Collections.Generic**: 辞書型などの基本的なコレクション

## Data一覧

### SettingData

#### 概要
AMU Core モジュールの設定項目定義を管理する静的クラスです。すべての設定項目をカテゴリ別に整理して提供します。

#### 名前空間
```csharp
using AMU.Editor.Setting;
using AMU.Editor.Core.Schema;
```

#### 主要プロパティ

##### SettingItems
```csharp
public static readonly Dictionary<string, SettingItem[]> SettingItems
```

設定項目をカテゴリ別に分類した辞書です。

**構造:**
- **キー**: カテゴリ名（例: "Core_general"）
- **値**: そのカテゴリに属する設定項目の配列

#### 現在の設定項目

##### Core_general カテゴリ

**Core_language (言語設定)**
```csharp
new ChoiceSettingItem("Core_language",
    new Dictionary<string, string>
    {
        { "ja_jp", "日本語" },
        { "en_us", "English" },
    }, "ja_jp")
```

- **型**: 選択肢型
- **目的**: UIの表示言語を設定
- **デフォルト**: 日本語（ja_jp）
- **選択肢**: 日本語、英語

**Core_dirPath (データ保存フォルダ)**
```csharp
new FilePathSettingItem(
    "Core_dirPath",
    System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"),
    true)
```

- **型**: ファイルパス型（ディレクトリ選択）
- **目的**: AMUのデータ保存先フォルダを設定
- **デフォルト**: ユーザーのドキュメントフォルダ/AvatarModifyUtilities
- **制約**: ディレクトリのみ選択可能

**Core_versionInfo (バージョン情報)**
```csharp
new StringSettingItem("Core_versionInfo", "0.2.1", true)
```

- **型**: 文字列型（読み取り専用）
- **目的**: 現在のAMUバージョンを表示
- **デフォルト**: "0.2.1"
- **制約**: 読み取り専用

**Core_repositoryUrl (リポジトリURL)**
```csharp
new StringSettingItem("Core_repositoryUrl", "https://github.com/4OF-fof/AvatarModifyUtilities", true)
```

- **型**: 文字列型（読み取り専用）
- **目的**: AMUのGitHubリポジトリURLを表示
- **デフォルト**: GitHubリポジトリURL
- **制約**: 読み取り専用

#### 使用例

```csharp
using AMU.Editor.Setting;

// 全設定項目の取得
var allSettings = SettingData.SettingItems;

// 特定カテゴリの設定項目取得
var coreSettings = SettingData.SettingItems["Core_general"];

// 特定設定項目の検索
var languageSetting = coreSettings.FirstOrDefault(item => item.Name == "Core_language");
if (languageSetting is ChoiceSettingItem choiceItem)
{
    Console.WriteLine($"デフォルト言語: {choiceItem.DefaultValue}");
    foreach (var choice in choiceItem.Choices)
    {
        Console.WriteLine($"選択肢: {choice.Key} -> {choice.Value}");
    }
}
```

### 言語ファイル（Localization Data）

#### 概要
多言語対応のためのローカライゼーションデータです。JSON形式で各言語のテキストを定義します。

#### ファイル形式

```json
{
  "キー": "ローカライズされたテキスト",
  "Core_setting": "設定",
  "Core_general": "一般"
}
```

#### 現在サポートされている言語

##### 日本語 (ja_jp.json)
```json
{
  "Core_setting": "設定",
  "Core_general": "一般",
  "Core_language": "表示言語",
  "Core_dirPath": "データ保存フォルダ",
  "Core_versionInfo": "バージョン情報",
  "Core_repositoryUrl": "リポジトリURL"
}
```

##### 英語 (en_us.json)
AMU Coreモジュール用の英語版ローカライゼーションファイル

#### 命名規則

**設定項目用キー:**
- `{カテゴリ}_{設定名}`: 設定項目の表示名
- 例: `Core_language`, `Core_dirPath`

**UI要素用キー:**
- `ui_{要素名}_{詳細}`: UI要素のテキスト
- 例: `ui_button_save`, `ui_label_search`

**メッセージ用キー:**
- `message_{種類}_{詳細}`: ユーザーメッセージ
- 例: `message_error_filenotfound`, `message_success_saved`

#### 言語ファイルの追加

新しい言語を追加する手順：

1. **言語ファイルの作成**
```bash
# 例: 韓国語サポートの追加
Core/Data/lang/ko_kr.json
```

2. **JSON構造の定義**
```json
{
  "Core_setting": "설정",
  "Core_general": "일반",
  "Core_language": "표시 언어"
}
```

3. **ChoiceSettingItemの更新**
```csharp
new ChoiceSettingItem("Core_language",
    new Dictionary<string, string>
    {
        { "ja_jp", "日本語" },
        { "en_us", "English" },
        { "ko_kr", "한국어" }  // 新しい選択肢を追加
    }, "ja_jp")
```

## データ拡張ガイド

### 新しい設定項目の追加

1. **設定項目の定義**
```csharp
// SettingData.cs に新しい設定項目を追加
{ "Core_advanced", new SettingItem[] {
    new BoolSettingItem("Core_enableDebugMode", false),
    new IntSettingItem("Core_maxCacheSize", 1000, 100, 10000),
    new StringSettingItem("Core_customPath", "")
} }
```

2. **言語ファイルの更新**
```json
// ja_jp.json
{
  "Core_advanced": "詳細設定",
  "Core_enableDebugMode": "デバッグモードを有効にする",
  "Core_maxCacheSize": "最大キャッシュサイズ",
  "Core_customPath": "カスタムパス"
}
```

3. **UI層での自動反映**
設定項目は自動的にSettingWindowに反映されます。特別な処理は不要です。

### 設定カテゴリの追加

```csharp
// 新しいカテゴリの追加例
{ "Core_experimental", new SettingItem[] {
    new BoolSettingItem("Core_betaFeatures", false),
    new ChoiceSettingItem("Core_experimentalMode",
        new Dictionary<string, string>
        {
            { "disabled", "無効" },
            { "basic", "基本" },
            { "advanced", "詳細" }
        }, "disabled")
} }
```

## ベストプラクティス

### 設定項目設計の原則

1. **一貫性のある命名**
```csharp
// 良い例：一貫した命名規則
"Core_language"      // カテゴリ_機能名
"Core_dirPath"       // カテゴリ_機能名
"Core_maxItems"      // カテゴリ_機能名

// 避けるべき例：不一致な命名
"language"           // カテゴリなし
"CoreDirPath"        // 命名規則が異なる
"max-items"          // 異なる区切り文字
```

2. **適切なデフォルト値**
```csharp
// 良い例：実用的なデフォルト値
new IntSettingItem("Core_maxItems", 1000, 1, 100000)     // 実用的な範囲
new FilePathSettingItem("Core_dirPath", GetDefaultPath(), true)  // 有効なパス

// 避けるべき例：問題のあるデフォルト値
new IntSettingItem("Core_maxItems", 0, 1, 100)          // デフォルトが無効
new FilePathSettingItem("Core_dirPath", "", true)       // 空のパス
```

3. **読み取り専用フラグの適切な使用**
```csharp
// 読み取り専用にすべき項目
new StringSettingItem("Core_versionInfo", "1.0.0", true)     // バージョン情報
new StringSettingItem("Core_installPath", GetInstallPath(), true)  // インストールパス

// ユーザーが変更可能にすべき項目
new StringSettingItem("Core_userName", "User", false)        // ユーザー名
new FilePathSettingItem("Core_workDir", GetDefaultDir(), false)    // 作業ディレクトリ
```
