# Core内で使用されているEditorPrefsについて

## 概要
Coreでは、Unityの`EditorPrefs`を利用してユーザーごとのエディタ設定（主に多言語対応）を保存・取得しています。これにより、エディタ拡張の設定値がUnityエディタ間で永続化されます。

---

## 主な用途とキー

### 1. 言語設定
- **キー:** `Setting.language`
- **デフォルト値:** `ja_jp`
- **取得例:**
  ```csharp
  string lang = EditorPrefs.GetString("Setting.language", "ja_jp");
  ```
- **設定例:**
  ```csharp
  EditorPrefs.SetString("Setting.language", "en_us");
  ```
- **用途:** 設定画面（SettingWindow）での言語選択や多言語UIの切り替えに利用されています。

---

## まとめ
- Coreでは、ユーザーごとのエディタ設定を永続化するために`EditorPrefs`を活用しています。
- 現状、キーが明確に定まっているのは「Setting.language」（言語設定）のみです。
