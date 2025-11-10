# ショートカットキー機能 実装計画

## 概要

マークダウン編集を効率化するためのショートカットキー機能を追加します。
シンタックスハイライト用のマークアップ挿入と、インデント操作の2つのカテゴリーに分かれています。

**実装予定日**: 2025-11-10
**対象バージョン**: 1.1

---

## 要件定義

### 1. シンタックスハイライト補助機能

選択範囲を特定の記号で囲む、または記号ペアを挿入する機能：

| ショートカット | 機能 | 選択時の動作 | 未選択時の動作 |
|---------------|------|-------------|---------------|
| Ctrl+B | 太字 | `**選択範囲**` | `****` を挿入（カーソルを中央に配置） |
| Ctrl+I | 斜体 | `*選択範囲*` | `**` を挿入（カーソルを中央に配置） |
| Ctrl+2 | ダブルクォート | `"選択範囲"` | `""` を挿入（カーソルを中央に配置） |
| Ctrl+@ | インラインコード | `` `選択範囲` `` | `` `` `` を挿入（カーソルを中央に配置） |

### 2. インデント操作機能

リストや引用のインデントを操作する機能：

| ショートカット | 機能 | 動作 |
|---------------|------|------|
| Ctrl+] | インデント増加 | 現在行の先頭にタブ（または2スペース）を挿入 |
| Ctrl+[ | インデント減少 | 現在行の先頭からタブ（または2スペース）を削除 |
| Tab | インデント増加 | 現在行の先頭にタブ（または2スペース）を挿入 |

**注意**: Tabキーは通常のテキスト入力時はデフォルト動作を維持し、リスト行でのみインデント操作として機能します。

---

## 技術設計

### アーキテクチャ

新しいヘルパークラスを作成し、既存の`MainWindow.xaml.cs`と連携します。

```
src/
├── Editor/
│   ├── SyntaxHighlighter.cs      (既存)
│   ├── MarkdownAssistant.cs      (既存)
│   └── TextFormatHelper.cs       (新規) ← テキスト整形機能
└── MainWindow.xaml.cs            (拡張)
```

### 新規クラス: TextFormatHelper

**役割**: 選択範囲の囲み処理とインデント操作のロジックを提供

**メソッド設計**:

```csharp
namespace MyMarkdownEditor.Editor;

public static class TextFormatHelper
{
    /// <summary>
    /// 選択範囲を指定の記号で囲む（選択がない場合は記号ペアを挿入）
    /// </summary>
    /// <param name="document">ドキュメント</param>
    /// <param name="selectionStart">選択開始位置</param>
    /// <param name="selectionLength">選択長</param>
    /// <param name="prefix">前置記号</param>
    /// <param name="suffix">後置記号（nullの場合はprefixと同じ）</param>
    /// <returns>新しいキャレット位置</returns>
    public static int WrapSelection(
        ICSharpCode.AvalonEdit.Document.TextDocument document,
        int selectionStart,
        int selectionLength,
        string prefix,
        string? suffix = null)
    {
        // 実装内容:
        // - 選択がある場合: 選択範囲を記号で囲む
        // - 選択がない場合: 記号ペアを挿入し、カーソルを中央に配置
    }

    /// <summary>
    /// 現在行のインデントを増やす
    /// </summary>
    /// <param name="document">ドキュメント</param>
    /// <param name="caretOffset">現在のキャレット位置</param>
    /// <param name="useSpaces">スペースを使用するか（true: 2スペース、false: タブ）</param>
    /// <returns>新しいキャレット位置</returns>
    public static int IncreaseIndent(
        ICSharpCode.AvalonEdit.Document.TextDocument document,
        int caretOffset,
        bool useSpaces = true)
    {
        // 実装内容:
        // - 現在行の先頭にタブまたは2スペースを挿入
        // - キャレット位置を調整
    }

    /// <summary>
    /// 現在行のインデントを減らす
    /// </summary>
    /// <param name="document">ドキュメント</param>
    /// <param name="caretOffset">現在のキャレット位置</param>
    /// <param name="useSpaces">スペースを使用するか（true: 2スペース、false: タブ）</param>
    /// <returns>新しいキャレット位置</returns>
    public static int DecreaseIndent(
        ICSharpCode.AvalonEdit.Document.TextDocument document,
        int caretOffset,
        bool useSpaces = true)
    {
        // 実装内容:
        // - 現在行の先頭からタブまたは2スペースを削除
        // - キャレット位置を調整
    }

    /// <summary>
    /// 現在行がリストまたは引用であるかを判定
    /// </summary>
    /// <param name="lineText">行テキスト</param>
    /// <returns>リストまたは引用の場合true</returns>
    public static bool IsListOrQuoteLine(string lineText)
    {
        // 実装内容:
        // - MarkdownAssistantの既存パターンを利用
        // - リスト、番号付きリスト、引用のパターンにマッチするか判定
    }
}
```

### MainWindow.xaml.csの拡張

**追加コマンド**:

```csharp
public ICommand FormatBoldCommand { get; }        // Ctrl+B
public ICommand FormatItalicCommand { get; }      // Ctrl+I
public ICommand FormatQuoteCommand { get; }       // Ctrl+2
public ICommand FormatInlineCodeCommand { get; }  // Ctrl+@
public ICommand IncreaseIndentCommand { get; }    // Ctrl+]
public ICommand DecreaseIndentCommand { get; }    // Ctrl+[
```

**コンストラクタでの初期化**:

```csharp
FormatBoldCommand = new RelayCommand(FormatBold);
FormatItalicCommand = new RelayCommand(FormatItalic);
FormatQuoteCommand = new RelayCommand(FormatQuote);
FormatInlineCodeCommand = new RelayCommand(FormatInlineCode);
IncreaseIndentCommand = new RelayCommand(IncreaseIndent);
DecreaseIndentCommand = new RelayCommand(DecreaseIndent);
```

**コマンド実装メソッド**:

```csharp
private void FormatBold()
{
    int selectionStart = TextEditor.SelectionStart;
    int selectionLength = TextEditor.SelectionLength;
    int newCaretOffset = TextFormatHelper.WrapSelection(
        TextEditor.Document, selectionStart, selectionLength, "**");
    TextEditor.CaretOffset = newCaretOffset;
    TextEditor.Select(0, 0); // 選択解除
    TextEditor.Focus();
}

// FormatItalic, FormatQuote, FormatInlineCode も同様の構造

private void IncreaseIndent()
{
    int caretOffset = TextEditor.CaretOffset;
    int newCaretOffset = TextFormatHelper.IncreaseIndent(
        TextEditor.Document, caretOffset, useSpaces: true);
    TextEditor.CaretOffset = newCaretOffset;
    TextEditor.Focus();
}

// DecreaseIndent も同様の構造
```

**Tabキーの特殊処理**:

`TextEditor_PreviewKeyDown`メソッドを拡張：

```csharp
private void TextEditor_PreviewKeyDown(object sender, KeyEventArgs e)
{
    // Tabキー処理
    if (e.Key == Key.Tab)
    {
        // 現在行を取得
        int caretOffset = TextEditor.CaretOffset;
        var document = TextEditor.Document;
        var line = document.GetLineByOffset(caretOffset);
        string lineText = document.GetText(line.Offset, line.Length);

        // リストまたは引用の場合はインデント操作
        if (TextFormatHelper.IsListOrQuoteLine(lineText))
        {
            e.Handled = true;
            IncreaseIndent();
            return;
        }
    }

    // Enterキー処理（既存のコード）
    if (e.Key == Key.Enter)
    {
        // ... 既存の実装
    }
}
```

### MainWindow.xamlの拡張

**InputBindingsに追加**:

```xml
<Window.InputBindings>
    <!-- 既存のキーバインディング -->
    <KeyBinding Key="N" Modifiers="Control" Command="{Binding NewCommand}" />
    <KeyBinding Key="O" Modifiers="Control" Command="{Binding OpenCommand}" />
    <KeyBinding Key="S" Modifiers="Control" Command="{Binding SaveCommand}" />

    <!-- 新規: シンタックスハイライト補助 -->
    <KeyBinding Key="B" Modifiers="Control" Command="{Binding FormatBoldCommand}" />
    <KeyBinding Key="I" Modifiers="Control" Command="{Binding FormatItalicCommand}" />
    <KeyBinding Key="D2" Modifiers="Control" Command="{Binding FormatQuoteCommand}" />
    <KeyBinding Key="OemAtSign" Modifiers="Control" Command="{Binding FormatInlineCodeCommand}" />

    <!-- 新規: インデント操作 -->
    <KeyBinding Key="OemCloseBrackets" Modifiers="Control" Command="{Binding IncreaseIndentCommand}" />
    <KeyBinding Key="OemOpenBrackets" Modifiers="Control" Command="{Binding DecreaseIndentCommand}" />
</Window.InputBindings>
```

**注意**: `Key.OemAtSign`, `Key.OemCloseBrackets`, `Key.OemOpenBrackets`はキーボードレイアウトによって異なる場合があります。日本語キーボード（JIS配列）を前提とします。

---

## 実装手順

### フェーズ1: TextFormatHelperクラスの作成

**ステップ1.1**: `src/Editor/TextFormatHelper.cs`を作成
- 名前空間: `MyMarkdownEditor.Editor`
- `WrapSelection`メソッドの実装
- 単体テスト用のロジック検証（手動テスト）

**ステップ1.2**: インデント操作メソッドの実装
- `IncreaseIndent`メソッド
- `DecreaseIndent`メソッド
- `IsListOrQuoteLine`メソッド

**ステップ1.3**: エッジケースのハンドリング
- 空の選択範囲
- ドキュメントの先頭/末尾での操作
- すでにインデントがない行での`DecreaseIndent`

### フェーズ2: MainWindow.xaml.csの拡張

**ステップ2.1**: コマンドプロパティの追加
- 6つの新規コマンドを定義

**ステップ2.2**: コマンド実装メソッドの追加
- `FormatBold`, `FormatItalic`, `FormatQuote`, `FormatInlineCode`
- `IncreaseIndent`, `DecreaseIndent`

**ステップ2.3**: `TextEditor_PreviewKeyDown`の拡張
- Tabキーの特殊処理を追加

**ステップ2.4**: コンストラクタでのコマンド初期化

### フェーズ3: MainWindow.xamlの拡張

**ステップ3.1**: InputBindingsに6つのキーバインディングを追加

### フェーズ4: テストとデバッグ

**ステップ4.1**: シンタックスハイライト補助機能のテスト
- Ctrl+B: 選択あり/なしの両方をテスト
- Ctrl+I: 選択あり/なしの両方をテスト
- Ctrl+2: 選択あり/なしの両方をテスト
- Ctrl+@: 選択あり/なしの両方をテスト

**ステップ4.2**: インデント操作機能のテスト
- Ctrl+]: 通常行、リスト行、引用行でテスト
- Ctrl+[: インデントがある行、ない行でテスト
- Tab: リスト行で動作、通常行で従来動作を維持

**ステップ4.3**: エッジケースのテスト
- 複数行選択時の動作
- ドキュメント先頭/末尾での動作
- 既にマークアップが存在する場合（例: `**太字**`を再度Ctrl+B）

---

## テスト計画

### 手動テスト項目

#### シンタックスハイライト補助機能

| テストケース | 操作 | 期待結果 |
|-------------|------|---------|
| 太字（選択あり） | "Hello"を選択してCtrl+B | `**Hello**` |
| 太字（選択なし） | Ctrl+B | `****` が挿入され、カーソルが中央に |
| 斜体（選択あり） | "World"を選択してCtrl+I | `*World*` |
| 斜体（選択なし） | Ctrl+I | `**` が挿入され、カーソルが中央に |
| ダブルクォート（選択あり） | "text"を選択してCtrl+2 | `"text"` |
| ダブルクォート（選択なし） | Ctrl+2 | `""` が挿入され、カーソルが中央に |
| インラインコード（選択あり） | "code"を選択してCtrl+@ | `` `code` `` |
| インラインコード（選択なし） | Ctrl+@ | `` `` `` が挿入され、カーソルが中央に |

#### インデント操作機能

| テストケース | 操作 | 期待結果 |
|-------------|------|---------|
| インデント増加（リスト） | `- Item`の行でCtrl+] | `  - Item` |
| インデント増加（通常行） | `Hello`の行でCtrl+] | `  Hello` |
| インデント減少（リスト） | `  - Item`の行でCtrl+[ | `- Item` |
| インデント減少（インデントなし） | `- Item`の行でCtrl+[ | `- Item`（変化なし） |
| Tab（リスト行） | `- Item`の行でTab | `  - Item` |
| Tab（通常行） | `Hello`の行でTab | タブ文字が挿入される（デフォルト動作） |

#### エッジケース

| テストケース | 操作 | 期待結果 |
|-------------|------|---------|
| 複数行選択 | 複数行を選択してCtrl+B | エラーなし（実装依存） |
| 既存マークアップ | `**太字**`を選択してCtrl+B | `****太字****`（二重適用） |
| ドキュメント先頭 | 先頭でCtrl+@ | 正常に挿入 |
| ドキュメント末尾 | 末尾でCtrl+@ | 正常に挿入 |

---

## 技術的な実装ポイント

### 1. AvalonEditのSelection API

```csharp
// 選択範囲の取得
int selectionStart = TextEditor.SelectionStart;
int selectionLength = TextEditor.SelectionLength;

// 選択されたテキストの取得
string selectedText = TextEditor.SelectedText;

// テキストの置換
TextEditor.Document.Replace(selectionStart, selectionLength, newText);

// 選択解除
TextEditor.Select(0, 0);

// キャレット位置の設定
TextEditor.CaretOffset = newPosition;
```

### 2. キーバインディングの注意点

**日本語キーボード特有のキー**:
- `@`キー: `Key.OemAtSign` (US配列では`Key.D2 + Shift`)
- `[`キー: `Key.OemOpenBrackets`
- `]`キー: `Key.OemCloseBrackets`
- `2`キー: `Key.D2`

**キーコードの検証**:
実装前に実機で`e.Key`の値をデバッグ出力して確認することを推奨。

### 3. Tabキーの特殊処理

Tabキーはデフォルトでフォーカス移動やインデント挿入を行うため、`e.Handled = true`で既定動作を抑制します。

```csharp
if (e.Key == Key.Tab)
{
    // 条件判定
    if (shouldHandleAsIndent)
    {
        e.Handled = true; // デフォルト動作を抑制
        // カスタム処理
    }
    // e.Handled = false の場合、デフォルト動作が実行される
}
```

### 4. インデント幅の設定

現在の実装では**2スペース固定**とします。将来的に設定ファイルで変更可能にすることも検討できます。

```csharp
private const string IndentString = "  "; // 2スペース
// または
private const string IndentString = "\t"; // タブ
```

---

## 既知の制限事項

### 1. 複数行選択のサポート

初期実装では、複数行にまたがる選択範囲に対しては、単純に選択範囲全体を記号で囲みます。
将来的には、各行ごとにマークアップを適用する機能も検討できます。

例:
```
現在: "Line1\nLine2" + Ctrl+B → **Line1\nLine2**
将来: "Line1\nLine2" + Ctrl+B → **Line1**\n**Line2**
```

### 2. 既存マークアップの検出

既に太字マークアップ（`**text**`）が存在する場合、Ctrl+Bを押すと二重に適用されます（`****text****`）。
トグル機能（マークアップの削除）は今回の実装には含めません。

### 3. Tabキーの挙動

通常のテキスト編集中にTabキーを押した場合、デフォルトでタブ文字が挿入されます。
リスト行でのみインデント操作として機能するため、ユーザーに若干の学習コストがかかります。

---

## パフォーマンス考慮

### 影響範囲

- **シンタックスハイライト補助**: 数文字～数百文字の挿入/置換のため、パフォーマンス影響は無視できる
- **インデント操作**: 単一行の先頭に文字を挿入/削除するだけなので、高速

### メモリ使用

- 新規クラス`TextFormatHelper`は静的メソッドのみで、インスタンスを保持しない
- 既存のAvalonEdit DocumentAPIを使用するため、追加のメモリ消費はほぼゼロ

---

## 将来の拡張案

以下は現時点の実装には含まれないが、将来的に検討可能な機能：

1. **マークアップのトグル機能**
   - 既に`**text**`が選択されている場合、Ctrl+Bで`text`に戻す

2. **複数行選択時の行ごと処理**
   - 複数行を選択してCtrl+Bを押した場合、各行ごとに太字マークアップを適用

3. **カスタマイズ可能なインデント幅**
   - 設定画面でタブ/スペース、スペース幅を選択可能に

4. **追加のショートカット**
   - Ctrl+K: リンク挿入 `[text](url)`
   - Ctrl+L: リスト項目挿入 `- `
   - Ctrl+Shift+C: コードブロック挿入 ` ``` `

5. **マクロ機能**
   - ユーザー定義のテキスト挿入スニペット

---

## 実装完了後のドキュメント更新

実装完了後、以下のドキュメントを更新します：

1. **development-log.md**
   - フェーズ3として本機能の実装ログを追加
   - 実装内容、コード行数、テスト結果を記録

2. **README.md**（存在する場合）
   - ショートカットキー一覧セクションに新機能を追加

3. **requirements.md**（必要に応じて）
   - 実装済み機能リストを更新

---

## 参考資料

### AvalonEdit ドキュメント
- [AvalonEdit GitHub](https://github.com/icsharpcode/AvalonEdit)
- [TextDocument API](https://github.com/icsharpcode/AvalonEdit/wiki/Document-API)

### WPF InputBindings
- [Microsoft Docs: InputBinding](https://docs.microsoft.com/ja-jp/dotnet/api/system.windows.input.inputbinding)

---

**作成日**: 2025-11-10
**対象バージョン**: 1.1
**推定実装時間**: 約3-4時間
**推定コード行数**: 約150-200行（TextFormatHelper: 80行、MainWindow拡張: 70-120行）
