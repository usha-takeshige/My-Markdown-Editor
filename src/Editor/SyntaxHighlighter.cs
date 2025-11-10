using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.IO;
using System.Xml;

namespace MyMarkdownEditor.Editor;

/// <summary>
/// マークダウン用のシンタックスハイライト定義を提供するクラス
/// バージョン1.0: 見出し、太字、斜体、コードブロック、インラインコードをサポート
/// </summary>
public static class SyntaxHighlighter
{
    /// <summary>
    /// マークダウン用のハイライト定義を取得する
    /// </summary>
    /// <returns>IHighlightingDefinition</returns>
    public static IHighlightingDefinition GetMarkdownHighlighting()
    {
        var xshdContent = @"<?xml version=""1.0""?>
<SyntaxDefinition name=""Markdown"" xmlns=""http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008"">
    <Color name=""Heading"" foreground=""#0066CC"" fontWeight=""bold"" />
    <Color name=""Bold"" fontWeight=""bold"" />
    <Color name=""Italic"" fontStyle=""italic"" />
    <Color name=""Code"" foreground=""#D14"" background=""#F5F5F5"" fontFamily=""Consolas"" />
    <Color name=""InlineCode"" foreground=""#D14"" background=""#F5F5F5"" fontFamily=""Consolas"" />

    <RuleSet>
        <!-- 見出し (# から ######) -->
        <Rule color=""Heading"">
            ^[ \t]*\#{1,6}[ \t]+.+$
        </Rule>

        <!-- 太字 (**text**) - 斜体より先にマッチング -->
        <Rule color=""Bold"">
            \*\*[^\*]+?\*\*
        </Rule>

        <!-- 斜体 (*text*) - 太字と競合しないよう配慮 -->
        <Rule color=""Italic"">
            (?&lt;!\*)\*[^\*\n]+?\*(?!\*)
        </Rule>

        <!-- インラインコード (`code`) -->
        <Rule color=""InlineCode"">
            `[^`\n]+?`
        </Rule>

        <!-- コードブロック開始マーカー (```) -->
        <Rule color=""Code"">
            ^[ \t]*```.*$
        </Rule>
    </RuleSet>
</SyntaxDefinition>";

        using var reader = new StringReader(xshdContent);
        using var xmlReader = XmlReader.Create(reader);
        return HighlightingLoader.Load(xmlReader, HighlightingManager.Instance);
    }
}
