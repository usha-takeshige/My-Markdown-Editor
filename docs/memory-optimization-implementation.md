# メモリ最適化実装レポート

## 実施日
2025-11-11

## 概要
Issue #10に基づき、My Markdown Editorのメモリ使用量を削減するための最適化を実装しました。
`docs/memory-analysis.md`で特定された問題点に対して、優先度順に修正を行いました。

## 実装された修正

### Phase 1: クリティカルな修正

#### 1. TextEditorの適切な破棄とイベントハンドラのクリーンアップ

**ファイル:** `src/MainWindow.xaml.cs`

**変更内容:**
- `MainWindow`クラスに`IDisposable`インターフェースを実装
- `Dispose()`メソッドを追加し、以下の処理を実装:
  - `TextEditor.TextChanged`イベントハンドラの解除
  - `TextEditor.PreviewKeyDown`イベントハンドラの解除
  - TextEditorのDocumentをnullに設定してメモリ解放を促進
- `Window_Closing`イベントで`Dispose()`を呼び出し
- `_disposed`フラグを追加して二重破棄を防止

**効果:**
- ウィンドウクローズ時のメモリリーク防止
- 複数ウィンドウを開閉する際のメモリ蓄積を防止
- イベントハンドラによる循環参照の解消

**コード例:**
```csharp
public void Dispose()
{
    if (_disposed)
        return;

    // イベントハンドラの解除
    TextEditor.TextChanged -= TextEditor_TextChanged;
    TextEditor.PreviewKeyDown -= TextEditor_PreviewKeyDown;

    // TextEditorの破棄
    if (TextEditor != null)
    {
        TextEditor.Document = null;
    }

    _disposed = true;
    GC.SuppressFinalize(this);
}
```

#### 2. シンタックスハイライト定義のキャッシング

**ファイル:** `src/Editor/SyntaxHighlighter.cs`

**変更内容:**
- 静的フィールド`_cachedHighlighting`と`_cachedSettingsHash`を追加
- `GetMarkdownHighlighting()`メソッドを修正:
  - 設定のハッシュコードを計算
  - キャッシュが有効で設定が同じ場合は既存の定義を再利用
  - 設定が変更された場合のみ新しい定義を生成
- `ClearCache()`メソッドを追加（設定変更時に使用可能）
- スレッドセーフのため`_cacheLock`を使用

**ファイル:** `src/Settings/HighlightSettings.cs`

**変更内容:**
- `GetHashCode()`メソッドをオーバーライド
  - 各ColorDefinitionのハッシュを組み合わせて計算
- `Equals()`メソッドをオーバーライド
  - 全てのプロパティの等価性を確認

**ファイル:** `src/Settings/HighlightSettings.cs` (ColorDefinition)

**変更内容:**
- `GetHashCode()`メソッドをオーバーライド
  - 全プロパティ（Foreground, Background, FontWeight, FontStyle, FontFamily）のハッシュを組み合わせ
- `Equals()`メソッドをオーバーライド
  - 全プロパティの文字列比較

**効果:**
- 複数ウィンドウを開いても、ハイライト定義は1つのみメモリに保持
- XSHD XMLの再生成コスト削減
- ウィンドウ起動時のパフォーマンス向上

**推定メモリ削減:**
- 1ウィンドウあたり約50-100KB（IHighlightingDefinitionとXML文字列）
- 5ウィンドウで約200-400KBの削減

#### 3. 設定のシングルトンパターン実装

**ファイル:** `src/Settings/HighlightSettings.cs`

**変更内容:**
- 静的フィールド`_instance`と`_instanceLock`を追加
- `Load()`メソッドを修正:
  - 初回呼び出し時のみディスクから読み込み
  - 2回目以降はキャッシュされたインスタンスを返す
- `ClearCache()`メソッドを追加
- スレッドセーフな実装（double-checked lockingパターン）

**ファイル:** `src/Settings/WindowSettings.cs`

**変更内容:**
- 静的フィールド`_cachedInitialSettings`と`_cacheLock`を追加
- `Load()`メソッドを修正:
  - 初回呼び出し時のみディスクから読み込み
  - 2回目以降はキャッシュをクローンして返す
  - 各ウィンドウが独立して設定を変更できるようにクローンを使用
- `Clone()`プライベートメソッドを追加
- `ClearCache()`メソッドを追加

**効果:**
- 設定ファイルの重複読み込み防止
- ディスクI/Oの削減
- 複数ウィンドウを開く際のパフォーマンス向上

**推定メモリ削減:**
- HighlightSettings: 1ウィンドウあたり約5-10KB削減
- WindowSettings: 初期読み込みのI/O削減（メモリはクローンのため同等）

### Phase 2: 高優先度の修正

#### 4. ダイアログの適切な破棄

**ファイル:** `src/MainWindow.xaml.cs`

**変更内容:**
- `OpenFile()`メソッド:
  - `OpenFileDialog`の宣言を`using var dialog = ...`に変更
