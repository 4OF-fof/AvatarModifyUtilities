# Core/UI モジュール ドキュメント

## 概要

Core/UIモジュールは、AvatarModifyUtilities（AMU）のCore機能における描画・ユーザーインターフェース担当モジュールです。新しい層構造（Controllers、Services）と連携し、UI固有の責務に集中した実装となっています。

## ディレクトリ構造

```
Core/UI/
└── SettingWindow.cs        # 設定ウィンドウ UI
```

## 依存関係
Core/UIは以下の層に依存します：

- **Controllers層**: データの永続化と取得
- **Services層**: 初期化とサービス機能

逆に、Core/UIは**API層には依存しません**。

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

## 依存関係

```
Core/UI/
├── Controllers/     # データ管理
│   ├── LocalizationController
│   └── SettingsController
└── Services/        # サービス機能（間接的）
    └── InitializationService
```