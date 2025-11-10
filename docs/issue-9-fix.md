# Issue #9: 全角文字の斜体表示バグ修正

## 問題の概要

マークダウン記法の斜体構文（`*テキスト*`）を使用した際、半角文字は正常に斜体表示されるが、全角文字（日本語など）は斜体表示されない問題が発生していました。

### 再現手順

1. エディタで `*test*` と入力 → 正常に斜体表示される
2. エディタで `*テスト*` と入力 → 斜体表示されない（通常のフォントスタイルのまま）

## バグの原因

### 技術的な背景

WPFの`FontStyle.Italic`プロパティは、指定されたフォントファミリーに**ネイティブなイタリック体**が存在する場合のみ機能します。これは、フォントファミリーが持つ実際のイタリックフォントファイル（例: Arial Italic, Times New Roman Italic）を使用するためです。

### 日本語フォントの特性

日本語フォント（Meiryo UI、MS Gothic、MS Minchoなど）は、以下の理由からイタリック体をネイティブサポートしていません：

1. **文化的背景**: 日本語の組版において、斜体は伝統的に使用されない
2. **技術的理由**: 全角文字（漢字、ひらがな、カタカナ）は複雑な字形を持つため、物理的に傾けると可読性が著しく低下する
3. **フォント設計**: 多くの日本語フォントはRegularとBoldウェイトのみを提供し、Italic/Obliqueバリアントは含まれていない

### 具体的な問題点

本アプリケーションでは、エディタのフォントとして`Meiryo UI`を使用しています（MainWindow.xaml:41）：

```xaml
<avalonEdit:TextEditor
    FontFamily="Meiryo UI"
    ...
/>
```

また、シンタックスハイライトの設定（HighlightSettings.cs:124-127）では、斜体に対して以下の設定を行っていました：

```csharp
Italic = new ColorDefinition
{
    FontStyle = "italic"
}
```

この設定は、AvalonEditのXSHD（XML Syntax Highlighting Definition）において以下のように展開されます：

```xml
<Color name="Italic" fontStyle="italic" />
```

しかし、Meiryo UIフォントには`Meiryo UI Italic`という実際のフォントファイルが存在しないため、`fontStyle="italic"`を指定しても全角文字には何の効果もありません。

半角のアルファベット文字が斜体になる理由は、Windowsのフォントフォールバック機能により、英字の表示時に自動的にイタリック体を持つ欧文フォント（例: Segoe UI Italic）が代替使用されるためです。

## 修正内容

### 解決アプローチ

ネイティブなイタリックフォントに依存せず、**人工的な傾斜（Oblique/Skew）**を適用することで、全角文字を含むすべてのテキストを視覚的に斜体風に表示する方法を採用しました。

### 実装の詳細

#### 1. ItalicTransformer クラスの作成

新しいファイル `src/Editor/ItalicTransformer.cs` を作成し、`DocumentColorizingTransformer`を継承したカスタムトランスフォーマーを実装しました。

**主な機能:**

- **パターンマッチング**: 正規表現を使用して斜体パターン `*text*` を検出（`**text**`は除外）
- **SkewTransform の適用**: `TextEffect`を通じて、マッチしたテキストに対して`SkewTransform(-15, 0)`を適用
- **Typeface の設定**: 半角文字用に`FontStyles.Italic`も併用し、ネイティブイタリックがある文字では自然な表示を維持

**SkewTransform の仕組み:**

```csharp
var skewTransform = new SkewTransform(-15, 0);
```

- `-15`度のX軸方向の傾斜を適用（負の値で右方向に傾く）
- これにより、フォントの機能に関係なく、視覚的に斜体風の表示を実現

#### 2. MainWindow.xaml.cs への統合

MainWindowのコンストラクタで、TextEditorの`LineTransformers`コレクションに`ItalicTransformer`を追加：

```csharp
// 斜体トランスフォーマーの追加（全角文字の斜体表示対応）
TextEditor.TextArea.TextView.LineTransformers.Add(new ItalicTransformer());
```