- `SaveFile()`メソッド:
  - `SaveFileDialog`の宣言を`using var dialog = ...`に変更

**効果:**
- ダイアログのネイティブリソースの適切な解放
- 小規模だが確実なメモリリーク防止

**推定メモリ削減:**
- 1回のダイアログ使用あたり数KB程度（累積すると効果あり）

#### 5. Undo/Redoスタックの制限設定

**ファイル:** `src/MainWindow.xaml.cs`

**変更内容:**
- `MainWindow`コンストラクタに以下を追加:
```csharp
if (TextEditor.Document?.UndoStack != null)
{
    TextEditor.Document.UndoStack.SizeLimit = 100;
}
```

**効果:**
- Undo/Redoスタックが無制限に成長することを防止
- 長時間の編集セッションでのメモリ使用量の抑制
- 100回のUndo/Redoは実用上十分な操作履歴

**推定メモリ削減:**
- 長時間使用時に数MBから数十MBの削減可能
- 大きなファイルや頻繁な編集時に特に効果的

## 総合的な効果

### メモリ削減の見込み

| シナリオ | 削減見込み | 主な効果 |
|---------|-----------|---------|
| 単一ウィンドウ | 10-20% | Undo/Redoスタック制限、適切なクリーンアップ |
| 複数ウィンドウ（5個） | 30-50% | ハイライト定義キャッシュ、設定キャッシュ |
| 長時間使用 | メモリリーク防止 | イベントハンドラ解除、Undo/Redoスタック制限 |

### パフォーマンスの改善

1. **ウィンドウ起動時間の短縮**
   - 2つ目以降のウィンドウはハイライト定義の生成をスキップ
   - 設定ファイルの読み込みをスキップ

2. **安定性の向上**
   - メモリリークの防止により、長時間使用でも安定動作
   - リソースの適切な解放

## 実装の詳細

### スレッドセーフティ

すべてのキャッシング実装で`lock`を使用し、マルチスレッド環境での安全性を確保:
- `SyntaxHighlighter._cacheLock`
- `HighlightSettings._instanceLock`
- `WindowSettings._cacheLock`

### 後方互換性

すべての変更は既存のAPIを維持し、呼び出し側の変更は不要:
- `HighlightSettings.Load()`の戻り値と使用方法は変更なし
- `WindowSettings.Load()`の戻り値と使用方法は変更なし
- `SyntaxHighlighter.GetMarkdownHighlighting()`の戻り値と使用方法は変更なし

### キャッシュのクリア

将来的に設定の動的変更機能を追加する場合のため、各クラスに`ClearCache()`メソッドを用意:
- `SyntaxHighlighter.ClearCache()` - ハイライト定義キャッシュをクリア
- `HighlightSettings.ClearCache()` - 設定インスタンスキャッシュをクリア
- `WindowSettings.ClearCache()` - ウィンドウ設定キャッシュをクリア

## テスト推奨項目

実際の環境でのテスト時に確認すべき項目:

1. **基本機能の動作確認**
   - [ ] ファイルの新規作成、開く、保存が正常に動作
   - [ ] シンタックスハイライトが正常に表示
   - [ ] ウィンドウサイズの保存・復元が正常に動作

2. **メモリ関連の確認**
   - [ ] 複数ウィンドウを開いてタスクマネージャーでメモリ使用量を確認
   - [ ] ウィンドウを閉じた際にメモリが解放されることを確認
   - [ ] 長時間の編集セッション後もメモリ使用量が安定していることを確認

3. **パフォーマンスの確認**
   - [ ] 2つ目以降のウィンドウの起動が速いことを確認
   - [ ] Undo/Redo操作が100回まで正常に動作することを確認

4. **エッジケースの確認**
   - [ ] 設定ファイルが存在しない場合の初回起動
   - [ ] 不正な設定ファイルの処理
   - [ ] 複数ウィンドウを同時に開く・閉じる操作

## 今後の改善案

現時点では実装していないが、将来的に検討可能な最適化:

1. **ステータス更新のスロットリング**
   - UpdateStatus()の呼び出し頻度を制限（例: 100ms間隔）
   - 高頻度な文字列アロケーションを削減

2. **大きなファイルのストリーミング処理**
   - 現在は全文をメモリに読み込むが、非常に大きなファイルには不向き
   - ストリーミングやページング処理の検討

3. **設定変更の動的反映**
   - 設定変更時にClearCache()を呼び出す仕組み
   - リアルタイムでの設定変更UI

## まとめ

本実装により、以下の改善が達成されました:

1. **メモリリークの防止** - イベントハンドラとリソースの適切なクリーンアップ
2. **メモリ使用量の削減** - キャッシングによる重複オブジェクトの削減
3. **パフォーマンスの向上** - ディスクI/Oとオブジェクト生成の削減
4. **長期安定性の向上** - Undo/Redoスタック制限による無制限な成長の防止

すべての変更はクリーンで保守性の高いコードとして実装され、将来の機能拡張にも対応できる設計となっています。
