# Schema 層ドキュメント

**最終更新日**: 2025年6月17日

## 概要

Schema層は、AMU Core モジュールにおけるデータ構造とスキーマ定義を管理します。型安全なデータ操作とUI生成のための基盤となるクラス群を提供します。

## ディレクトリ構造

```
Core/Schema/
└── SettingItem.cs          # 設定項目のスキーマ定義
```

## 依存関係

Schema層は以下の特徴を持ちます：

- **他の層に依存しない**: 最下位の独立した層
- **型安全性の提供**: 厳密な型定義によるコンパイル時チェック
- **UI生成の基盤**: SettingWindowなどのUI自動生成に使用

## Schema一覧

### SettingItem

#### 概要
設定項目の型定義とメタデータを管理する抽象基底クラスです。各設定項目の型、デフォルト値、制約などを定義します。

#### 名前空間
```csharp
using AMU.Editor.Core.Schema;
```

#### 基底クラス

##### SettingItem（抽象クラス）
```csharp
public abstract class SettingItem
{
    public string Name { get; }        // 設定項目名
    public SettingType Type { get; }   // 設定項目の型
}
```

#### 設定型定義

##### SettingType列挙型
```csharp
public enum SettingType 
{ 
    String,     // 文字列
    Int,        // 整数
    Bool,       // 真偽値
    Float,      // 浮動小数点数
    Choice,     // 選択肢
    FilePath,   // ファイルパス
    TextArea    // テキストエリア
}
```

### 具象設定項目クラス

#### StringSettingItem
文字列型の設定項目を定義します。

```csharp
public class StringSettingItem : SettingItem
{
    public string DefaultValue { get; }    // デフォルト値
    public bool IsReadOnly { get; }        // 読み取り専用フラグ
}
```

**使用例:**
```csharp
// 通常の文字列設定
var nameItem = new StringSettingItem("UserName", "Default User");

// 読み取り専用の文字列設定
var versionItem = new StringSettingItem("Version", "1.0.0", true);
```

#### IntSettingItem
整数型の設定項目を定義します。

```csharp
public class IntSettingItem : SettingItem
{
    public int DefaultValue { get; }    // デフォルト値
    public int MinValue { get; }        // 最小値
    public int MaxValue { get; }        // 最大値
}
```

**使用例:**
```csharp
// 範囲指定付きの整数設定
var maxItemsItem = new IntSettingItem("MaxItems", 100, 1, 1000);
```

#### BoolSettingItem
真偽値型の設定項目を定義します。

```csharp
public class BoolSettingItem : SettingItem
{
    public bool DefaultValue { get; }    // デフォルト値
}
```

**使用例:**
```csharp
// チェックボックス設定
var enabledItem = new BoolSettingItem("FeatureEnabled", true);
```

#### FloatSettingItem
浮動小数点数型の設定項目を定義します。

```csharp
public class FloatSettingItem : SettingItem
{
    public float DefaultValue { get; }   // デフォルト値
    public float MinValue { get; }       // 最小値
    public float MaxValue { get; }       // 最大値
}
```

**使用例:**
```csharp
// スライダー設定
var scaleItem = new FloatSettingItem("Scale", 1.0f, 0.1f, 5.0f);
```

#### ChoiceSettingItem
選択肢型の設定項目を定義します。

```csharp
public class ChoiceSettingItem : SettingItem
{
    public Dictionary<string, string> Choices { get; }  // 選択肢（キー, 表示名）
    public string DefaultValue { get; }                 // デフォルト値
}
```

**使用例:**
```csharp
// 言語選択設定
var languageItem = new ChoiceSettingItem("Language",
    new Dictionary<string, string>
    {
        { "ja_jp", "日本語" },
        { "en_us", "English" }
    }, "ja_jp");
```

#### FilePathSettingItem
ファイルパス型の設定項目を定義します。

```csharp
public class FilePathSettingItem : SettingItem
{
    public string DefaultValue { get; }      // デフォルトパス
    public string ExtensionFilter { get; }   // 拡張子フィルタ
    public bool IsDirectory { get; }         // ディレクトリ選択フラグ
}
```

**使用例:**
```csharp
// ファイル選択設定
var configFileItem = new FilePathSettingItem("ConfigFile", "", "*.json");

// ディレクトリ選択設定
var dataFolderItem = new FilePathSettingItem("DataFolder", "C:\\Data", true);
```

#### TextAreaSettingItem
テキストエリア型の設定項目を定義します。

```csharp
public class TextAreaSettingItem : SettingItem
{
    public string DefaultValue { get; }    // デフォルト値
    public bool IsReadOnly { get; }        // 読み取り専用フラグ
    public int MinLines { get; }           // 最小行数
    public int MaxLines { get; }           // 最大行数
}
```

**使用例:**
```csharp
// コメント入力エリア
var commentItem = new TextAreaSettingItem("Comments", "", false, 3, 10);

// 読み取り専用の情報表示エリア
var infoItem = new TextAreaSettingItem("Information", "詳細情報...", true, 5, 5);
```

## 設計パターン

### 型安全性の保証

各設定項目は厳密に型付けされており、コンパイル時に型の不整合を検出できます。

```csharp
// コンパイル時にエラーが検出される例
var intItem = new IntSettingItem("Count", 10);
// string value = intItem.DefaultValue; // コンパイルエラー！
int value = intItem.DefaultValue; // OK
```

### 拡張性の考慮

新しい設定項目型を追加する際は以下の手順で行います：

1. **SettingType列挙型に新しい型を追加**
2. **新しい具象クラスを作成**
3. **UI層での対応処理を追加**

```csharp
// 新しい設定項目型の例
public enum SettingType 
{ 
    // ...existing types...
    Color,      // 新しい色選択型
    DateTime    // 新しい日時選択型
}

public class ColorSettingItem : SettingItem
{
    public Color DefaultValue { get; }
    
    public ColorSettingItem(string name, Color defaultValue) 
        : base(name, SettingType.Color)
    {
        DefaultValue = defaultValue;
    }
}
```

## 使用ガイドライン

### 命名規則

設定項目名は以下の規則に従います：

- **カテゴリ_設定名** の形式を使用
- 例: `Core_language`, `AssetManager_maxItems`

### デフォルト値の設定

適切なデフォルト値を設定することで、初回起動時の使いやすさが向上します：

```csharp
// 良い例：適切なデフォルト値
var languageItem = new ChoiceSettingItem("Core_language",
    new Dictionary<string, string>
    {
        { "ja_jp", "日本語" },
        { "en_us", "English" }
    }, "ja_jp"); // システムロケールに基づく適切なデフォルト

// 避けるべき例：意味のないデフォルト値
var pathItem = new FilePathSettingItem("DataPath", ""); // 空文字では不親切
```

### 制約の適切な設定

数値型では適切な範囲制約を設定します：

```csharp
// 良い例：実用的な範囲制約
var maxItemsItem = new IntSettingItem("MaxItems", 100, 1, 10000);

// 避けるべき例：制約が厳しすぎる
var itemsItem = new IntSettingItem("Items", 10, 10, 10); // 選択肢がない
```