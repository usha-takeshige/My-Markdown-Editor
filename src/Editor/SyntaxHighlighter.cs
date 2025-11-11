using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System;
using System.IO;
using System.Text;
using System.Xml;
using MyMarkdownEditor.Settings;

namespace MyMarkdownEditor.Editor;

/// <summary>
/// マークダウン用のシンタックスハイライト定義を提供するクラス
/// バージョン1.2: キャッシング機能を追加してメモリ効率を改善
/// </summary>
public static class SyntaxHighlighter
{
    // ハイライト定義のキャッシュ
    private static IHighlightingDefinition? _cachedHighlighting;
    private static string? _cachedSettingsHash;
    private static readonly object _cacheLock = new object();

    /// <summary>
    /// マークダウン用のハイライト定義を取得する（キャッシュ対応）
    /// </summary>
    /// <param name="settings">ハイライト設定</param>
    /// <returns>IHighlightingDefinition</returns>
    public static IHighlightingDefinition GetMarkdownHighlighting(HighlightSettings settings)
    {
        lock (_cacheLock)
        {
            // 設定のハッシュを計算
            var settingsHash = settings.GetHashCode().ToString();

            // キャッシュが有効で設定が変わっていない場合は再利用
            if (_cachedHighlighting != null && _cachedSettingsHash == settingsHash)
            {
                return _cachedHighlighting;
            }

            try
            {
                var xshdContent = GenerateXshd(settings);
                using var reader = new StringReader(xshdContent);
                using var xmlReader = XmlReader.Create(reader);
                var highlighting = HighlightingLoader.Load(xmlReader, HighlightingManager.Instance);

                // キャッシュに保存
                _cachedHighlighting = highlighting;
                _cachedSettingsHash = settingsHash;

                return highlighting;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to generate XSHD: {ex.Message}");
                // エラー時はデフォルト設定で再試行
                try
                {
                    var defaultXshd = GenerateXshd(HighlightSettings.GetDefault());
                    using var reader = new StringReader(defaultXshd);
                    using var xmlReader = XmlReader.Create(reader);
                    var highlighting = HighlightingLoader.Load(xmlReader, HighlightingManager.Instance);

                    // デフォルト設定もキャッシュに保存
                    _cachedHighlighting = highlighting;
                    _cachedSettingsHash = HighlightSettings.GetDefault().GetHashCode().ToString();

                    return highlighting;
                }
                catch
                {
                    // 最終的にエラーが発生した場合はnullを返す（ハイライトなし）
                    return null!;
                }
            }
        }
    }

    /// <summary>
    /// キャッシュをクリアする（設定変更時などに使用）
    /// </summary>
    public static void ClearCache()
    {
        lock (_cacheLock)
        {
            _cachedHighlighting = null;
            _cachedSettingsHash = null;
        }
    }

    /// <summary>
    /// 設定値からXSHD形式のXMLを動的に生成する
    /// </summary>
    /// <param name="settings">ハイライト設定</param>
    /// <returns>XSHD形式のXML文字列</returns>
    private static string GenerateXshd(HighlightSettings settings)
    {
        var sb = new StringBuilder();

        // XMLヘッダー
        sb.AppendLine(@"<?xml version=""1.0""?>");
        sb.AppendLine(@"<SyntaxDefinition name=""Markdown"" xmlns=""http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008"">");

        // Color要素を動的生成
        sb.Append(GenerateColorElement("Heading", settings.Heading));
        sb.Append(GenerateColorElement("Bold", settings.Bold));
        sb.Append(GenerateColorElement("Italic", settings.Italic));
        sb.Append(GenerateColorElement("Code", settings.Code));
        sb.Append(GenerateColorElement("InlineCode", settings.InlineCode));

        // RuleSet（正規表現パターンは固定）
        sb.AppendLine("    <RuleSet>");
        sb.AppendLine(@"        <!-- 見出し (# から ######) -->");
        sb.AppendLine(@"        <Rule color=""Heading"">");
        sb.AppendLine(@"            ^[ \t]*\#{1,6}[ \t]+.+$");
        sb.AppendLine(@"        </Rule>");
        sb.AppendLine();
        sb.AppendLine(@"        <!-- 太字 (**text**) - 斜体より先にマッチング -->");
        sb.AppendLine(@"        <Rule color=""Bold"">");
        sb.AppendLine(@"            \*\*[^\*]+?\*\*");
        sb.AppendLine(@"        </Rule>");
        sb.AppendLine();
        sb.AppendLine(@"        <!-- 斜体 (*text*) - 太字と競合しないよう配慮 -->");
        sb.AppendLine(@"        <Rule color=""Italic"">");
        sb.AppendLine(@"            (?&lt;!\*)\*[^\*\n]+?\*(?!\*)");
        sb.AppendLine(@"        </Rule>");
        sb.AppendLine();
        sb.AppendLine(@"        <!-- インラインコード (`code`) -->");
        sb.AppendLine(@"        <Rule color=""InlineCode"">");
        sb.AppendLine(@"            `[^`\n]+?`");
        sb.AppendLine(@"        </Rule>");
        sb.AppendLine();
        sb.AppendLine(@"        <!-- コードブロック開始マーカー (```) -->");
        sb.AppendLine(@"        <Rule color=""Code"">");
        sb.AppendLine(@"            ^[ \t]*```.*$");
        sb.AppendLine(@"        </Rule>");
        sb.AppendLine("    </RuleSet>");
        sb.AppendLine("</SyntaxDefinition>");

        return sb.ToString();
    }

    /// <summary>
    /// Color要素のXML文字列を生成する
    /// </summary>
    /// <param name="name">色定義の名前</param>
    /// <param name="def">色定義</param>
    /// <returns>Color要素のXML文字列</returns>
    private static string GenerateColorElement(string name, ColorDefinition def)
    {
        var attributes = new List<string>();

        // 設定されている属性のみを追加
        if (!string.IsNullOrEmpty(def.Foreground))
            attributes.Add($@"foreground=""{def.Foreground}""");

        if (!string.IsNullOrEmpty(def.Background))
            attributes.Add($@"background=""{def.Background}""");

        if (!string.IsNullOrEmpty(def.FontWeight))
            attributes.Add($@"fontWeight=""{def.FontWeight}""");

        if (!string.IsNullOrEmpty(def.FontStyle))
            attributes.Add($@"fontStyle=""{def.FontStyle}""");

        if (!string.IsNullOrEmpty(def.FontFamily))
            attributes.Add($@"fontFamily=""{def.FontFamily}""");

        // 属性が1つもない場合でも空のColor要素を生成
        var attributesStr = attributes.Count > 0 ? " " + string.Join(" ", attributes) : "";
        return $@"    <Color name=""{name}""{attributesStr} />" + Environment.NewLine;
    }
}
