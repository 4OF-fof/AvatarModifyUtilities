# Core/UI モジュール ドキュメント

## 概要

Core/UIモジュールは、AvatarModifyUtilities（AMU）のCore機能における描画・ユーザーインターフェース担当モジュールです。2025年6月のリファクタリングにより、新しい層構造（Controllers、Services）と連携し、UI固有の責務に集中した実装となっています。

## ディレクトリ構造

```
Core/UI/
└── SettingWindow.cs        # 設定ウィンドウ UI
```

## 設計原則

### 責務の分離
Core/UIモジュールは以下の責務を担います：

1. **UI描画**: Unity Editor GUI の描画ロジック
2. **ユーザー操作**: ボタンクリック、入力フィールドなどの操作処理
3. **画面レイアウト**: ウィンドウの構成とレスポンシブ対応
4. **視覚的フィードバック**: ユーザーへの情報表示

### 依存関係
Core/UIは以下の層に依存します：

- **Controllers層**: データの永続化と取得
- **Services層**: 初期化とサービス機能

逆に、Core/UIは**API層には依存しません**。UIは内部機能であり、外部公開機能は使用しません。

## クラス詳細

### SettingWindow

設定画面のUIを管理するEditorWindowクラスです。

#### 主要機能

1. **多言語対応UI**
   - LocalizationControllerを使用してテキストの国際化
   - 言語切り替え時の即座反映

2. **設定管理**
   - SettingsControllerを使用した型安全な設定操作
   - EditorPrefsの直接操作を回避

3. **検索・フィルタリング**
   - 設定項目の動的検索
   - カテゴリフィルタリング

4. **レスポンシブレイアウト**
   - 固定ウィンドウサイズでの最適化
   - メニューパネルと設定パネルの分離

#### 使用例

```csharp
// メニューから起動
// AMU/Setting

// プログラムから起動
AMU.Editor.Core.UI.SettingWindow.ShowWindow();
```

#### 対応設定項目

- **String**: 文字列入力フィールド
- **Int**: 整数入力フィールド  
- **Bool**: チェックボックス
- **Float**: スライダー
- **Choice**: ドロップダウン選択
- **FilePath**: ファイル/フォルダ選択
- **TextArea**: 複数行テキスト入力

## リファクタリング内容（2025年6月）

### 変更点

1. **名前空間の追加**
   ```csharp
   // 旧
   public class SettingWindow : EditorWindow
   
   // 新
   namespace AMU.Editor.Core.UI
   {
       public class SettingWindow : EditorWindow
   }
   ```

2. **Controllers層の活用**
   ```csharp
   // 旧（直接EditorPrefs操作）
   string lang = EditorPrefs.GetString("Setting.Core_language", "en_us");
   EditorPrefs.SetString(key, newValue);
   
   // 新（SettingsController使用）
   string lang = SettingsController.GetSetting("Core_language", "en_us");
   SettingsController.SetSetting(item.Name, newValue);
   ```

3. **ローカライゼーションAPIの更新**
   ```csharp
   // 旧（後方互換性API）
   LocalizationManager.LoadLanguage(lang);
   LocalizationManager.GetText(key);
   
   // 新（新しいController）
   LocalizationController.LoadLanguage(lang);
   LocalizationController.GetText(key);
   ```

4. **重複コードの削除**
   - 設定項目取得ロジックをSettingsControllerに統合
   - 初期値設定ロジックをSettingsControllerに委譲
   - SettingDataHelperクラスの削除

### 削除された機能

1. **InitializeDefaultValues()メソッド**
   - SettingsController.InitializeEditorPrefs()で代替

2. **SetDefaultValue()メソッド**
   - SettingsControllerのSetDefaultValue()で代替

3. **SettingDataHelperクラス**
   - SettingsController.GetAllSettingItems()で代替

## 使用方法

### 基本的な使用法

```csharp
using AMU.Editor.Core.UI;

// 設定ウィンドウを開く
SettingWindow.ShowWindow();
```

### カスタム設定項目の追加

新しい設定項目を追加する場合：

1. `AMU.Data.Setting`名前空間で設定データを定義
2. SettingWindowは自動的に新しい項目を認識して表示

```csharp
// 設定データの例（別ファイル）
namespace AMU.Data.Setting
{
    public static class MyModuleSettings
    {
        public static Dictionary<string, SettingItem[]> SettingItems = new()
        {
            ["MyModule"] = new SettingItem[]
            {
                new StringSettingItem("MyModule_setting1", "Default Value", false),
                new BoolSettingItem("MyModule_setting2", true)
            }
        };
    }
}
```

## 今後の拡張

### 推奨される拡張方針

1. **新しいUIコンポーネント**
   - `Core/UI/`配下に新しいEditorWindowクラスを追加
   - 既存の層構造（Controllers、Services）を活用

2. **設定UIの拡張**
   - 新しい設定項目タイプの追加
   - カスタムGUIコンポーネントの実装

3. **レイアウトの改善**
   - レスポンシブデザインの強化
   - テーマ機能の追加

### 注意事項

- UIコンポーネントは**UI専用の責務**に集中する
- データ管理は**Controllers層**に委譲する
- 外部公開機能（API層）は使用しない
- 新しい名前空間 `AMU.Editor.Core.UI` を使用する

## 依存関係

```
Core/UI/
├── Controllers/     # データ管理
│   ├── LocalizationController
│   └── SettingsController
└── Services/        # サービス機能（間接的）
    └── InitializationService
```

## アーキテクチャ図

```
┌─────────────────┐
│   Core/UI/      │  ←─ ユーザー操作
│  SettingWindow  │
└─────────────────┘
         │
         ▼
┌─────────────────┐
│ Controllers/    │
│ - Localization  │
│ - Settings      │
└─────────────────┘
         │
         ▼
┌─────────────────┐
│   EditorPrefs   │  ←─ 永続化
│  Language Files │
└─────────────────┘
```

このリファクタリングにより、Core/UIモジュールは保守性が向上し、新しい層構造との統合が完了しました。
