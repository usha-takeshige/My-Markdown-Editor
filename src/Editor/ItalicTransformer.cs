using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace MyMarkdownEditor.Editor;

/// <summary>
/// マークダウンの斜体構文に対してTextEffectとSkewTransformを適用するトランスフォーマー
/// 全角文字を含む斜体テキストを視覚的に傾けて表示する
/// </summary>
/// <remarks>
/// 【問題の背景】
/// WPFのFontStyle.Italicは、フォントファミリーにネイティブなイタリック体がある場合のみ機能します。
/// 日本語フォント（Meiryo UIなど）は全角文字のイタリック体をサポートしていないため、
/// FontStyle.Italicを指定しても全角文字は傾きません。
///
/// 【解決策】
/// TextEffectCollectionにSkewTransformを含むTextEffectを追加することで、
/// フォントの機能に依存せず、全角・半角を問わず視覚的に傾斜させて表示します。
/// </remarks>
public class ItalicTransformer : DocumentColorizingTransformer
{
    // 斜体パターン: *text* (ただし **text** は除外)
    private static readonly Regex ItalicPattern = new Regex(
        @"(?<!\*)\*(?<content>[^\*\n]+?)\*(?!\*)",
        RegexOptions.Compiled
    );

    /// <summary>
    /// ドキュメントの行に対して色付け処理を実行する
    /// </summary>
    protected override void ColorizeLine(DocumentLine line)
    {
        if (line == null)
            return;

        var lineText = CurrentContext.Document.GetText(line);
        var matches = ItalicPattern.Matches(lineText);

        foreach (Match match in matches)
        {
            // マッチした内容部分のみに適用（前後の*は除く）
            var contentGroup = match.Groups["content"];
            if (contentGroup.Success)
            {
                int startOffset = line.Offset + contentGroup.Index;
                int endOffset = startOffset + contentGroup.Length;

                ChangeLinePart(startOffset, endOffset, element =>
                {
                    // SkewTransformを含むTextEffectを適用
                    var skewTransform = new SkewTransform(-15, 0);

                    // Typefaceも斜体に設定（半角文字で効果がある）
                    var typeface = element.TextRunProperties.Typeface;
                    var italicTypeface = new Typeface(
                        typeface.FontFamily,
                        FontStyles.Italic,
                        typeface.Weight,
                        typeface.Stretch
                    );

                    element.TextRunProperties = new ItalicTextRunProperties(
                        element.TextRunProperties,
                        italicTypeface,
                        skewTransform
                    );
                });
            }
        }
    }

    /// <summary>
    /// Italic TypefaceとSkewTransformを適用するカスタムTextRunProperties
    /// </summary>
    private class ItalicTextRunProperties : TextRunProperties
    {
        private readonly TextRunProperties _base;
        private readonly Typeface _typeface;
        private readonly Transform _transform;

        public ItalicTextRunProperties(TextRunProperties baseProps, Typeface typeface, Transform transform)
        {
            _base = baseProps;
            _typeface = typeface;
            _transform = transform;
        }

        public override Typeface Typeface => _typeface;
        public override double FontRenderingEmSize => _base.FontRenderingEmSize;
        public override double FontHintingEmSize => _base.FontHintingEmSize;
        public override TextDecorationCollection? TextDecorations => _base.TextDecorations;
        public override Brush? ForegroundBrush => _base.ForegroundBrush;
        public override Brush? BackgroundBrush => _base.BackgroundBrush;
        public override System.Globalization.CultureInfo? CultureInfo => _base.CultureInfo;

        public override TextEffectCollection? TextEffects
        {
            get
            {
                // 既存のTextEffectをコピー
                var effects = new TextEffectCollection();
                if (_base.TextEffects != null)
                {
                    foreach (var effect in _base.TextEffects)
                        effects.Add(effect);
                }

                // SkewTransformを含むTextEffectを追加
                var textEffect = new TextEffect
                {
                    Transform = _transform,
                    PositionStart = 0,
                    PositionCount = int.MaxValue
                };
                effects.Add(textEffect);

                return effects;
            }
        }

        public override System.Windows.Media.TextFormatting.TextRunTypographyProperties? TypographyProperties
            => _base.TypographyProperties;
    }
}
