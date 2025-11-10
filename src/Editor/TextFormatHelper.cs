using ICSharpCode.AvalonEdit.Document;

namespace MyMarkdownEditor.Editor;

/// <summary>
/// テキスト整形機能を提供するクラス
/// 選択範囲の囲み処理とインデント操作をサポート
/// </summary>
public static class TextFormatHelper
{
    private const string IndentString = "  "; // 2スペース固定

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
        TextDocument document,
        int selectionStart,
        int selectionLength,
        string prefix,
        string? suffix = null)
    {
        suffix ??= prefix; // suffixがnullの場合はprefixを使用

        if (selectionLength > 0)
        {
            // 選択範囲がある場合: 選択範囲を記号で囲む
            string selectedText = document.GetText(selectionStart, selectionLength);
            string wrappedText = $"{prefix}{selectedText}{suffix}";

            document.Replace(selectionStart, selectionLength, wrappedText);

            // キャレットを囲んだテキストの後ろに配置
            return selectionStart + wrappedText.Length;
        }
        else
        {
            // 選択範囲がない場合: 記号ペアを挿入し、カーソルを中央に配置
            string pair = $"{prefix}{suffix}";
            document.Insert(selectionStart, pair);

            // キャレットを記号の中央に配置
            return selectionStart + prefix.Length;
        }
    }

    /// <summary>
    /// 現在行のインデントを増やす
    /// </summary>
    /// <param name="document">ドキュメント</param>
    /// <param name="caretOffset">現在のキャレット位置</param>
    /// <param name="useSpaces">スペースを使用するか（true: 2スペース、false: タブ）</param>
    /// <returns>新しいキャレット位置</returns>
    public static int IncreaseIndent(
        TextDocument document,
        int caretOffset,
        bool useSpaces = true)
    {
        // 現在行を取得
        var line = document.GetLineByOffset(caretOffset);

        // インデント文字列を決定
        string indent = useSpaces ? IndentString : "\t";

        // 行の先頭にインデントを挿入
        document.Insert(line.Offset, indent);

        // キャレット位置を調整（インデント分だけ右に移動）
        return caretOffset + indent.Length;
    }

    /// <summary>
    /// 現在行のインデントを減らす
    /// </summary>
    /// <param name="document">ドキュメント</param>
    /// <param name="caretOffset">現在のキャレット位置</param>
    /// <param name="useSpaces">スペースを使用するか（true: 2スペース、false: タブ）</param>
    /// <returns>新しいキャレット位置</returns>
    public static int DecreaseIndent(
        TextDocument document,
        int caretOffset,
        bool useSpaces = true)
    {
        // 現在行を取得
        var line = document.GetLineByOffset(caretOffset);
        string lineText = document.GetText(line.Offset, line.Length);

        // インデント文字列を決定
        string indent = useSpaces ? IndentString : "\t";

        // 行の先頭がインデント文字列で始まる場合は削除
        if (lineText.StartsWith(indent))
        {
            document.Remove(line.Offset, indent.Length);

            // キャレット位置を調整（インデント分だけ左に移動）
            int newOffset = caretOffset - indent.Length;
            return newOffset < line.Offset ? line.Offset : newOffset;
        }
        else if (lineText.StartsWith("\t"))
        {
            // タブ文字の場合も削除
            document.Remove(line.Offset, 1);

            int newOffset = caretOffset - 1;
            return newOffset < line.Offset ? line.Offset : newOffset;
        }

        // インデントがない場合は変更なし
        return caretOffset;
    }

    /// <summary>
    /// 現在行がリストまたは引用であるかを判定
    /// </summary>
    /// <param name="lineText">行テキスト</param>
    /// <returns>リストまたは引用の場合true</returns>
    public static bool IsListOrQuoteLine(string lineText)
    {
        return MarkdownAssistant.IsMarkdownPattern(lineText);
    }
}
