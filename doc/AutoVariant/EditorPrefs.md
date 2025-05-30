# AutoVariantで利用しているEditorPrefsの仕様と運用

## 概要
AutoVariantモジュールではUnityの`EditorPrefs`を活用し、自動Variant作成機能やビルド前保存機能の設定を永続化しています。これにより、ユーザーの設定がUnityエディタセッション間で保持され、一貫した動作を提供します。

---

## 主な用途とキー一覧

### 1. 自動Variant作成機能の有効/無効
- **キー:** `Setting.AutoVariant_enableAutoVariant`
- **デフォルト値:** `true`
- **取得例:**
  ```csharp
  bool isEnabled = EditorPrefs.GetBool("Setting.AutoVariant_enableAutoVariant", true);
  ```
- **用途:** プレハブがシーンに追加されたときに自動的にVariantを作成するかどうかを制御。

### 2. ビルド前保存機能の有効/無効
- **キー:** `Setting.AutoVariant_enablePrebuild`
- **デフォルト値:** `true`
- **取得例:**
  ```csharp
  bool isPrebuildEnabled = EditorPrefs.GetBool("Setting.AutoVariant_enablePrebuild", true);
  ```
- **用途:** VRChatアバターのビルド前に自動的に最適化されたアバターをエクスポートするかどうかを制御。

### 3. 関連アセット包含設定
- **キー:** `Setting.AutoVariant_includeAllAssets`
- **デフォルト値:** `true`
- **取得例:**
  ```csharp
  bool includeAll = EditorPrefs.GetBool("Setting.AutoVariant_includeAllAssets", true);
  ```
- **用途:** 
  - `true`: アバターの全ての依存関係を含めてエクスポート
  - `false`: `Assets/AMU_Variants/`配下のアセットのみを含めてエクスポート

---

## 設定の動作への影響

### 自動Variant作成機能 (`AutoVariant_enableAutoVariant`)
- **有効時:** プレハブがシーンに追加されると自動的に以下の処理を実行
  - マテリアルを`Assets/AMU_Variants/Material/`にコピー
  - Variantプレハブを`Assets/AMU_Variants/`に作成
  - シーン内のオブジェクトをVariantに置き換え
- **無効時:** 自動処理は実行されず、手動でVariant作成が必要

### ビルド前保存機能 (`AutoVariant_enablePrebuild`)
- **有効時:** VRChatアバターのビルド時に以下の処理を自動実行
  - マテリアルの最適化（重複除去）
  - 設定されたディレクトリにUnityPackageとしてエクスポート
  - ビルド後のマテリアル状態復元
- **無効時:** ビルド前の自動保存は実行されない

### アセット包含設定 (`AutoVariant_includeAllAssets`)
- **有効時:** アバターの全依存関係（テクスチャ、シェーダー、アニメーション等）を含む
- **無効時:** AMU_Variants配下のファイルのみを含む軽量パッケージを作成

---

## 実装における利用箇所

### ConvertVariant.cs
```csharp
// 自動Variant作成の有効性チェック
if (!EditorPrefs.GetBool("Setting.AutoVariant_enableAutoVariant", false)) return;
```

### Prebuild.cs
```csharp
// ビルド前保存の有効性チェック
if (EditorPrefs.GetBool("Setting.AutoVariant_enablePrebuild", true))
{
    OptimizeMaterials();
}

// アセット包含設定の確認
bool includeAllAssets = EditorPrefs.GetBool("Setting.AutoVariant_includeAllAssets", true);
```

---

## 設定変更方法
これらの設定は`SettingWindow`（AMU > Setting）から変更可能です。設定画面では以下のように表示されます：

- **自動でVariantを作成する** (AutoVariant_enableAutoVariant)
- **ビルド前に現在のアバターを保存する** (AutoVariant_enablePrebuild)  
- **関連アセットをすべて含める** (AutoVariant_includeAllAssets)

---

## 運用上の注意点
- 設定変更は即座に反映されますが、一部の機能は次回のプレハブ追加やビルド時から有効になります
- `includeAllAssets`設定により、エクスポートされるUnityPackageのサイズが大きく変わる可能性があります
- 自動Variant作成を無効にした場合、手動でのマテリアル管理が必要になります

---

## まとめ
- AutoVariantでは3つの主要な設定を`EditorPrefs`で管理
- 各設定はユーザーの作業フローや出力要件に応じて調整可能
- 設定画面から直感的に変更でき、変更は永続化される
- 設定内容により自動化の範囲や出力内容が制御される