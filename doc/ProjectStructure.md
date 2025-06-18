# プロジェクト構造・ローカライズ・設定ファイルガイド

このドキュメントでは、AvatarModifyUtilitiesプロジェクトのフォルダ構造、ローカライズキーの配置と命名規則、設定ファイルの書き方について説明します。

## フォルダ構造

### 基本構造

```
Assets/AvatarModifyUtilities/
├── doc/                    # ドキュメント
├── Editor/                 # Editorスクリプト
│   ├── Core/               # 共通機能
│   ├── AssetManager/       # アセット管理機能
│   └── AutoVariant/        # 自動バリアント機能
```

### 各モジュールの詳細構造

各機能モジュール（Core、AssetManager、AutoVariant）は以下の構造に従います：

```
ModuleName/
├── API/                  # 外部API関連
├── Controllers/          # UIとロジックの制御
├── Data/                 # データクラスと設定
│   ├── lang/             # ローカライズファイル
│   └── Setting.cs        # 設定定義
├── Schema/               # データスキーマ定義
├── Services/             # ビジネスロジック
└── UI/                   # UI関連
    └── _Components/      # UIコンポーネント
```

## ローカライズシステム

### ファイル配置

各モジュールの`Data/lang/`フォルダに言語ファイルを配置します：

```
ModuleName/Data/lang/
├── en_us.json            # 英語（デフォルト）
├── ja_jp.json            # 日本語
└── [other_lang].json     # その他の言語
```

### 命名規則

#### キー命名パターン

1. **設定関連キー**: `ModuleName_feature_element`
   ```json
   {
     "AssetManager_windowTitle": "Asset Manager",
     "AssetManager_searchPlaceholder": "Search assets...",
     "AutoVariant_exportSettings": "Export Settings"
   }
   ```

2. **UIキー**: `ModuleName_ui_element_action`
   ```json
   {
     "Core_ui_button_save": "Save",
     "Core_ui_button_cancel": "Cancel",
     "AssetManager_ui_label_name": "Name",
     "AutoVariant_ui_menu_file": "File"
   }
   ```

3. **メッセージキー**: `ModuleName_message_type_context`
   ```json
   {
     "Core_message_error_filenotfound": "File not found",
     "AssetManager_message_success_saved": "Successfully saved",
     "AutoVariant_message_warning_unsaved": "You have unsaved changes",
     "Core_message_info_loading": "Loading..."
   }
   ```

## 設定ファイルシステム

### 設定ファイルの配置

各モジュールの`Data/`フォルダに`Setting.cs`を配置します：

```
ModuleName/Data/Setting.cs
```

### 設定ファイルの構造

#### 基本テンプレート

```csharp
using System.Collections.Generic;
using AMU.Editor.Core.Schema;

namespace AMU.Editor.Setting
{
    public static class ModuleNameSettingData
    {
        public static readonly Dictionary<string, SettingItem[]> SettingItems = 
            new Dictionary<string, SettingItem[]>
        {
            { "Category Name", new SettingItem[] {
                // 設定項目を定義
            } },
        };
    }
}
```

#### 設定項目の種類

1. **StringSettingItem**: 文字列設定
   ```csharp
   new StringSettingItem("key_name", "default_value", isReadOnly: false)
   ```

2. **BoolSettingItem**: ブール値設定
   ```csharp
   new BoolSettingItem("key_name", defaultValue: false)
   ```

3. **IntSettingItem**: 整数設定
   ```csharp
   new IntSettingItem("key_name", defaultValue: 0, minValue: 0, maxValue: 100)
   ```

4. **FloatSettingItem**: 浮動小数点数設定
   ```csharp
   new FloatSettingItem("key_name", defaultValue: 0.0f, minValue: 0.0f, maxValue: 1.0f)
   ```

5. **ChoiceSettingItem**: 選択肢設定
   ```csharp
   new ChoiceSettingItem("key_name",
       new Dictionary<string, string>
       {
           { "value1", "Display Name 1" },
           { "value2", "Display Name 2" }
       }, "default_value")
   ```

6. **FilePathSettingItem**: ファイルパス設定
   ```csharp
   new FilePathSettingItem("key_name", "default/path", isReadOnly: false)
   ```

7. **TextAreaSettingItem**: 複数行テキスト設定
   ```csharp
   new TextAreaSettingItem("key_name", "default\ntext", isReadOnly: false, minLines: 3, maxLines: 8)
   ```

## ベストプラクティス

### ローカライズ

1. **キー名は英語で記述**し、意味が明確になるようにする
2. **一貫した命名規則**を維持する
3. **モジュール固有のUI要素は`ModuleName_ui_`プレフィックス**を使用する
4. **メッセージは`ModuleName_message_`プレフィックス**を使用し、種類別に分類する（error, success, warning, info）
5. **プレースホルダー**が必要な場合は`{0}`, `{1}`形式を使用する
6. **モジュール間で共通のキーは避け**、各モジュールで独自のキーを定義する

### 設定ファイル

1. **モジュール名をクラス名に含める**（例：`AssetManagerSettingData`）
2. **設定項目は論理的にグループ化**する
3. **適切なデフォルト値**を設定する
4. **読み取り専用項目**は`isReadOnly: true`を設定する
5. **数値項目には適切な最小・最大値**を設定する

### フォルダ構造

1. **機能ごとにモジュール分割**する
2. **共通機能はCoreに配置**する
3. **UIとロジックを分離**する
4. **スキーマ定義は専用フォルダ**に配置する
5. **ヘルパークラスは用途に応じて配置**する

## 新機能追加時の手順

1. **フォルダ構造**を決定し、適切な場所にファイルを配置
2. **既存APIとコントローラの確認**：新機能実装前に[API.md](API.md)と[Controllers.md](Controllers.md)を確認し、既存の機能を活用する
3. **ローカライズキー**を定義し、英語・日本語ファイルを作成
4. **設定が必要な場合**は`Setting.cs`を作成
5. **スキーマが必要な場合**は`Schema/`フォルダに定義ファイルを作成
6. **ドキュメント**を更新

### APIとコントローラの活用

新機能を実装する際は、以下の点に注意してください：

- **既存APIの活用**：類似機能を実装する前に、既存APIで対応可能か確認してください
- **既存コントローラの活用**：設定管理（SettingsController）、ローカライズ（LocalizationController）などの共通機能は既存コントローラを使用してください
- **重複実装の回避**：新しいAPIやコントローラを作成する前に、既存のドキュメントを確認し、重複した機能を作成しないようにしてください
- **拡張性の考慮**：既存機能を拡張する場合は、後方互換性を保ちながら実装してください

このガイドに従うことで、プロジェクト全体の一貫性と保守性を維持できます。
