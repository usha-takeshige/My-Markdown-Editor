using System.Text.RegularExpressions;

namespace MyMarkdownEditor.Editor;

/// <summary>
/// マークダウン補助機能を提供するクラス
/// リスト自動継続、番号付きリスト自動インクリメント、引用自動継続などを実装
/// </summary>
public static class MarkdownAssistant
{
    private static readonly Regex UnorderedListPattern = new(@"^(\s*)[-*+]\s+(.*)$");
    private static readonly Regex OrderedListPattern = new(@"^(\s*)(\d+)\.\s+(.*)$");
    private static readonly Regex QuotePattern = new(@"^(\s*)>\s+(.*)$");

    /// <summary>
    /// 現在行のテキストから次行のプレフィックスを取得する
    /// </summary>
    /// <param name="lineText">現在行のテキスト</param>
    /// <param name="shouldContinue">継続すべきかどうか</param>
    /// <returns>次行のプレフィックス（継続しない場合は空文字）</returns>
    public static string GetNextLinePrefix(string lineText, out bool shouldContinue)
    {
        shouldContinue = false;

        if (string.IsNullOrEmpty(lineText))
        {
            return string.Empty;
        }

        // 順序なしリストのチェック (-, *, +)
        var unorderedMatch = UnorderedListPattern.Match(lineText);
        if (unorderedMatch.Success)
        {
            var indent = unorderedMatch.Groups[1].Value;
            var content = unorderedMatch.Groups[2].Value;

            // コンテンツが空の場合は継続しない
            if (string.IsNullOrWhiteSpace(content))
            {
                shouldContinue = false;
                return string.Empty;
            }

            shouldContinue = true;
            return $"{indent}- ";
        }

        // 順序付きリストのチェック (1., 2., など)
        var orderedMatch = OrderedListPattern.Match(lineText);
        if (orderedMatch.Success)
        {
            var indent = orderedMatch.Groups[1].Value;
            var number = int.Parse(orderedMatch.Groups[2].Value);
            var content = orderedMatch.Groups[3].Value;

            // コンテンツが空の場合は継続しない
            if (string.IsNullOrWhiteSpace(content))
            {
                shouldContinue = false;
                return string.Empty;
            }

            shouldContinue = true;
            return $"{indent}{number + 1}. ";
        }

        // 引用のチェック (>)
        var quoteMatch = QuotePattern.Match(lineText);
        if (quoteMatch.Success)
        {
            var indent = quoteMatch.Groups[1].Value;
            var content = quoteMatch.Groups[2].Value;

            // コンテンツが空の場合は継続しない
            if (string.IsNullOrWhiteSpace(content))
            {
                shouldContinue = false;
                return string.Empty;
            }

            shouldContinue = true;
            return $"{indent}> ";
        }

        return string.Empty;
    }

    /// <summary>
    /// 指定された行がリストまたは引用のパターンに一致するかどうかを判定
    /// </summary>
    /// <param name="lineText">チェックする行のテキスト</param>
    /// <returns>パターンに一致する場合はtrue</returns>
    public static bool IsMarkdownPattern(string lineText)
    {
        if (string.IsNullOrEmpty(lineText))
        {
            return false;
        }

        return UnorderedListPattern.IsMatch(lineText) ||
               OrderedListPattern.IsMatch(lineText) ||
               QuotePattern.IsMatch(lineText);
    }
}
