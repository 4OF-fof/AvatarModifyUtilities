# MEMO: 技術的負債と今後の改善点

## 目次
- [後方互換性のための技術的負債](#後方互換性のための技術的負債)
- [リファクタリング履歴](#リファクタリング履歴)
- [今後の改善計画](#今後の改善計画)

---

## 後方互換性のための技術的負債

### 2025年6月17日: Coreモジュールリファクタリング

#### 1. BackwardCompatibilityAliases.cs
**ファイル:** `Editor/Core/Services/BackwardCompatibilityAliases.cs`

**目的:** 旧API名から新API名への橋渡し

**問題点:**
- 本来は不要な重複実装
- メモリ使用量の増加
- メンテナンスコストの増加

**内容:**
```csharp
// ObjectCaptureHelper エイリアス（非推奨）
namespace AMU.Editor.Core.Helper
{
    [System.Obsolete("Use AMU.Editor.Core.API.ObjectCaptureAPI instead", false)]
    public static class ObjectCaptureHelper
    {
        // ObjectCaptureAPI.CaptureObject への単純なリダイレクト
    }
}

// PipelineManagerHelper エイリアス（非推奨）
namespace AMU.Editor.Core.Helper
{
    [System.Obsolete("Use AMU.Editor.Core.API.VRChatAPI instead", false)]
    public static class PipelineManagerHelper
    {
        // VRChatAPI メソッドへの単純なリダイレクト
    }
}

// AMUInitializer エイリアス（非推奨）
namespace AMU.Editor.Initializer
{
    [System.Obsolete("Use AMU.Editor.Core.Services.InitializationService instead", false)]
    public static class AMUInitializer
    {
        // InitializationService.Initialize への単純なリダイレクト
    }
}
```

**削除時期の目安:**
- Phase 2（6ヶ月後）: 警告レベルを上げる
- Phase 3（1年後）: 完全削除

#### 2. LocalizationController.cs 内の後方互換性実装
**ファイル:** `Editor/Core/Controllers/LocalizationController.cs`

**問題点:**
- 同一ファイル内での名前空間混在
- 2つの実装パスの維持

**内容:**
```csharp
namespace AMU.Data.Lang
{
    /// <summary>
    /// 後方互換性のためのpartialクラス実装
    /// </summary>
    public static partial class LocalizationManager
    {
        // LocalizationController への単純なリダイレクト
        public static string CurrentLanguage => AMU.Editor.Core.Controllers.LocalizationController.CurrentLanguage;
        public static void LoadLanguage(string languageCode) { /* リダイレクト */ }
        public static string GetText(string key) { /* リダイレクト */ }
    }
}
```

**改善案:**
- 別ファイルに分離する
- より明確な警告メッセージの追加

#### 3. Helper.meta ファイルの残存
**ファイル:** `Editor/Core/Helper.meta`

**問題点:**
- 削除されたディレクトリのメタファイルが残存
- Unityプロジェクトでの不整合の可能性

**対応予定:**
- 次回のクリーンアップで削除

---

## リファクタリング履歴

### 2025年6月17日: Coreモジュール3層アーキテクチャ化

#### 実施内容
1. **Helper ディレクトリの廃止**
   - `AMUInitializer.cs` → `Services/InitializationService.cs`
   - `LocalizationHelper.cs` → `Controllers/LocalizationController.cs`
   - `ObjectCaptureHelper.cs` → `API/ObjectCaptureAPI.cs`
   - `PipelineManagerHelper.cs` → `API/VRChatAPI.cs`

2. **新しいディレクトリ構造の導入**
   ```
   Core/
   ├── API/           # 外部公開機能
   ├── Controllers/   # データ管理
   ├── Services/      # 初期化・サービス
   ├── Data/          # 既存（変更なし）
   └── UI/            # 既存（変更なし）
   ```

3. **機能強化**
   - 型安全な設定管理（SettingsController）
   - エラーハンドリングの改善
   - ログ出力の統一
   - XMLドキュメンテーションの追加

#### 影響範囲
- **変更なし**: UI層、既存の公開インターフェース
- **新規追加**: API、Controllers、Services層
- **非推奨**: 旧Helper系クラス（動作は継続）

#### 移行状況
- **自動移行済み**: 内部呼び出し
- **手動移行必要**: 外部モジュールでの旧API使用箇所
  - `AutoVariant/Watcher/AvatarExporter.cs`
  - `AutoVariant/Watcher/ConvertVariant.cs`
  - `AutoVariant/Watcher/AvatarValidator.cs`

---

## 今後の改善計画

### Phase 1: 安定化期間（現在〜3ヶ月）
- [ ] 新アーキテクチャの動作確認
- [ ] パフォーマンステスト
- [ ] ドキュメント整備
- [ ] 外部モジュールでの動作確認

### Phase 2: 移行推進期間（3〜9ヶ月）
- [ ] 外部モジュールの段階的移行
  - [ ] AutoVariantモジュール
  - [ ] AssetManagerモジュール
  - [ ] その他依存モジュール
- [ ] Obsolete警告レベルの引き上げ
- [ ] 新機能の新アーキテクチャでの実装

### Phase 3: 完全移行期間（9〜12ヶ月）
- [ ] 後方互換性エイリアスの削除
- [ ] 不要なファイル・コードの完全削除
- [ ] パフォーマンス最適化
- [ ] 最終動作確認

### 技術的改善項目

#### 高優先度
1. **Helper.meta ファイルの削除**
   - 影響: 低
   - 工数: 小
   - 時期: 次回メンテナンス時

2. **LocalizationController の分離**
   - 影響: 中
   - 工数: 中
   - 時期: Phase 2開始時

3. **外部モジュールの移行**
   - 影響: 高
   - 工数: 大
   - 時期: Phase 2期間中

#### 中優先度
1. **設定項目の型定義強化**
   - SettingsControllerでのより厳密な型チェック
   - カスタム設定型のサポート

2. **エラーハンドリングの統一**
   - カスタム例外クラスの導入
   - エラー報告機能の強化

3. **ローカライゼーション機能の拡張**
   - フォールバック言語の実装
   - 動的言語切り替えの改善

#### 低優先度
1. **パフォーマンス最適化**
   - 設定アクセスのキャッシュ化
   - ローカライゼーションの遅延読み込み

2. **テスト基盤の整備**
   - 単体テストの追加
   - 統合テストの実装

3. **監視・ログ機能の強化**
   - 詳細な使用状況の追跡
   - パフォーマンスメトリクスの収集

### 削除予定コード一覧

#### 即座に削除可能（次回メンテナンス時）
- `Editor/Core/Helper.meta`

#### Phase 2で削除
- `BackwardCompatibilityAliases.cs` 内の一部エイリアス
- 使用されなくなったObsoleteメソッド

#### Phase 3で削除
- `BackwardCompatibilityAliases.cs` 全体
- `LocalizationController.cs` 内の後方互換性実装
- 未使用の設定項目

### 注意事項

1. **破壊的変更の回避**
   - Public APIの変更は最小限に
   - 段階的な移行期間の確保

2. **文書化の徹底**
   - 変更内容の明確な記録
   - 移行ガイドの継続更新

3. **テストの充実**
   - 後方互換性の継続確認
   - 新機能の動作確認

4. **コミュニティへの配慮**
   - 十分な移行期間の提供
   - 明確な移行ガイドの提供

---

**最終更新:** 2025年6月17日  
**次回レビュー予定:** 2025年9月17日