`LineTransformers`は、AvalonEditがテキストをレンダリングする際に各行に対して順次適用されるトランスフォーマーのコレクションです。

### コードの詳細説明

#### ItalicTextRunProperties クラス

`TextRunProperties`を継承したカスタムクラスで、以下の機能を提供：

1. **Typeface のオーバーライド**: 半角文字用に`FontStyles.Italic`を設定
2. **TextEffects のオーバーライド**: `SkewTransform`を含む`TextEffect`を動的に生成し、既存のエフェクトに追加

```csharp
public override TextEffectCollection? TextEffects
{
    get
    {
        var effects = new TextEffectCollection();

        // 既存のエフェクトをコピー
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
```

## 変更されたファイル

1. **src/Editor/ItalicTransformer.cs** (新規作成)
   - `ItalicTransformer`クラスの実装
   - `ItalicTextRunProperties`内部クラスの実装

2. **src/MainWindow.xaml.cs** (変更)
   - Line 60: `ItalicTransformer`のインスタンスを`LineTransformers`に追加

3. **docs/issue-9-fix.md** (このファイル、新規作成)
   - バグの原因と修正内容のドキュメント

## テスト結果

修正後、以下のテストケースで正常に動作することを確認：

### テストケース1: 全角文字の斜体
```markdown
*これは日本語のテキストです*
```
**期待結果**: 全角文字が視覚的に右に傾いて表示される
**実際の結果**: ✓ 正常に傾斜表示

### テストケース2: 半角文字の斜体
```markdown
*This is English text*
```
**期待結果**: 半角文字が斜体で表示される
**実際の結果**: ✓ 正常にイタリック表示（ネイティブイタリック + Skew）

### テストケース3: 混在テキスト
```markdown
*日本語とEnglishの混在*
```
**期待結果**: 両方が統一的に傾斜表示される
**実際の結果**: ✓ 正常に両方とも傾斜表示

### テストケース4: 太字との区別
```markdown
**太字** と *斜体* の違い
```
**期待結果**: 太字は傾かず、斜体のみが傾く
**実際の結果**: ✓ 正常に区別されて表示

## 技術的な注意点

### SkewTransform の角度選定

`SkewTransform(-15, 0)`の`-15`度という値は、以下の理由で選定されました：

1. **可読性**: 傾斜角度が大きすぎると可読性が低下（-20度以上は推奨されない）
2. **視覚的な効果**: 斜体であることが明確に認識できる最小角度
3. **業界標準**: 多くのフォントのイタリック体は約12〜15度の傾斜を持つ

### パフォーマンスへの影響

- `DocumentColorizingTransformer`は効率的な差分更新を行うため、パフォーマンスへの影響は最小限
- 正規表現は`RegexOptions.Compiled`でコンパイル済み、高速なマッチングを実現
- `TextEffect`はWPFのネイティブ機能を使用するため、描画オーバーヘッドは無視できるレベル

### 代替アプローチとの比較

他に検討されたアプローチ：

1. **フォントの変更**:
   - ❌ イタリックをサポートする日本語フォントは限られており、アプリ全体のデザインに影響

2. **カスタムレンダリング**:
   - ❌ GlyphRunを使った低レベルレンダリングは複雑で保守性が低い

3. **RenderTransform の使用**:
   - ❌ AvalonEditのTextエリア全体に影響し、特定のテキスト範囲のみに適用できない

4. **TextEffect + SkewTransform** (採用):
   - ✅ シンプルで保守性が高い
   - ✅ 特定のテキスト範囲のみに適用可能
   - ✅ WPFのネイティブ機能を活用
   - ✅ パフォーマンスへの影響が最小限

## まとめ

本修正により、マークダウンの斜体構文が全角文字に対しても正常に機能するようになりました。`SkewTransform`を使用した人工的な傾斜は、ネイティブイタリックフォントに依存しない堅牢な解決策であり、将来的に他のフォントを使用する場合でも問題なく動作します。

---

**修正実施日**: 2025-11-10
**関連Issue**: #9
**担当者**: Claude Code Agent
