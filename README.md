# Untitled - VRChat Avatar Variant Optimization Tool

**Untitled(仮)**は、VRChatアバター開発を効率化するUnityエディタ拡張ツールです。プレハブのVariant自動作成、ビルド前の自動保存機能を提供します。

## 主な機能

### 🔄 AutoVariant - 自動Variant作成システム
- **プレハブ追加の自動監視**: シーンにプレハブを追加すると自動的にVariantを作成
- **マテリアルの自動管理**: 使用するマテリアルを`Assets/Untitled_Variants/Material/`に自動コピー
- **エクスポートの最適化**: 変更のないマテリアルはコピー元を使用する設定でエクスポート
- **自動バックアップ**: アップロード前に自動的にアバターをunitypackageとして保存

## 使い方

### 基本的なワークフロー

1. **設定の確認**
   - `Untitled > Setting`から設定画面を開く
   - 「自動でVariantを作成する」をONにする（推奨・デフォルト設定）
   - 「ビルド前に現在のアバターを保存する」をONにする（推奨・デフォルト設定）

2. **アバター作業**
   - 通常通りアバターのプレハブをシーンに配置
   - 自動的にVariantが作成され、マテリアルが編集用にコピーされる

3. **ビルド**
   - VRC SDKから通常通りアップロード
   - ビルド前に自動的にアバターがエクスポートされる

## 設定項目

### Core（共通設定）
- **表示言語**: `ja_jp` (日本語) / `en_us` (English)
- **データ保存フォルダ**: エクスポートファイルの保存先

### AutoVariant（自動化設定）
- **自動でVariantを作成する**: プレハブ追加時の自動Variant作成
- **ビルド前に現在のアバターを保存する**: ビルド前の自動エクスポート
- **関連アセットをすべて含める**: エクスポート時の依存関係包含範囲

## 出力ファイル

### エクスポートされるUnityPackage
```
{データ保存フォルダ}/AutoVariant/
├── {blueprintId}/                    # アップロード済みアバター
│   └── YYMMDD-001.unitypackage
└── local/                           # ローカルアバター
    └── YYMMDD-{avatarName}-001.unitypackage
```

### 作成されるVariantファイル
```
Assets/Untitled_Variants/
├── Material/                        # コピーされたマテリアル
│   ├── Material1.mat
│   └── Material2.mat
└── Untitled_{OriginalName}.prefab   # Variantプレハブ
```