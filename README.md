# Avatar Modify Utilities(AMU)

**AMU**は、VRChatアバター改変を効率化するUnityエディタ拡張ツールです。プレハブのVariant自動作成、ビルド前の自動保存機能、アセットの管理画面を提供します。

## 主な機能

### 自動Variant作成システム
- **プレハブ追加の自動監視**: シーンにプレハブを追加すると自動的にVariantを作成、置換を行います
- **マテリアルの自動管理**: プレハブに関連するマテリアルを`Assets/AMU_Variants/Material/`に自動でコピーします
- **エクスポートの最適化**: 変更のないマテリアルはオリジナルの物を使用する設定でエクスポートします
- **自動バックアップ**: VRchatへのアップロード前に自動的にアバターをunitypackageとして保存します

### アセット管理システム
- **Boothからのインポート**: [Chrome拡張](https://github.com/4OF-fof/AMU_BoothExporter)を利用して取得したjsonファイルを読み込むことで、Boothで購入したアセットを自動で登録します
- **ファイルの分類**: タグやカテゴリ、アセット名などの情報から所有アセットの絞り込みができます
- **依存関係の解消**: 依存するアセットを登録することで、必要な時に自動で依存アセットがインポートされます

## 出力ファイル

### 作成されるVariant
```
Assets/AMU_Variants/
├── Material/                   # コピーされたマテリアル
│   ├── Material1.mat
│   └── Material2.mat
└── AMU_{OriginalName}.prefab   # Variantプレハブ
```

### エクスポートされるUnityPackage
```
{データ保存フォルダ}/AutoVariant/
├── {blueprintId}/                    # アップロード済みアバター
│   └── YYMMDD-001.unitypackage
└── local/                            # ローカルアバター
    └── YYMMDD-{avatarName}-001.unitypackage
```

### VRCAssetManagerのデータ

```
{データ保存フォルダ}/VrcAssetManager/
├── BoothItem/
│   ├── Package/                # Boothアイテムの商品ファイル
│   └── Thumbnail/              # Boothアイテムのサムネイル画像
├── Package/                    # 手動登録したアセット
├── Thumbnail/                  # 手動登録したサムネイル画像
├── Unzip/                      # インポート用にZipから抽出されたアセット
└── AssetLibrary.json           # アセットの情報データベース
```