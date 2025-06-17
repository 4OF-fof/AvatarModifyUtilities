# Core/UI モジュール ドキュメント

## 概要

Core/UIモジュールは、AvatarModifyUtilities（AMU）のCore機能における描画・ユーザーインターフェース担当モジュールです。新しい層構造（Controllers、Services）と連携し、UI固有の責務に集中した実装となっています。

## ディレクトリ構造

```
Core/UI/
├── SettingWindow.cs                    # 設定ウィンドウ メインクラス
└── _Components/                        # UIコンポーネント
    ├── MenuComponent.cs                # メニュー部分のコンポーネント
    ├── SettingPanelComponent.cs        # 設定パネルのコンポーネント
    └── SettingItemRenderer.cs          # 設定項目レンダラー
```

## 依存関係
Core/UIは以下の層に依存します：

- **Controllers層**: データの永続化と取得
- **Services層**: 初期化とサービス機能

逆に、Core/UIは**API層には依存しません**。

## クラス詳細

### SettingWindow

設定画面のUIを管理するEditorWindowクラスです。コンポーネント分割により、UIの描画詳細を各コンポーネントに委譲し、ライフサイクル管理とコンポーネント間の調整に責務を集中しています。

#### アーキテクチャ

- **責務の分離**: UIロジックをコンポーネントに分割
- **依存性注入**: コンポーネント間の疎結合
- **イベント駆動**: 言語変更などのイベント処理

#### 主要機能

1. **コンポーネント管理**
   - MenuComponent、SettingPanelComponentの初期化と管理
   - コンポーネント間の連携調整

2. **ライフサイクル管理**
   - ウィンドウの初期化とリソース管理
   - 言語変更時の再初期化

3. **UI統合**
   - 各コンポーネントの描画統合
   - ウィンドウサイズとレイアウトの管理

### MenuComponent

設定ウィンドウの左側メニュー部分を管理するコンポーネントです。

#### 主要機能

1. **メニュー項目管理**
   - 設定カテゴリの表示と選択
   - Core_generalカテゴリの優先表示

2. **検索機能**
   - メニュー項目のリアルタイム検索
   - 検索結果のフィルタリング

3. **UI描画**
   - メニュー背景の描画
   - 選択状態の視覚化

#### 公開インターフェース

```csharp
public class MenuComponent
{
    public int SelectedMenu { get; }
    public string[] MenuItems { get; }
    
    public void Initialize(Dictionary<string, SettingItem[]> settingItems)
    public void Draw(Vector2 windowPosition)
    public List<int> GetFilteredMenuIndices()
    public string GetMenuSearch()
}
```

### SettingPanelComponent

設定ウィンドウの右側設定パネル部分を管理するコンポーネントです。

#### 主要機能

1. **設定項目表示**
   - 選択されたカテゴリの設定項目を表示
   - SettingItemRendererとの連携

2. **検索結果表示**
   - フィルタリングされた設定項目の表示
   - 空の検索結果への対応

3. **言語変更検出**
   - 言語設定変更の検出とイベント発火
   - LocalizationControllerとの連携

#### 公開インターフェース

```csharp
public class SettingPanelComponent
{
    public System.Action OnLanguageChanged { get; set; }
    
    public void Initialize(Dictionary<string, SettingItem[]> settingItems)
    public void Draw(Vector2 windowPosition, MenuComponent menuComponent)
}
```

### SettingItemRenderer

各設定項目タイプの描画を担当する静的クラスです。

#### 主要機能

1. **タイプ別描画**
   - 7つの設定項目タイプに対応
   - 型安全な描画処理

2. **統一されたUI**
   - 一貫したラベルスタイル
   - レスポンシブなレイアウト

3. **設定値管理**
   - SettingsControllerとの直接連携
   - 変更検出と即座反映

#### 対応設定項目

- **String**: 文字列入力フィールド（読み取り専用対応）
- **Int**: 整数入力フィールド  
- **Bool**: チェックボックス
- **Float**: スライダー（最小値・最大値対応）
- **Choice**: ドロップダウン選択
- **FilePath**: ファイル/フォルダ選択（ブラウザ対応）
- **TextArea**: 複数行テキスト入力（行数制限対応）

#### 公開インターフェース

```csharp
public static class SettingItemRenderer
{
    public static void DrawSettingItem(SettingItem item, string menuSearch)
}
```

## 使用方法

### 基本的な使用法

```csharp
using AMU.Editor.Core.UI;

// 設定ウィンドウを開く（メニューから）
// AMU/Setting

// プログラムから起動
SettingWindow.ShowWindow();
```

### コンポーネントの個別使用

```csharp
using AMU.Editor.Core.UI.Components;

// メニューコンポーネントの使用例
var menuComponent = new MenuComponent();
menuComponent.Initialize(settingItems);
menuComponent.Draw(windowSize);

// 設定パネルコンポーネントの使用例
var panelComponent = new SettingPanelComponent();
panelComponent.Initialize(settingItems);
panelComponent.OnLanguageChanged = () => { /* 言語変更処理 */ };
panelComponent.Draw(windowSize, menuComponent);

// 設定項目の個別描画
SettingItemRenderer.DrawSettingItem(settingItem, searchQuery);
```

### カスタム設定項目の追加

新しい設定項目を追加する場合、コンポーネント分割により既存コードの変更は最小限です：

1. `AMU.Data.Setting`名前空間で設定データを定義
2. SettingItemRendererが新しい項目タイプに対応していれば自動的に表示
3. 新しい項目タイプの場合、SettingItemRendererに描画ロジックを追加

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
                new BoolSettingItem("MyModule_setting2", true),
                new FloatSettingItem("MyModule_slider", 0.5f, 0.0f, 1.0f)
            }
        };
    }
}
```

## 依存関係

```
Core/UI/
├── SettingWindow.cs                    # メインウィンドウ
│   ├── Controllers/                    # データ管理層
│   │   ├── LocalizationController     # 多言語化
│   │   └── SettingsController         # 設定管理
│   └── _Components/                    # UIコンポーネント層
│       ├── MenuComponent              # メニュー管理
│       ├── SettingPanelComponent      # パネル管理  
│       └── SettingItemRenderer        # 項目描画
└── Services/                           # サービス機能（間接的）
    └── InitializationService          # 初期化サービス
```

### コンポーネント間の依存関係

```
SettingWindow
├── MenuComponent
│   └── LocalizationController
│   └── SettingsController
├── SettingPanelComponent  
│   ├── MenuComponent (参照)
│   ├── SettingItemRenderer
│   └── LocalizationController
└── SettingItemRenderer
    ├── SettingsController
    └── LocalizationController
```