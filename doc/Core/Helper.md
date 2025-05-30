# Core/Helper ディレクトリの内容と活用方法

## 概要
`Core/Helper` ディレクトリには、Unityエディタ拡張やプロジェクトの共通処理を補助するヘルパークラスが格納されています。主に多言語対応やVRChat向けのPipelineManager操作など、他の機能から再利用されるロジックを提供します。

---

## 各ファイルの説明

### LocalizationHelper.cs
- **役割:** 多言語対応のためのローカライズ管理を行う静的クラス。
- **主な機能:**
  - `LoadLanguage(languageCode: string): void`
    - 指定言語のJSONファイルを全て読み込み、キーと値の辞書を生成。
  - `GetText(key: string): string`
    - 指定キーに対応するテキストを取得。該当がなければキー自体を返す。
- **利用例:**
  ```csharp
  string lang = EditorPrefs.GetString("Setting.Core_language", "ja_jp");
  LocalizationManager.LoadLanguage(lang);
  string label = LocalizationManager.GetText("setting");
  ```
- **注意点:**
  - `Assets/AMU/Editor`配下の全ての`{lang}.json`ファイルを自動で検出します。
  - 言語ファイルの形式やキーの重複に注意してください。
  - 言語設定を反映したUIを正しく表示するため、`LoadLanguage()`は`OnEnable()`内で呼び出してください。

### PipelineManagerHelper.cs
- **役割:** VRChatのPipelineManagerコンポーネントからblueprintIdを安全に取得するヘルパー。
- **主な機能:**
  - `GetBlueprintId(go: GameObject): string?`
    - 指定GameObjectにアタッチされたPipelineManagerからblueprintIdを取得。
- **利用例:**
  ```csharp
  string blueprintId = PipelineManagerHelper.GetBlueprintId(targetGameObject);
  if (blueprintId != null) Debug.Log($"blueprintId = {blueprintId}");
  ```
- **注意点:**
  - PipelineManagerが存在しない場合やblueprintIdが"avtr"で始まらない場合はnullを返します。
  - 対象GameObjectにPipelineManagerが正しくアタッチされていることを確認してください。

---

## 拡張・運用のポイント
- 新たな共通処理を追加する場合は、再利用性・汎用性を意識してヘルパークラスとして実装してください。
- 多言語対応や設定値の取得には`EditorPrefs`や`lang`ディレクトリのJSONファイルを活用してください。
- コードの追加時は、既存の命名規則やディレクトリ構成に従ってください。

---

## まとめ
- `Core/Helper`はプロジェクト全体で再利用される補助的なロジックを集約しています。
- 多言語対応やVRChat向けの特殊処理など、共通化できる機能はここに実装してください。
