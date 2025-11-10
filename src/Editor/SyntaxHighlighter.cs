using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.IO;
using System.Xml;

namespace MyMarkdownEditor.Editor;

/// <summary>
/// マークダウン用のシンタックスハイライト定義を提供するクラス
/// MVP版では見出し（#, ##, ###）のみをサポート
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

    <RuleSet>
        <!-- 見出し (# から ######) -->
        <Rule color=""Heading"">
            ^[ \t]*\#{1,6}[ \t]+.+$
        </Rule>
    </RuleSet>
</SyntaxDefinition>";

        using var reader = new StringReader(xshdContent);
        using var xmlReader = XmlReader.Create(reader);
        return HighlightingLoader.Load(xmlReader, HighlightingManager.Instance);
    }
}
