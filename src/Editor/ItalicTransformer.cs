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
/// IVisualLineTransformerを実装し、TextEffectCollectionにSkewTransformを含むTextEffectを追加することで、
/// フォントの機能に依存せず、全角・半角を問わず視覚的に傾斜させて表示します。
/// </remarks>
public class ItalicTransformer : IVisualLineTransformer
{
    // 斜体パターン: *text* (ただし **text** は除外)
    private static readonly Regex ItalicPattern = new Regex(
        @"(?<!\*)\*(?<content>[^\*\n]+?)\*(?!\*)",
        RegexOptions.Compiled
    );

    /// <summary>
    /// ビジュアルラインの要素にトランスフォームを適用する
    /// </summary>
    public void Transform(ITextRunConstructionContext context, IList<VisualLineElement> elements)
    {
        // 行のテキストを取得
        var line = context.VisualLine.FirstDocumentLine;
        var lineText = context.Document.GetText(line.Offset, line.Length);

        var matches = ItalicPattern.Matches(lineText);

        foreach (Match match in matches)
        {
            var contentGroup = match.Groups["content"];
            if (contentGroup.Success)
            {
                // マッチした内容部分のオフセット（前後の*は除く）
                int contentStart = line.Offset + contentGroup.Index;
                int contentEnd = contentStart + contentGroup.Length;

                // 該当範囲の要素にTextEffectを適用
                foreach (var element in elements)
                {
                    int elementStart = context.VisualLine.FirstDocumentLine.Offset + element.RelativeTextOffset;
                    int elementEnd = elementStart + element.DocumentLength;

                    // 要素が斜体範囲内にある場合
                    if (elementEnd > contentStart && elementStart < contentEnd)
                    {
                        // 新しいTextRunPropertiesを作成
                        var baseProperties = element.TextRunProperties;
                        var skewTransform = new SkewTransform(-15, 0);

                        // Typefaceも斜体に設定
                        var typeface = baseProperties.Typeface;
                        var italicTypeface = new Typeface(
                            typeface.FontFamily,
                            FontStyles.Italic,
                            typeface.Weight,
                            typeface.Stretch
                        );

                        // リフレクションを使ってTextRunPropertiesを設定
                        var newProperties = new ItalicTextRunProperties(
                            baseProperties,
                            italicTypeface,
                            skewTransform
                        );

                        // 要素のTextRunPropertiesをリフレクションで置き換え
                        var propertyInfo = element.GetType().GetProperty("TextRunProperties");
                        if (propertyInfo != null && propertyInfo.CanWrite)
                        {
                            propertyInfo.SetValue(element, newProperties);
                        }
                        else
                        {
                            // 読み取り専用の場合、バッキングフィールドを直接変更
                            var fieldInfo = element.GetType().GetField("<TextRunProperties>k__BackingField",
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (fieldInfo != null)
                            {
                                fieldInfo.SetValue(element, newProperties);
                            }
                        }
                    }
                }
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

        public override TextRunTypographyProperties? TypographyProperties
            => _base.TypographyProperties;
    }
}
