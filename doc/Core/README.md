# Core モジュール ドキュメント

## 概要

Coreモジュールは、AvatarModifyUtilities（AMU）の中核機能を提供する基盤モジュールです。以下の4つの明確な層に分離されています：

- **API層**: 外部から呼び出される公開機能
- **Controllers層**: 永続データの管理
- **Services層**: 初期化処理とサービス機能
- **UI層**: ユーザーインターフェース

## ディレクトリ構造

```
Core/
├── API/                    # 外部公開API
│   ├── ObjectCaptureAPI.cs # オブジェクトキャプチャ機能
│   └── VRChatAPI.cs        # VRChat関連機能
├── Controllers/            # データコントローラ
│   ├── LocalizationController.cs  # ローカライゼーション管理
│   └── SettingsController.cs      # 設定データ管理
├── Services/               # サービス層
│   ├── InitializationService.cs       # 初期化サービス
│   └── BackwardCompatibilityAliases.cs # 後方互換性エイリアス
├── Data/                   # データ定義
└── UI/                     # UI関連
    └── SettingWindow.cs    # 設定ウィンドウ
```

## 層の詳細

### API層 (`Core/API/`)

外部モジュールから呼び出される公開機能を提供します。

#### ObjectCaptureAPI
- **目的**: GameObjectのキャプチャとテクスチャ生成
- **主要メソッド**:
  - `CaptureObject(GameObject, string, int, int)`: オブジェクトキャプチャ

#### VRChatAPI
- **目的**: VRChat関連の機能提供
- **主要メソッド**:
  - `GetBlueprintId(GameObject)`: Blueprint ID取得
  - `IsVRCAvatar(GameObject)`: VRCアバター判定

### Controllers層 (`Core/Controllers/`)

永続データの管理とアクセス制御を担当します。

#### LocalizationController
- **目的**: 多言語化機能の管理
- **機能**:
  - 言語ファイルの読み込み
  - ローカライズテキストの提供
  - 言語切り替え
- **主要メソッド**:
  - `LoadLanguage(string)`: 言語ファイル読み込み
  - `GetText(string)`: ローカライズテキスト取得
  - `HasKey(string)`: キー存在確認

#### SettingsController
- **目的**: EditorPrefs設定の管理
- **機能**:
  - 設定の初期化
  - 設定値の取得・保存
  - 型安全な設定アクセス
- **主要メソッド**:
  - `InitializeEditorPrefs()`: 設定初期化
  - `GetSetting<T>(string, T)`: 設定値取得
  - `SetSetting<T>(string, T)`: 設定値保存

### UI層 (`Core/UI/`)

ユーザーインターフェースの描画と操作を担当します。

#### SettingWindow
- **目的**: 設定画面のUI管理
- **機能**:
  - 多言語対応インターフェース
  - 設定項目の動的表示
  - 検索・フィルタリング機能
  - レスポンシブレイアウト
- **主要メソッド**:
  - `ShowWindow()`: 設定ウィンドウを開く
- **依存関係**: 
  - LocalizationController（ローカライゼーション）
  - SettingsController（設定管理）

### Services層 (`Core/Services/`)

システムの初期化とサービス機能を提供します。

#### InitializationService
- **目的**: AMUの起動時初期化
- **機能**:
  - 自動初期化（`[InitializeOnLoad]`）
  - 各コンポーネントの初期化
  - エラーハンドリング
- **初期化対象**:
  - EditorPrefs設定
  - TagTypeManager
  - ローカライゼーション

#### BackwardCompatibilityAliases
- **目的**: 既存コードとの互換性維持
- **提供エイリアス**:
  - `ObjectCaptureHelper` → `ObjectCaptureAPI`
  - `PipelineManagerHelper` → `VRChatAPI`
  - `AMUInitializer` → `InitializationService`
