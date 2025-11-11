using System;
using System.Windows.Input;
using ICSharpCode.AvalonEdit.Document;
using MyMarkdownEditor.Editor;

namespace MyMarkdownEditor.Handlers;

/// <summary>
/// キーボード入力イベントを処理するハンドラー
/// Ctrl+ショートカット、Tabキー、Enterキーのカスタム動作を提供
/// </summary>
public class KeyboardInputHandler
{
    // ドキュメント操作用のデリゲート
    private readonly Func<int> _getCaretOffset;
    private readonly Action<int> _setCaretOffset;
    private readonly Func<TextDocument> _getDocument;

    // コマンドアクション
    private readonly Action _formatBold;
    private readonly Action _formatItalic;
    private readonly Action _formatQuote;
    private readonly Action _formatInlineCode;
    private readonly Action _increaseIndent;
    private readonly Action _decreaseIndent;

    /// <summary>
    /// KeyboardInputHandlerの新しいインスタンスを作成します
    /// </summary>
    /// <param name="getCaretOffset">キャレット位置を取得するデリゲート</param>
    /// <param name="setCaretOffset">キャレット位置を設定するデリゲート</param>
    /// <param name="getDocument">ドキュメントを取得するデリゲート</param>
    /// <param name="formatBold">太字コマンド</param>
    /// <param name="formatItalic">斜体コマンド</param>
    /// <param name="formatQuote">引用符コマンド</param>
    /// <param name="formatInlineCode">インラインコードコマンド</param>
    /// <param name="increaseIndent">インデント増加コマンド</param>
    /// <param name="decreaseIndent">インデント減少コマンド</param>
    public KeyboardInputHandler(
        Func<int> getCaretOffset,
        Action<int> setCaretOffset,
        Func<TextDocument> getDocument,
        Action formatBold,
        Action formatItalic,
        Action formatQuote,
        Action formatInlineCode,
        Action increaseIndent,
        Action decreaseIndent)
    {
        _getCaretOffset = getCaretOffset ?? throw new ArgumentNullException(nameof(getCaretOffset));
        _setCaretOffset = setCaretOffset ?? throw new ArgumentNullException(nameof(setCaretOffset));
        _getDocument = getDocument ?? throw new ArgumentNullException(nameof(getDocument));
        _formatBold = formatBold ?? throw new ArgumentNullException(nameof(formatBold));
        _formatItalic = formatItalic ?? throw new ArgumentNullException(nameof(formatItalic));
        _formatQuote = formatQuote ?? throw new ArgumentNullException(nameof(formatQuote));
        _formatInlineCode = formatInlineCode ?? throw new ArgumentNullException(nameof(formatInlineCode));
        _increaseIndent = increaseIndent ?? throw new ArgumentNullException(nameof(increaseIndent));
        _decreaseIndent = decreaseIndent ?? throw new ArgumentNullException(nameof(decreaseIndent));
    }

    /// <summary>
    /// PreviewKeyDownイベントを処理する
    /// </summary>
    /// <param name="e">キーボードイベント引数</param>
    /// <returns>イベントが処理された場合はtrue</returns>
    public bool HandlePreviewKeyDown(KeyEventArgs e)
    {
        // Ctrl修飾子付きのカスタムショートカットを優先処理
        if (HandleCtrlShortcuts(e))
        {
            return true;
        }

        // Tabキーの処理（リスト行でのみインデント操作として機能）
        if (HandleTabKey(e))
        {
            return true;
        }

        // Enterキーの処理（マークダウン自動継続）
        if (HandleEnterKey(e))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Ctrl+ショートカットを処理する
    /// </summary>
    /// <param name="e">キーボードイベント引数</param>
    /// <returns>イベントが処理された場合はtrue</returns>
    private bool HandleCtrlShortcuts(KeyEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.Control)
        {
            return false;
        }

        switch (e.Key)
        {
            case Key.B:
                e.Handled = true;
                _formatBold();
                return true;

            case Key.I:
                e.Handled = true;
                _formatItalic();
                return true;

            case Key.D2:
                e.Handled = true;
                _formatQuote();
                return true;

            case Key.OemTilde: // Oem3と同じ値
                e.Handled = true;
                _formatInlineCode();
                return true;

            case Key.Oem6:
                e.Handled = true;
                _increaseIndent();
                return true;

            case Key.OemOpenBrackets: // Oem4と同じ値
                e.Handled = true;
                _decreaseIndent();
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Tabキーを処理する
    /// リスト行または引用行の場合はインデント操作として機能
    /// </summary>
    /// <param name="e">キーボードイベント引数</param>
    /// <returns>イベントが処理された場合はtrue</returns>
    private bool HandleTabKey(KeyEventArgs e)
    {
        if (e.Key != Key.Tab)
        {
            return false;
        }

        // 現在行を取得
        int caretOffset = _getCaretOffset();
        var document = _getDocument();
        var line = document.GetLineByOffset(caretOffset);
        string lineText = document.GetText(line.Offset, line.Length);

        // リストまたは引用の場合はインデント操作
        if (TextFormatHelper.IsListOrQuoteLine(lineText))
        {
            e.Handled = true; // デフォルト動作を抑制
            _increaseIndent();
            return true;
        }

        // 通常行の場合はデフォルト動作（タブ文字挿入）
        return false;
    }

    /// <summary>
    /// Enterキーを処理する
    /// マークダウンパターン（リスト、引用）の自動継続を実装
    /// </summary>
    /// <param name="e">キーボードイベント引数</param>
    /// <returns>イベントが処理された場合はtrue</returns>
    private bool HandleEnterKey(KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return false;
        }

        // 現在のキャレット位置を取得
        int caretOffset = _getCaretOffset();
        var document = _getDocument();

        // 現在行を取得
        var line = document.GetLineByOffset(caretOffset);
        string lineText = document.GetText(line.Offset, line.Length);

        // 次行のプレフィックスを取得
        string nextPrefix = MarkdownAssistant.GetNextLinePrefix(lineText, out bool shouldContinue);

        if (shouldContinue && !string.IsNullOrEmpty(nextPrefix))
        {
            // マークダウンパターンを継続
            e.Handled = true;

            // 改行を挿入
            document.Insert(caretOffset, Environment.NewLine);

            // 新しい行の先頭にプレフィックスを挿入
            int newCaretOffset = caretOffset + Environment.NewLine.Length;
            document.Insert(newCaretOffset, nextPrefix);

            // カーソルをプレフィックスの後ろに移動
            _setCaretOffset(newCaretOffset + nextPrefix.Length);
            return true;
        }
        else if (MarkdownAssistant.IsMarkdownPattern(lineText) && !shouldContinue)
        {
            // コンテンツが空のリスト行でEnterを押した場合、マーカーを削除
            e.Handled = true;

            // 現在行のマーカー部分を削除
            document.Remove(line.Offset, line.Length);

            // 改行を挿入
            document.Insert(line.Offset, Environment.NewLine);
            _setCaretOffset(line.Offset + Environment.NewLine.Length);
            return true;
        }

        return false;
    }
}
