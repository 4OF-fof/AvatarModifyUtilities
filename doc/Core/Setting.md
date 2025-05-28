# 設定項目の追加方法

## 概要

Untitledプロジェクトの設定画面（SettingWindow）に新しい設定項目を追加するには、主に以下の手順を踏みます。

---

## 1. 設定項目クラスの選択・作成

`Editor/Core/Data/Structure/SettingItem.cs` には、以下のような設定項目用クラスが用意されています。

- `StringSettingItem`（文字列）
- `IntSettingItem`（整数）
- `BoolSettingItem`（真偽値）
- `FloatSettingItem`（小数）
- `ChoiceSettingItem`（選択肢）

必要に応じて、これらのクラスを使って設定項目を定義します。

---

## 2. 設定データの追加

`Untitled.Data.Setting` namespace 内の任意のクラス（例: `SettingData` など）の `SettingItems` 辞書に、設定項目を追加します。

例:
```csharp
namespace Untitled.Data.Setting
{
    public static class SettingData
    {
        public static readonly Dictionary<string, SettingItem[]> SettingItems = new Dictionary<string, SettingItem[]>
        {
            { "general", new SettingItem[] {
                // 文字列
                new StringSettingItem("user_name", "defaultUser"),
                // 整数
                new IntSettingItem("max_count", 10, 0, 100),
                // 真偽値
                new BoolSettingItem("is_enabled", true),
                // 小数
                new FloatSettingItem("volume", 0.5f, 0f, 1f),
                // 選択肢
                new ChoiceSettingItem("language",
                    new Dictionary<string, string>
                    {
                        { "ja_jp", "日本語" },
                        { "en_us", "English" },
                    }, "ja_jp"),
            } },
        };
    }
}
```

---

## 3. 多言語対応（任意）

設定項目名や選択肢を多言語対応したい場合は、`Editor/Core/Data/lang/ja_jp.json` や `en_us.json` にキーと翻訳を追加します。

また、`TextField.cs` にも新しいキーを追加してください。これは型安全なアクセスや自動生成のために利用されます。

例:
```json
{
  "user_name": "ユーザー名"
}
```

`TextField.cs` 例:
```csharp
public string user_name;
```

---

## 4. UIへの自動反映

`SettingWindow` は `SettingItems` を自動で読み込み、型に応じたUIを生成します。追加した項目は自動的に設定画面に表示されます。

---

## 5. 値の取得・保存

値は `EditorPrefs` を通じて自動で保存・取得されます。  
例:  
```csharp
string userName = EditorPrefs.GetString("Setting.user_name", "defaultUser");
```

---

## 設定項目クラスのコンストラクタ引数と戻り値

各設定項目クラスの登録用コンストラクタの引数と戻り値は以下の通りです。

### StringSettingItem
- **用途:** 文字列設定
- **コンストラクタ:**
  ```csharp
  StringSettingItem(string name, string defaultValue = "")
  ```
  - `name`: 設定キー名
  - `defaultValue`: デフォルト値（省略可）
  - **戻り値:** StringSettingItemインスタンス

### IntSettingItem
- **用途:** 整数設定
- **コンストラクタ:**
  ```csharp
  IntSettingItem(string name, int defaultValue = 0, int minValue = 0, int maxValue = 100)
  ```
  - `name`: 設定キー名
  - `defaultValue`: デフォルト値
  - `minValue`: 最小値
  - `maxValue`: 最大値
  - **戻り値:** IntSettingItemインスタンス

### BoolSettingItem
- **用途:** 真偽値設定
- **コンストラクタ:**
  ```csharp
  BoolSettingItem(string name, bool defaultValue = false)
  ```
  - `name`: 設定キー名
  - `defaultValue`: デフォルト値
  - **戻り値:** BoolSettingItemインスタンス

### FloatSettingItem
- **用途:** 小数設定
- **コンストラクタ:**
  ```csharp
  FloatSettingItem(string name, float defaultValue = 0f, float minValue = 0f, float maxValue = 1f)
  ```
  - `name`: 設定キー名
  - `defaultValue`: デフォルト値
  - `minValue`: 最小値
  - `maxValue`: 最大値
  - **戻り値:** FloatSettingItemインスタンス

### ChoiceSettingItem
- **用途:** 選択肢設定
- **コンストラクタ:**
  ```csharp
  ChoiceSettingItem(string name, Dictionary<string, string> choices, string defaultValue = "")
  ```
  - `name`: 設定キー名
  - `choices`: 選択肢（キー:値）
  - `defaultValue`: デフォルト値
  - **戻り値:** ChoiceSettingItemインスタンス

---

## まとめ

- `SettingItem` を使って設定項目を定義
- `SettingData.SettingItems` に追加
- 必要に応じて多言語ファイルも編集
- 画面・保存は自動
