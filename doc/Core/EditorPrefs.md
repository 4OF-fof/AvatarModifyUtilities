# Coreで利用しているEditorPrefsの仕様と運用

## 概要
CoreではUnityの`EditorPrefs`を活用し、ユーザーごとのエディタ設定（主に多言語対応、保存先ディレクトリ、バージョン情報など）を永続化しています。これにより、エディタ拡張の設定値がUnityエディタ間で自動的に保存・復元され、ユーザー体験の一貫性が保たれます。

---

## 主な用途とキー一覧

### 1. 言語設定
- **キー:** `Setting.Core_language`
- **デフォルト値:** `ja_jp`
- **取得例:**
  ```csharp
  string lang = EditorPrefs.GetString("Setting.Core_language", "ja_jp");
  ```
- **用途:** 設定画面（SettingWindow）での言語選択や多言語UIの切り替えに利用。

### 2. データ保存フォルダ
- **キー:** `Setting.Core_dirPath`
- **デフォルト値:** `System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities")`
- **用途:** 各種データやエクスポートファイルの保存先ディレクトリ。

### 3. バージョン情報（表示専用/readonly）
- **キー:** `Setting.Core_versionInfo`
- **デフォルト値:** `"0.1.0-alpha"`
- **用途:** 設定画面でのバージョン表示。

### 4. リポジトリURL（表示専用/readonly）
- **キー:** `Setting.Core_repositoryUrl`
- **デフォルト値:** `"https://github.com/4OF-fof/AvatarModifyUtilities"`
- **用途:** 設定画面でのリポジトリURL表示。

---

## 運用・実装上のポイント
- これらの値は`SettingWindow`から自動的に保存・取得されます。
- 新たな設定項目を追加する場合は、`SettingItem`を定義し`SettingData.SettingItems`に登録してください。
- 多言語対応が必要な場合は、`Editor/Core/Data/lang/ja_jp.json`や`en_us.json`にキーと翻訳を追加し、`TextField.cs`にも新しいキーを追加してください。
- 設定値の取得・保存は`EditorPrefs`経由で自動的に行われます。

---

## まとめ
- Coreではユーザーごとのエディタ設定を永続化するために`EditorPrefs`を活用しています。
- 主なキーは「Setting.Core_language」「Setting.Core_dirPath」「Setting.Core_versionInfo」「Setting.Core_repositoryUrl」などです。
- これらの値は`SettingWindow`から自動的に保存・取得され、ユーザー体験の一貫性を担保します。
- 拡張やカスタマイズ時は、関連する多言語ファイルや型安全なアクセスのためのコードも忘れずに更新してください。
