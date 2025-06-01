# Avatar Modify Utilities(AMU)

**AMU**は、VRChatアバター開発を効率化するUnityエディタ拡張ツールです。プレハブのVariant自動作成、ビルド前の自動保存機能を提供します。

## 主な機能

### 🔄 AutoVariant - 自動Variant作成システム
- **プレハブ追加の自動監視**: シーンにプレハブを追加すると自動的にVariantを作成
- **マテリアルの自動管理**: 使用するマテリアルを`Assets/AMU_Variants/Material/`に自動コピー
- **エクスポートの最適化**: 変更のないマテリアルはコピー元を使用する設定でエクスポート
- **自動バックアップ**: アップロード前に自動的にアバターをunitypackageとして保存

### 📦 BoothPackageManager - Booth商品管理システム
- **購入商品の一覧表示**: Boothで購入した商品を画像付きで管理
- **ファイルの自動整理**: ImportフォルダやDownloadフォルダからファイルを自動分類・移動
- **ダウンロード状況の確認**: ローカルにファイルが存在するかどうかを視覚的に確認
## 使い方

### 基本的なワークフロー

1. **設定の確認**
   - `AMU > Setting`から設定画面を開く
   - 「自動でVariantを作成する」をONにする（推奨・デフォルト設定）
   - 「ビルド前に現在のアバターを保存する」をONにする（推奨・デフォルト設定）

2. **アバター作業**
   - 通常通りアバターのプレハブをシーンに配置
   - 自動的にVariantが作成され、マテリアルが編集用にコピーされる

3. **ビルド**
   - VRC SDKから通常通りアップロード
   - ビルド前に自動的にアバターがエクスポートされる

### BoothPackageManager の使い方

1. **ウィンドウを開く**
   - `AMU > Booth Package Manager`からウィンドウを開く

2. **商品データベースの取得・設定**
   - [BPMExtension](https://github.com/4OF-fof/BPMExtension)を使用してBPMlibrary.jsonを取得
   - 取得したBPMlibrary.jsonファイルをImportフォルダまたはDownloadフォルダに配置
   - 自動的に最新版のJSONが検出され、使用される

3. **ファイル管理**
   - 購入したファイルをImportフォルダまたはDownloadフォルダに配置
   - 自動的に適切なフォルダに分類・移動される

## 設定項目

### Core（共通設定）
- **表示言語**: `ja_jp` (日本語) / `en_us` (English)
- **データ保存フォルダ**: エクスポートファイルの保存先

### AutoVariant（自動化設定）
- **自動でVariantを作成する**: プレハブ追加時の自動Variant作成
- **ビルド前に現在のアバターを保存する**: ビルド前の自動エクスポート
- **関連アセットをすべて含める**: エクスポート時の依存関係包含範囲

### Booth Package Manager（商品管理設定）
- **Downloadフォルダも検索対象にする**: ファイル自動移動時の検索範囲拡張

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
Assets/AMU_Variants/
├── Material/                        # コピーされたマテリアル
│   ├── Material1.mat
│   └── Material2.mat
└── AMU_{OriginalName}.prefab   # Variantプレハブ
```

### BoothPackageManagerのファイル構造
```
{データ保存フォルダ}/
├── Import/                          # 手動配置用フォルダ
│   └── {各種ファイル}
└── BPM/                            # BoothPackageManagerデータ
    ├── BPMlibrary.json             # 商品データベース
    ├── thumbnail/                  # 商品画像キャッシュ
    │   └── {ハッシュ化画像ファイル}
    └── file/                       # ファイル保存フォルダ
        └── {作者名}/               # 作者別分類フォルダ
            └── {商品ID}/
                └── {商品ファイル}
```