# 設定項目の追加・管理方法

## 概要
AMUプロジェクトの設定画面（SettingWindow）に新しい設定項目を追加・管理する手順をまとめます。型安全・多言語対応・自動UI生成・EditorPrefsによる永続化など、拡張性と保守性を重視した設計です。

---

## 1. 設定項目クラスの選択・作成
`Editor/Core/Data/Structure/SettingItem.cs` には、以下のような型ごとの設定項目クラスが用意されています。
- `StringSettingItem`（文字列）
- `IntSettingItem`（整数）
- `BoolSettingItem`（真偽値）
- `FloatSettingItem`（小数）
- `ChoiceSettingItem`（選択肢）
- `FilePathSettingItem`（ファイル・ディレクトリ選択）
必要に応じて、これらのクラスを使って新しい設定項目を定義します。

---

## 2. 設定データへの追加
`AMU.Data.Setting` namespace内の`SettingData.SettingItems`辞書に、追加したい設定項目を登録します。
```csharp
namespace AMU.Data.Setting
{
    public static class SettingData
    {
        public static readonly Dictionary<string, SettingItem[]> SettingItems = new Dictionary<string, SettingItem[]>
        {
            { "general", new SettingItem[] {
                new StringSettingItem("user_name", "defaultUser"),
                new IntSettingItem("max_count", 10, 0, 100),
                new BoolSettingItem("is_enabled", true),
                new FloatSettingItem("volume", 0.5f, 0f, 1f),
                new ChoiceSettingItem("language",
                    new Dictionary<string, string>
                    {
                        { "ja_jp", "日本語" },
                        { "en_us", "English" },
                    }, "ja_jp"),
                new FilePathSettingItem("savePath", "", "cs", true),
            } },
        };
    }
}
```

---

## 3. 多言語対応（任意）
設定項目名や選択肢を多言語対応したい場合は、`Editor/Core/Data/lang/ja_jp.json`や`en_us.json`にキーと翻訳を追加します。また、`TextField.cs`にも新しいキーを追加してください。
```json
{
  "user_name": "ユーザー名"
}
```
`TextField.cs`例:
```csharp
public string user_name;
```

---

## 4. UIへの自動反映
`SettingWindow`は`SettingItems`を自動で読み込み、型に応じたUIを生成します。追加した項目は自動的に設定画面に表示されます。

---

## 5. 値の取得・保存
値は`EditorPrefs`を通じて自動で保存・取得されます。
```csharp
string userName = EditorPrefs.GetString("Setting.user_name", "defaultUser");
```

---

## まとめ
- `SettingItem`で設定項目を定義し、`SettingData.SettingItems`に追加
- 必要に応じて多言語ファイル・TextField.csも編集
- 画面・保存は自動で反映
- 型安全・多言語・自動UI・永続化の仕組みを活用し、拡張性・保守性の高い設定管理を実現
