# Services層 - AutoVariant

## 概要

Services層は、AutoVariantモジュールの初期化処理とサービス機能を担当します。エディターイベントの監視、自動処理の実行、VRCSDKとの統合などの長時間実行されるサービスを管理します。

## クラス構成

### ConvertVariantService

#### 目的
プレハブがシーンに追加された際の自動変換処理を監視・実行します。

#### 特徴
- `[InitializeOnLoad]`による自動初期化
- Hierarchyの変更監視
- プレハブバリアントの自動生成

#### ライフサイクル

##### `Initialize()`
サービスを初期化し、イベントハンドラーを登録します。

**処理内容:**
- AutoVariant有効性の確認
- EditorApplicationイベントの登録
- 処理状態の初期化

##### `Shutdown()`
サービスを停止し、イベントハンドラーの登録を解除します。

**処理内容:**
- イベントハンドラーの解除
- 処理状態のクリア

#### 監視・処理メソッド

##### Hierarchy変更監視
- **`OnHierarchyChanged()`**: Hierarchy変更時の処理
- **`FindAddedPrefabRoots()`**: 新規追加されたプレハブルートの検出
- **`HandlePrefabAddition(GameObject)`**: プレハブ追加時の処理

##### プレハブ処理
- **`CopyAndReplaceMaterials(GameObject, string)`**: マテリアルのコピーと置換
- **`ReplaceWithVariant(GameObject, string)`**: バリアントプレハブとの置換

#### 処理フロー

```
1. Hierarchy変更検出
   ↓
2. 新規プレハブの特定
   ↓
3. AMU_Variantsディレクトリ作成
   ↓
4. マテリアルコピー・置換
   ↓
5. プレハブバリアント作成
   ↓
6. シーンオブジェクトの置換
```

### MaterialOptimizationService

#### 目的
アクティブアバターのマテリアル最適化処理を管理します。

#### 主要機能
- マテリアル状態の保存・復元
- ネストされたプレハブの最適化
- エクスポート処理との連携

#### 主要メソッド

##### `OptimizeActiveAvatars()`
シーン内のすべてのアクティブアバターを最適化します。

**処理フロー:**
1. アクティブアバターの検索
2. マテリアル状態の保存
3. 各アバターの最適化実行
4. マテリアル状態の復元

##### `OptimizeAvatar(GameObject avatar)`
指定されたアバターのみを最適化します。

**パラメータ:**
- `avatar`: 最適化対象のアバター

##### ネストプレハブ最適化
- **`OptimizeNestedPrefabs(GameObject)`**: ネストされたプレハブの最適化
- **`OptimizeNestedPrefabsRecursive(string, HashSet<string>)`**: 再帰的な最適化処理

#### マテリアル状態管理

##### RendererMaterialState クラス
マテリアルの元の状態を保存するためのクラスです。

**プロパティ:**
- `renderer`: 対象のRenderer
- `originalMaterials`: 元のマテリアル配列

**メソッド:**
- `RestoreMaterials()`: マテリアルを元の状態に復元

### AvatarValidationService

#### 目的
アバターの状態確認と検証を行います。

#### 主要機能
- アクティブアバターの検索
- アバター数の検証
- VRCアバターの判定

#### 主要メソッド

##### `ValidateAvatarCount()`
アクティブなアバター数を検証し、複数ある場合は警告を表示します。

**戻り値:**
- `bool`: 検証が成功したかどうか（1体以下の場合true）

##### `FindActiveAvatars()`
シーン内のアクティブなVRCアバターを検索します。

**戻り値:**
- `GameObject[]`: アクティブなアバターの配列

##### `GetSingleActiveAvatar()`
単一のアクティブアバターを取得します。

**戻り値:**
- `GameObject`: アクティブなアバター（複数の場合null）

##### `IsVRCAvatar(GameObject obj)`
指定されたGameObjectがVRCアバターかどうかを判定します。

**パラメータ:**
- `obj`: 判定対象のGameObject

**戻り値:**
- `bool`: VRCアバターかどうか

#### 多言語対応
エラーダイアログの表示は、現在の言語設定に基づいて表示されます。

**サポート言語:**
- 日本語 (`ja_jp`)
- 英語 (`en_us`) - デフォルト

### PrebuildService

#### 目的
VRCSDKビルドプロセスに統合される前処理を管理します。

#### VRCSDKコールバック実装
`IVRCSDKBuildRequestedCallback`インターフェースを実装し、ビルド要求時に自動実行されます。

#### 主要メソッド

##### `OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)`
VRCSDKビルドが要求された際に呼び出されます。

**パラメータ:**
- `requestedBuildType`: 要求されたビルドタイプ

**戻り値:**
- `bool`: ビルドを続行するかどうか

**処理フロー:**
1. アバター数の検証
2. 最適化設定の確認
3. 最適化処理の実行

## サービス間の連携

### 初期化順序
1. ConvertVariantService（エディター起動時）
2. その他のサービス（必要時に初期化）

### データの流れ
```
ConvertVariantService → MaterialOptimizationService → AvatarExportAPI
                     ↓
              AvatarValidationService ← PrebuildService
```

### イベント連携
- ConvertVariantService: EditorApplication.hierarchyChanged
- MaterialOptimizationService: API呼び出し
- AvatarValidationService: 他サービスからの呼び出し
- PrebuildService: VRCSDK Build Pipeline

## エラーハンドリング

### ConvertVariantService
- プレハブ処理中の例外: ログ出力後、処理を継続
- ディレクトリ作成失敗: エラーログ出力後、処理をスキップ

### MaterialOptimizationService
- 最適化処理中の例外: マテリアル状態を復元してからエラーログ出力
- Renderer参照が無効: 警告ログ出力後、処理を継続

### AvatarValidationService
- アバター検索中の例外: エラーログ出力後、空の配列を返す
- ダイアログ表示失敗: コンソールログで代替メッセージ出力

### PrebuildService
- 検証・最適化失敗: エラーログ出力後、falseを返してビルドを中止

## パフォーマンス考慮事項

### ConvertVariantService
- 処理済みInstanceIDのキャッシュで重複処理を防止
- 5秒間隔でキャッシュをクリア
- プレハブステージ中は処理をスキップ

### MaterialOptimizationService
- マテリアル状態の保存を最小限に抑制
- 一時的なプレハブインスタンスの適切な破棄

### AvatarValidationService
- アバター検索は必要時のみ実行
- 結果のキャッシュは実装せず、リアルタイム性を重視

## 設定依存関係

### ConvertVariantService
- `Setting.AutoVariant_enableAutoVariant`: サービスの有効性制御

### MaterialOptimizationService
- 設定への直接依存なし（API層経由で間接的に依存）

### AvatarValidationService
- `Setting.Core_language`: エラーメッセージの言語選択

### PrebuildService
- `Setting.AutoVariant_enablePrebuild`: 最適化処理の有効性制御

## 今後の改善予定

1. **非同期処理対応**: 重い処理の非同期化
2. **プログレス表示**: 長時間処理のプログレス表示
3. **エラー回復機能**: 処理失敗時の自動回復
4. **ログ統合**: 統一ログシステムとの連携
5. **設定変更の動的反映**: 設定変更時のサービス再初期化
6. **バッチ処理最適化**: 複数プレハブの一括処理
