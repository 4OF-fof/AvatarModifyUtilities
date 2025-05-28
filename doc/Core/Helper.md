# Core/Helper ディレクトリの内容と使用方法

## 概要
`Core/Helper` ディレクトリには、Unityエディタ拡張やプロジェクトの共通処理を補助するヘルパークラスが格納されています。主に多言語対応やVRChat向けのPipelineManager操作など、他の機能から再利用されるロジックを提供します。

---

## 各ファイルの説明

### LocalizationHelper.cs
- **役割**: 多言語対応のためのローカライズ管理を行う静的クラス。
- **主な機能**:
  - `LoadLanguage(languageCode: string): void`
    - 引数: 読み込む言語コード（例: "ja_jp"）
    - return: なし（void）
    - 概要: 指定言語のJSONファイルを全て読み込み、キーと値の辞書を生成。
  - `GetText(key: string): string`
    - 引数: 取得したいテキストのキー
    - return: 指定キーに対応するテキスト。該当がなければキー自体を返す。
- **使い方例**:
  ```csharp
  string lang = EditorPrefs.GetString("Setting.Core_language", "ja_jp");
  LocalizationManager.LoadLanguage(lang);
  string label = LocalizationManager.GetText("setting");
  ```
- **注意点**:
  - `Assets/Untitled/Editor`配下の全ての`{lang}.json`ファイルを自動で検索します。
  - 言語ファイルの形式やキーの重複に注意してください。
  - 言語設定を反映したUIを正しく表示するため、`LoadLanguage()`は`OnEnable()`内で呼び出してください。

### PipelineManagerHelper.cs
- **役割**: VRChatのPipelineManagerコンポーネントからblueprintIdを安全に取得するヘルパー。
- **主な機能**:
  - `GetBlueprintId(go: GameObject): string?`
    - 引数: blueprintIdを取得したいGameObject
    - return: blueprintId（"avtr..."形式）またはnull
    - 概要: 指定GameObjectにアタッチされたPipelineManagerからblueprintIdを取得。
- **使い方例**:
  ```csharp
  string blueprintId = PipelineManagerHelper.GetBlueprintId(targetGameObject);
  if (blueprintId != null) Debug.Log($"blueprintId = {blueprintId}");
  ```
- **注意点**:
  - PipelineManagerが存在しない場合やblueprintIdが"avtr"で始まらない場合はnullを返します。
  - 対象GameObjectにPipelineManagerが正しくアタッチされていることを確認してください。

---
