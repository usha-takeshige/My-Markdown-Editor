# メモリ使用量分析と最適化計画

## 実施日
2025-11-11

## 概要
My Markdown Editorのメモリ使用量を削減するため、コードベースを分析し、メモリリークや非効率なメモリ使用パターンを特定しました。

## 発見された問題点

### 【重大】クリティカルな問題

#### 1. イベントハンドラの未解除
**ファイル:** `src/MainWindow.xaml.cs`
- **行:** 178, 267
- **問題:** `TextEditor_TextChanged`と`TextEditor_PreviewKeyDown`イベントハンドラがXAMLで登録されているが、ウィンドウクローズ時に明示的に解除されていない
- **影響:** TextEditorインスタンスが生き続ける場合、メモリリークの原因となる。特に複数ウィンドウを開く場合に問題
- **優先度:** 高

#### 2. TextEditorの未破棄
**ファイル:** `src/MainWindow.xaml.cs`
- **行:** 39-47 (XAML)
- **問題:** AvalonEdit TextEditorコントロールが明示的にDispose()されていない
- **影響:** AvalonEditは内部データ構造、Undo/Redoスタック、イベントハンドラを保持しており、適切にクリーンアップする必要がある
- **優先度:** 高

#### 3. 複数ウィンドウの追跡管理なし
**ファイル:** `src/MainWindow.xaml.cs`
- **行:** 71-72
- **問題:** `NewFile()`が新しいMainWindowインスタンスを作成するが、それらを追跡していない
- **影響:** 各ウィンドウが独自に設定をロードし、シンタックスハイライトを生成し、個別のリソースを保持。孤立したウィンドウをクリーンアップする仕組みがない
- **優先度:** 中

### 【高】高優先度の問題

#### 4. シンタックスハイライトの重複生成
**ファイル:** `src/MainWindow.xaml.cs`
- **行:** 57
- **問題:** `SyntaxHighlighter.GetMarkdownHighlighting()`が各ウィンドウごとに新しいハイライト定義を生成

**ファイル:** `src/Editor/SyntaxHighlighter.cs`
- **行:** 25-46, 56-98
- **問題:** 呼び出しごとにStringBuilderで完全なXSHD XMLを生成し、`GenerateXshd()`が毎回XML構造全体を再生成
- **影響:** 複数ウィンドウを開くと、重複したIHighlightingDefinitionオブジェクトがメモリに存在
- **優先度:** 高

#### 5. ウィンドウごとに設定をディスクから読み込み
**ファイル:** `src/MainWindow.xaml.cs`
- **行:** 56, 60
- **問題:** `HighlightSettings.Load()`と`WindowSettings.Load()`が新しいウィンドウごとにディスクから読み込み

**ファイル:** `src/Settings/HighlightSettings.cs`, `src/Settings/WindowSettings.cs`
- **行:** HighlightSettings.cs 47-77, WindowSettings.cs 28-43
- **問題:** Load()呼び出しごとにファイルI/Oが発生
- **影響:** 不要なディスクI/Oと、メモリ内の重複オブジェクト
- **優先度:** 高

### 【中】中優先度の問題

#### 6. 大きなファイル内容をメモリに保持
**ファイル:** `src/FileManager/FileService.cs`
- **行:** 22, 40
- **問題:** `File.ReadAllText()`がファイル全体を単一の文字列としてメモリにロード
- **影響:** 非常に大きなマークダウンファイルの場合、大量のメモリを消費する可能性
- **優先度:** 中（個人メモ用途なので大きなファイルは稀）

#### 7. ドキュメント操作の明示的なクリーンアップなし
**ファイル:** `src/MainWindow.xaml.cs`
- **行:** 343, 347, 358, 361
- **問題:** Insert/Remove操作による直接的なドキュメント操作
- **影響:** Undo/Redoスタックが無制限に成長
- **優先度:** 中

#### 8. ダイアログオブジェクトの未破棄
**ファイル:** `src/MainWindow.xaml.cs`
- **行:** 85-89, 113-118
- **問題:** OpenFileDialogとSaveFileDialogが作成されるがDispose()されていない
- **影響:** 軽微なメモリリーク（通常はGCが処理）
- **優先度:** 低

### 【低】低優先度の問題

#### 9. ステータス更新での文字列連結
**ファイル:** `src/MainWindow.xaml.cs`
- **行:** 171-176
- **問題:** テキスト変更ごとに`UpdateStatus()`が呼ばれ、文字列補間で新しい文字列を頻繁に作成
- **影響:** タイピング中の高頻度な文字列アロケーション
- **優先度:** 低

#### 10. StringBuilder の再利用なし
**ファイル:** `src/Editor/SyntaxHighlighter.cs`
- **行:** 56
- **問題:** 毎回新しいStringBuilderを作成
- **影響:** 軽微（ハイライト設定が頻繁に再生成されない限り）
- **優先度:** 低

## 良好な実装

以下は適切に実装されている点：

1. **正規表現パターンがstatic readonly** (`src/Editor/MarkdownAssistant.cs` 11-13行) - メモリ効率が良い
2. **Readerにusingステートメントを使用** (`src/Editor/SyntaxHighlighter.cs` 26-28, 37-38行) - 適切な破棄
3. **UTF8Encodingオブジェクトがインライン** (`src/FileManager/FileService.cs` 22, 40行) - キャッシュ不要
4. **RelayCommandが弱いイベントを使用** (`src/MainWindow.xaml.cs` 382-386行) - CommandManager.RequerySuggestedは安全

## 最適化計画（優先度順）

### Phase 1: クリティカルな修正（即時実施）

1. **TextEditorの適切な破棄とイベントハンドラのクリーンアップ**
   - `MainWindow.xaml.cs`にIDisposableパターンを実装
   - Window_Closingイベントでイベントハンドラを解除
   - TextEditor.Dispose()を呼び出し

2. **シンタックスハイライト定義のキャッシュ**
   - `SyntaxHighlighter`にstaticキャッシュを追加
   - ハイライト設定が変更された場合のみ再生成
   - すべてのウィンドウで同じ定義を共有

3. **設定のシングルトンパターン実装**
   - `HighlightSettings`と`WindowSettings`をシングルトンに変更
   - アプリケーションレベルでキャッシュ
   - 設定変更時にのみディスクから再読み込み

### Phase 2: 高優先度の修正

4. **ダイアログの適切な破棄**
   - OpenFileDialogとSaveFileDialogにusingステートメントを使用

5. **Undo/Redoスタックの制限設定**
   - TextEditorのUndoStack.SizeLimitを設定（例: 100操作）

### Phase 3: 中優先度の最適化

6. **ステータス更新のスロットリング**
   - UpdateStatus呼び出しを制限（例: 100ms間隔）
   - または、文字列ビルダーの使用を検討

## 期待される効果

### メモリ削減見込み

- **単一ウィンドウ:** 10-20% のメモリ削減
- **複数ウィンドウ（5個）:** 30-50% のメモリ削減
- **長時間使用:** メモリリークの防止により、時間経過によるメモリ増加を抑制

### パフォーマンス向上

- ウィンドウ起動時間の短縮（設定とハイライトのキャッシュ効果）
- より安定した長時間動作

## 実装スケジュール

1. Phase 1の実装と検証
2. Phase 2の実装と検証
3. 総合的なメモリプロファイリングとテスト
4. Phase 3は必要に応じて実装

## 備考

- 各フェーズ後にメモリプロファイラーでの検証を推奨
- 複数ウィンドウを開いて長時間使用するシナリオでテストが重要
