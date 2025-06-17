# Core モジュール ドキュメント

## 概要

Coreモジュールは、AvatarModifyUtilities（AMU）の中核機能を提供する基盤モジュールです。2025年6月のリファクタリングにより、以下の3つの明確な層に分離されました：

- **API層**: 外部から呼び出される公開機能
- **Controllers層**: 永続データの管理
- **Services層**: 初期化処理とサービス機能

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
└── UI/                     # UI関連（変更なし）
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

## 使用方法

### 新しいAPI使用例

```csharp
// オブジェクトキャプチャ
using AMU.Editor.Core.API;
var texture = ObjectCaptureAPI.CaptureObject(gameObject, "path/to/save.png");

// VRChat機能
using AMU.Editor.Core.API;
var blueprintId = VRChatAPI.GetBlueprintId(avatar);
bool isVRCAvatar = VRChatAPI.IsVRCAvatar(gameObject);

// 設定管理
using AMU.Editor.Core.Controllers;
SettingsController.SetSetting("MySettingKey", "MyValue");
var value = SettingsController.GetSetting<string>("MySettingKey", "DefaultValue");

// ローカライゼーション
using AMU.Editor.Core.Controllers;
LocalizationController.LoadLanguage("en_us");
var text = LocalizationController.GetText("ui_button_save");
```

### 後方互換性

既存のコードは引き続き動作しますが、新しいAPIの使用が推奨されます：

```csharp
// 古い方法（非推奨だが動作する）
using AMU.Editor.Core.Helper;
var texture = ObjectCaptureHelper.CaptureObject(gameObject, "path.png");

// 新しい方法（推奨）
using AMU.Editor.Core.API;
var texture = ObjectCaptureAPI.CaptureObject(gameObject, "path.png");
```

## 移行ガイド

### 既存コードの移行

1. **名前空間の更新**:
   - `AMU.Editor.Core.Helper` → `AMU.Editor.Core.API`
   - `AMU.Editor.Initializer` → `AMU.Editor.Core.Services`

2. **クラス名の更新**:
   - `ObjectCaptureHelper` → `ObjectCaptureAPI`
   - `PipelineManagerHelper` → `VRChatAPI`
   - `AMUInitializer` → `InitializationService`

3. **メソッド名の更新**:
   - `isVRCAvatar()` → `IsVRCAvatar()`

### 段階的移行

1. **Phase 1**: 後方互換性エイリアスを使用（現在）
2. **Phase 2**: Obsolete警告を確認し、新しいAPIに移行
3. **Phase 3**: 後方互換性エイリアスの削除（将来）

## 設計原則

1. **単一責任の原則**: 各層が明確な責任を持つ
2. **依存関係の逆転**: 上位層が下位層に依存
3. **開放閉鎖の原則**: 拡張に開放、修正に閉鎖
4. **後方互換性**: 既存コードの動作を保証

## 今後の拡張

### 推奨される拡張方針

1. **新しいAPI**: `Core/API/` に追加
2. **データ管理**: `Core/Controllers/` に追加
3. **サービス機能**: `Core/Services/` に追加
4. **UI機能**: `Core/UI/` に追加（既存構造を維持）

### 注意事項

- UIの変更は今回のリファクタリング対象外
- 既存のUI依存関係は維持
- 新しい機能は新しい層構造に従って実装
