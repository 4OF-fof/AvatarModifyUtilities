# Untitled

モジュールベースのUnity拡張機能群

## 特徴

- `Core`による設定画面と多言語対応サポート

## ディレクトリ構成

- `Editor/`
  - `Core/` : コア機能
    - `Data/` : データ
      - `Structure/` : 構造体の定義
      - `lang/`
        - `{lang_code}.json` : 翻訳テキスト
        - `TextField.cs` : 翻訳対象テキストのキー
      - `Setting.cs` : 一般設定
    - `UI` : 画面実装
  - `Sample/` : サンプル実装
- `doc/` : ドキュメント
  - `README.md` : このファイル