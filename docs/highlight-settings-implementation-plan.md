# シンタックスハイライト設定ファイル化 - 実装計画

**作成日**: 2025-11-10
**対象バージョン**: 1.1
**変更種別**: 機能拡張（設定のカスタマイズ化）

---

## 📋 概要

現在ハードコードされているシンタックスハイライトの色・スタイル設定を、JSON設定ファイル経由でカスタマイズ可能にする。これにより、ユーザーは再コンパイル不要で視覚的なテーマを変更できるようになる。

### 変更の目的
- **カスタマイズ性の向上**: ユーザーが好みの色やフォントを設定可能に
- **メンテナンス性の向上**: 設定とコードの分離により、将来的なテーマ追加が容易に
- **一貫性の確保**: `WindowSettings`と同様のJSON設定方式を採用

### 変更しない部分
- シンタックスハイライトの**正規表現パターン**は固定のまま
- マッチングルールの**優先順位**は固定のまま
- ハイライト対象の**要素種類**（見出し、太字、斜体など）は変更なし

---

## 🎯 実装目標

### Phase 1: 基本実装
1. 設定データモデルクラスの作成
2. 動的XSHD生成ロジックの実装
3. 既存コードの修正（呼び出し側）

### Phase 2: 品質向上
4. デフォルト設定ファイルの自動生成
5. エラーハンドリングの強化
6. 設定値のバリデーション

### Phase 3: ドキュメント整備
7. 設定ファイルのカスタマイズガイド作成
8. サンプル設定ファイルの提供

---

## 📁 ファイル変更スコープ

### 新規作成ファイル

#### 1. `src/Settings/HighlightSettings.cs`

**目的**: シンタックスハイライト設定のデータモデルクラス

**主要クラス・構造**:

```csharp
namespace MyMarkdownEditor.Settings;

/// <summary>
/// シンタックスハイライトの色・スタイル設定を管理するクラス
/// </summary>
public class HighlightSettings
{
    // 設定保存先
    private static readonly string SettingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MyMarkdownEditor",
        "highlight-settings.json"
    );

    // 各要素の色・スタイル定義
    public ColorDefinition Heading { get; set; } = new() { /* デフォルト値 */ };
    public ColorDefinition Bold { get; set; } = new() { /* デフォルト値 */ };
    public ColorDefinition Italic { get; set; } = new() { /* デフォルト値 */ };
    public ColorDefinition Code { get; set; } = new() { /* デフォルト値 */ };
    public ColorDefinition InlineCode { get; set; } = new() { /* デフォルト値 */ };

    // 設定の読み込み
    public static HighlightSettings Load();

    // 設定の保存
    public void Save();

    // デフォルト設定を返す
    public static HighlightSettings GetDefault();
}

/// <summary>
/// 個々の要素の色・スタイル定義
/// </summary>
public class ColorDefinition
{
    public string? Foreground { get; set; }
    public string? Background { get; set; }
    public string? FontWeight { get; set; }  // "normal" | "bold"
    public string? FontStyle { get; set; }   // "normal" | "italic"
    public string? FontFamily { get; set; }
}
```

**デフォルト値**:
```csharp
public static HighlightSettings GetDefault()
{
    return new HighlightSettings
    {
        Heading = new ColorDefinition
        {
            Foreground = "#0066CC",
            FontWeight = "bold"
        },
        Bold = new ColorDefinition
        {
            FontWeight = "bold"
        },
        Italic = new ColorDefinition
        {
            FontStyle = "italic"
        },
        Code = new ColorDefinition
        {
            Foreground = "#D14",
            Background = "#F5F5F5",
            FontFamily = "Consolas"
        },
        InlineCode = new ColorDefinition
        {
            Foreground = "#D14",
            Background = "#F5F5F5",
            FontFamily = "Consolas"
        }
    };
}
```

**実装詳細**:

1. **Load()メソッド**:
   - 設定ファイルが存在する場合: JSON deserializeして返す
   - 存在しない場合: デフォルト設定を返す
   - 読み込みエラー時: デフォルト設定を返す（エラーログは出力）
   - 初回起動時: デフォルト設定ファイルを自動生成

2. **Save()メソッド**:
   - JSON serializeして設定ファイルに保存
   - ディレクトリが存在しない場合は作成
   - エラー時は無視（アプリ動作に影響させない）

3. **バリデーション**:
   - 色コードは `#RRGGBB` または `#RGB` 形式をチェック
   - 無効な値の場合はデフォルト値にフォールバック

**推定行数**: 約150行（コメント含む）

---

#### 2. `highlight-settings.json` (実行時生成)

**保存場所**: `%AppData%\MyMarkdownEditor\highlight-settings.json`

**フォーマット例**:

```json
{
  "Heading": {
    "Foreground": "#0066CC",
    "FontWeight": "bold"
  },
  "Bold": {
    "FontWeight": "bold"
  },
  "Italic": {
    "FontStyle": "italic"
  },
  "Code": {
    "Foreground": "#D14",
    "Background": "#F5F5F5",
    "FontFamily": "Consolas"
  },
  "InlineCode": {
    "Foreground": "#D14",
    "Background": "#F5F5F5",
    "FontFamily": "Consolas"
  }
}
```

**生成タイミング**:
- 初回起動時に自動生成
- 設定ファイルが存在しない場合
- `HighlightSettings.Load()` 内で処理

---

### 修正対象ファイル

#### 3. `src/Editor/SyntaxHighlighter.cs` (大幅修正)

**現在の実装**:
```csharp
public static IHighlightingDefinition GetMarkdownHighlighting()
{
    var xshdContent = @"<?xml version=""1.0""?>
    <SyntaxDefinition name=""Markdown"" ...>
        <Color name=""Heading"" foreground=""#0066CC"" fontWeight=""bold"" />
        ...
    </SyntaxDefinition>";

    // XSHDを読み込んでハイライト定義を返す
}
```

**新しい実装**:
```csharp
public static IHighlightingDefinition GetMarkdownHighlighting(HighlightSettings settings)
{
    // 設定値からXSHDを動的に生成
    var xshdContent = GenerateXshd(settings);

    using var reader = new StringReader(xshdContent);
    using var xmlReader = XmlReader.Create(reader);
    return HighlightingLoader.Load(xmlReader, HighlightingManager.Instance);
}

private static string GenerateXshd(HighlightSettings settings)
{
    var sb = new StringBuilder();
    sb.AppendLine(@"<?xml version=""1.0""?>");
    sb.AppendLine(@"<SyntaxDefinition name=""Markdown"" xmlns=""http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008"">");

    // Heading
    sb.Append(GenerateColorElement("Heading", settings.Heading));

    // Bold
    sb.Append(GenerateColorElement("Bold", settings.Bold));

    // Italic
    sb.Append(GenerateColorElement("Italic", settings.Italic));

    // Code
    sb.Append(GenerateColorElement("Code", settings.Code));

    // InlineCode
    sb.Append(GenerateColorElement("InlineCode", settings.InlineCode));

    sb.AppendLine("    <RuleSet>");
    sb.AppendLine(@"        <Rule color=""Heading"">^[ \t]*\#{1,6}[ \t]+.+$</Rule>");
    sb.AppendLine(@"        <Rule color=""Bold"">\*\*[^\*]+?\*\*</Rule>");
    sb.AppendLine(@"        <Rule color=""Italic"">(?&lt;!\*)\*[^\*\n]+?\*(?!\*)</Rule>");
    sb.AppendLine(@"        <Rule color=""InlineCode"">`[^`\n]+?`</Rule>");
    sb.AppendLine(@"        <Rule color=""Code"">^[ \t]*```.*$</Rule>");
    sb.AppendLine("    </RuleSet>");
    sb.AppendLine("</SyntaxDefinition>");

    return sb.ToString();
}

private static string GenerateColorElement(string name, ColorDefinition def)
{
    var attributes = new List<string>();

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

    return $@"    <Color name=""{name}"" {string.Join(" ", attributes)} />" + Environment.NewLine;
}
```

**変更サマリー**:
- メソッドシグネチャ: `GetMarkdownHighlighting()` → `GetMarkdownHighlighting(HighlightSettings settings)`
- ハードコードXSHD → 動的生成に変更
- 新規ヘルパーメソッド追加: `GenerateXshd()`, `GenerateColorElement()`
- 正規表現パターンは固定のまま維持

**推定変更行数**: 約100行追加（元の60行 → 約160行）

---

#### 4. `src/MainWindow.xaml.cs` (軽微な修正)

**変更箇所**: 56行目付近

**現在の実装**:
```csharp
public MainWindow()
{
    InitializeComponent();

    // ... (コマンドの初期化など)

    // シンタックスハイライトの設定
    TextEditor.SyntaxHighlighting = SyntaxHighlighter.GetMarkdownHighlighting();

    // ... (ウィンドウ設定の復元など)
}
```

**新しい実装**:
```csharp
public MainWindow()
{
    InitializeComponent();

    // ... (コマンドの初期化など)

    // シンタックスハイライトの設定
    var highlightSettings = HighlightSettings.Load();
    TextEditor.SyntaxHighlighting = SyntaxHighlighter.GetMarkdownHighlighting(highlightSettings);

    // ... (ウィンドウ設定の復元など)
}
```

**変更サマリー**:
- 1行追加: `var highlightSettings = HighlightSettings.Load();`
- 1行修正: 引数に `highlightSettings` を追加

**推定変更行数**: 2行

---

#### 5. `src/MyMarkdownEditor.csproj` (確認のみ、変更不要の可能性大)

**確認事項**:
- `System.Text.Json` のパッケージ参照
- 既に `WindowSettings.cs` で使用しているため、追加の参照は不要

**現在の状態**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net9.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="AvalonEdit" Version="6.3.1.120" />
  </ItemGroup>
</Project>
```

**結論**: 変更不要（`System.Text.Json`は.NET標準ライブラリに含まれる）

---

## 🔧 実装詳細

### 1. HighlightSettings.cs の詳細設計

#### クラス構造

```
HighlightSettings
├── Heading: ColorDefinition
├── Bold: ColorDefinition
├── Italic: ColorDefinition
├── Code: ColorDefinition
├── InlineCode: ColorDefinition
├── Load(): HighlightSettings (static)
├── Save(): void
└── GetDefault(): HighlightSettings (static)

ColorDefinition
├── Foreground: string?
├── Background: string?
├── FontWeight: string?
├── FontStyle: string?
└── FontFamily: string?
```

#### Load() メソッドの処理フロー

```
Load()
├─→ 設定ファイル存在チェック
│   ├─ YES → JSON deserialize
│   │        ├─ 成功 → 設定オブジェクトを返す
│   │        └─ 失敗 → デフォルト設定を返す & エラーログ
│   └─ NO  → デフォルト設定を生成
│             └─ Save()を呼んで自動生成
└─→ return settings
```

#### Save() メソッドの処理フロー

```
Save()
├─→ ディレクトリ存在チェック
│   └─ NO → Directory.CreateDirectory()
├─→ JSON serialize (WriteIndented: true)
└─→ File.WriteAllText()
    └─ エラー時は無視（アプリ動作を継続）
```

#### バリデーション仕様

| プロパティ | 検証内容 | 無効時の処理 |
|----------|---------|------------|
| Foreground/Background | `#[0-9A-Fa-f]{3}` または `#[0-9A-Fa-f]{6}` | デフォルト値に置換 |
| FontWeight | "normal" または "bold" | デフォルト値に置換 |
| FontStyle | "normal" または "italic" | デフォルト値に置換 |
| FontFamily | 任意の文字列 | そのまま使用（無効なフォント名でも実行時エラーにはならない） |

---

### 2. SyntaxHighlighter.cs の動的生成ロジック

#### XSHD生成の流れ

1. **ヘッダー部分を生成**
   ```xml
   <?xml version="1.0"?>
   <SyntaxDefinition name="Markdown" xmlns="http://...">
   ```

2. **Color要素を動的生成**
   - 各`ColorDefinition`を走査
   - 設定値が存在する属性のみをXML属性として出力
   - 例: `Foreground`が設定されていない場合は`foreground="..."`を出力しない

3. **RuleSet部分は固定**
   - 正規表現パターンは変更しない
   - マッチングルールの順序も固定

4. **フッター部分を生成**
   ```xml
   </SyntaxDefinition>
   ```

#### GenerateColorElement() の詳細

**入力例**:
```csharp
name = "Heading"
def = new ColorDefinition {
    Foreground = "#0066CC",
    FontWeight = "bold"
}
```

**出力例**:
```xml
<Color name="Heading" foreground="#0066CC" fontWeight="bold" />
```

**処理ロジック**:
1. 空のリスト `attributes` を作成
2. `def` の各プロパティをチェック
3. null/空でない場合、対応するXML属性文字列を `attributes` に追加
4. すべての属性を結合してXML要素文字列を返す

---

### 3. エラーハンドリング戦略

#### 設定ファイル読み込みエラー

| エラー種別 | 原因 | 対処 |
|----------|------|------|
| ファイル不存在 | 初回起動 | デフォルト設定を使用 & 自動生成 |
| JSON形式エラー | 手動編集ミス | デフォルト設定を使用 & エラーログ |
| プロパティ不足 | 古いバージョンの設定ファイル | 不足分をデフォルト値で補完 |
| 無効な色コード | 手動編集ミス | デフォルト値に置換 |

#### XSHD生成エラー

| エラー種別 | 原因 | 対処 |
|----------|------|------|
| XML生成失敗 | プログラムバグ | 例外をキャッチしてデフォルトXSHDを使用 |
| ハイライト読み込み失敗 | 無効なXSHD | 例外をキャッチしてnullを返す（ハイライトなし） |

**エラーログ出力**:
```csharp
try
{
    // 設定読み込み
}
catch (Exception ex)
{
    System.Diagnostics.Debug.WriteLine($"Failed to load highlight settings: {ex.Message}");
    return GetDefault();
}
```

---

## 📝 設定ファイルのカスタマイズ例

### ダークモード風テーマ

```json
{
  "Heading": {
    "Foreground": "#569CD6",
    "FontWeight": "bold"
  },
  "Bold": {
    "FontWeight": "bold",
    "Foreground": "#DCDCAA"
  },
  "Italic": {
    "FontStyle": "italic",
    "Foreground": "#CE9178"
  },
  "Code": {
    "Foreground": "#D7BA7D",
    "Background": "#1E1E1E",
    "FontFamily": "Consolas"
  },
  "InlineCode": {
    "Foreground": "#D7BA7D",
    "Background": "#2D2D30",
    "FontFamily": "Consolas"
  }
}
```

### ハイコントラストテーマ

```json
{
  "Heading": {
    "Foreground": "#FF0000",
    "FontWeight": "bold"
  },
  "Bold": {
    "FontWeight": "bold",
    "Foreground": "#000000"
  },
  "Italic": {
    "FontStyle": "italic",
    "Foreground": "#0000FF"
  },
  "Code": {
    "Foreground": "#FFFFFF",
    "Background": "#000000",
    "FontFamily": "Consolas"
  },
  "InlineCode": {
    "Foreground": "#FFFFFF",
    "Background": "#000000",
    "FontFamily": "Courier New"
  }
}
```

---

## 🧪 テスト計画

### 単体テスト項目

#### HighlightSettings.Load()
- [ ] 設定ファイルが存在しない場合、デフォルト設定を返す
- [ ] 設定ファイルが存在する場合、正しく読み込める
- [ ] JSON形式エラー時、デフォルト設定を返す
- [ ] プロパティ不足時、デフォルト値で補完される

#### HighlightSettings.Save()
- [ ] 正常に設定ファイルが作成される
- [ ] JSON形式が正しい（インデント付き）
- [ ] ディレクトリが存在しない場合、自動作成される
- [ ] 既存ファイルが正しく上書きされる

#### SyntaxHighlighter.GenerateXshd()
- [ ] デフォルト設定で正しいXSHDが生成される
- [ ] カスタム設定で正しいXSHDが生成される
- [ ] null/空プロパティの場合、該当属性が出力されない
- [ ] 生成されたXSHDが有効なXML形式である

#### SyntaxHighlighter.GetMarkdownHighlighting()
- [ ] デフォルト設定でハイライトが正常に動作
- [ ] カスタム設定でハイライトが正常に動作
- [ ] 無効な設定時、エラーをキャッチしてフォールバック

### 統合テスト項目

#### 初回起動シナリオ
1. [ ] アプリ初回起動時、デフォルト設定ファイルが自動生成される
2. [ ] エディタが正常にハイライト表示される
3. [ ] `%AppData%\MyMarkdownEditor\highlight-settings.json` が存在する

#### 設定カスタマイズシナリオ
1. [ ] 設定ファイルを手動編集
2. [ ] アプリ再起動
3. [ ] カスタム設定でハイライト表示される
4. [ ] 見出しの色が変更されている
5. [ ] コードブロックの背景色が変更されている

#### エラーリカバリーシナリオ
1. [ ] 設定ファイルを無効なJSON形式に編集
2. [ ] アプリ起動
3. [ ] デフォルト設定でハイライト表示される（エラーでクラッシュしない）
4. [ ] デバッグログにエラーメッセージが出力される

#### 後方互換性シナリオ
1. [ ] 旧バージョンの設定ファイル（プロパティ不足）を配置
2. [ ] アプリ起動
3. [ ] 不足分がデフォルト値で補完される
4. [ ] 正常にハイライト表示される

### 手動テスト項目

#### UI確認
- [ ] 各マークダウン要素が正しくハイライトされる
  - [ ] 見出し (#, ##, ###)
  - [ ] 太字 (**text**)
  - [ ] 斜体 (*text*)
  - [ ] インラインコード (`code`)
  - [ ] コードブロック (```)

#### パフォーマンス確認
- [ ] アプリ起動時間が大幅に遅延しない（目安: 1秒以内）
- [ ] 大きなファイル（10,000行以上）でもハイライトが滑らか
- [ ] 設定ファイル読み込みによるメモリ増加が微小（目安: 1MB以下）

---

## 📊 変更影響範囲

### 変更サマリー

| ファイル | 変更種別 | 変更規模 | 推定行数 | 重要度 | テスト優先度 |
|---------|---------|---------|---------|--------|----------|
| `src/Settings/HighlightSettings.cs` | 新規作成 | 中 | 約150行 | 高 | 高 |
| `src/Editor/SyntaxHighlighter.cs` | 大幅修正 | 大 | +100行 | 高 | 高 |
| `src/MainWindow.xaml.cs` | 軽微修正 | 小 | +2行 | 中 | 中 |
| `highlight-settings.json` | 実行時生成 | - | - | 低 | 低 |

### 影響を受けないコンポーネント

以下のコンポーネントは変更不要:
- **ファイル操作**: `FileService.cs` - 変更なし
- **マークダウン補助機能**: `MarkdownAssistant.cs` - 変更なし
- **テキスト整形**: `TextFormatHelper.cs` - 変更なし
- **ウィンドウ設定**: `WindowSettings.cs` - 変更なし
- **UIレイアウト**: `MainWindow.xaml` - 変更なし
- **アプリエントリ**: `App.xaml.cs` - 変更なし

### リグレッションリスク

| リスク項目 | リスク度 | 対策 |
|----------|---------|------|
| 既存のハイライトが動作しなくなる | 中 | デフォルト設定を現在の値と同一にする |
| アプリ起動が遅くなる | 低 | 設定読み込みは高速（JSON deserializeのみ） |
| 設定ファイルエラーでクラッシュ | 中 | try-catchで確実にフォールバック |
| メモリリークの可能性 | 低 | `using`で確実にリソース解放 |

---

## 🚀 実装手順

### Phase 1: 基本実装（優先度: 高）

#### Step 1.1: HighlightSettings.cs の実装
**所要時間**: 約1時間

1. `src/Settings/HighlightSettings.cs` を新規作成
2. `ColorDefinition` クラスを実装
3. `HighlightSettings` クラスの基本構造を実装
4. `GetDefault()` メソッドを実装（現在の値と同一）
5. `Load()` メソッドを実装（基本的なエラーハンドリング含む）
6. `Save()` メソッドを実装

**確認事項**:
- [ ] コンパイルエラーがないこと
- [ ] `Load()` でデフォルト設定が返ること
- [ ] `Save()` で設定ファイルが作成されること

#### Step 1.2: SyntaxHighlighter.cs の修正
**所要時間**: 約1.5時間

1. `GenerateColorElement()` ヘルパーメソッドを実装
2. `GenerateXshd()` メソッドを実装
3. `GetMarkdownHighlighting()` のシグネチャを変更
4. 動的XSHD生成ロジックを実装
5. ハードコードXSHDを削除

**確認事項**:
- [ ] コンパイルエラーがないこと
- [ ] デフォルト設定で以前と同じXSHDが生成されること
- [ ] 生成されたXSHDが有効なXML形式であること

#### Step 1.3: MainWindow.xaml.cs の修正
**所要時間**: 約10分

1. `MainWindow` コンストラクタを修正
2. `HighlightSettings.Load()` 呼び出しを追加
3. 引数に `highlightSettings` を渡す

**確認事項**:
- [ ] コンパイルエラーがないこと
- [ ] アプリが正常に起動すること
- [ ] ハイライトが以前と同じように動作すること

#### Step 1.4: 統合テスト
**所要時間**: 約30分

1. アプリをビルド
2. 初回起動時の動作確認
3. 設定ファイル自動生成の確認
4. ハイライト表示の確認（全要素）

**確認事項**:
- [ ] 0エラー、0警告でビルド成功
- [ ] 初回起動時、`highlight-settings.json` が生成される
- [ ] すべてのマークダウン要素が正しくハイライトされる
- [ ] デフォルト設定が以前の表示と同一である

---

### Phase 2: 品質向上（優先度: 中）

#### Step 2.1: バリデーションの強化
**所要時間**: 約1時間

1. 色コードの正規表現検証を実装
2. `FontWeight`, `FontStyle` の値検証を実装
3. 無効な値をデフォルトに置換するロジックを追加
4. バリデーション結果をデバッグログに出力

**確認事項**:
- [ ] 無効な色コードがデフォルトに置換されること
- [ ] 無効なFontWeightがデフォルトに置換されること
- [ ] アプリがクラッシュしないこと

#### Step 2.2: エラーハンドリングの強化
**所要時間**: 約30分

1. 各エラーケースに対する詳細なログ出力を追加
2. JSON deserializeエラー時のフォールバック処理を確認
3. XSHD生成エラー時のフォールバック処理を実装

**確認事項**:
- [ ] 無効なJSON形式でもクラッシュしないこと
- [ ] エラー時にデフォルト設定が使用されること
- [ ] デバッグログにエラー内容が出力されること

#### Step 2.3: 後方互換性の確保
**所要時間**: 約30分

1. プロパティ不足時の補完ロジックを実装
2. 旧バージョン設定ファイルのマイグレーション処理（必要に応じて）

**確認事項**:
- [ ] 古い設定ファイルでもクラッシュしないこと
- [ ] 不足分がデフォルト値で補完されること

---

### Phase 3: ドキュメント整備（優先度: 低）

#### Step 3.1: カスタマイズガイドの作成
**所要時間**: 約1時間

1. `docs/highlight-customization-guide.md` を作成
2. 設定ファイルの場所を明記
3. 各プロパティの説明を記載
4. カスタマイズ例（ダークモード、ハイコントラスト）を掲載
5. トラブルシューティングセクションを追加

#### Step 3.2: サンプル設定ファイルの提供
**所要時間**: 約30分

1. `docs/sample-highlight-themes/` ディレクトリを作成
2. `default.json` - デフォルト設定
3. `dark.json` - ダークモード風
4. `high-contrast.json` - ハイコントラスト
5. `README.md` - 各テーマの説明

#### Step 3.3: CLAUDE.md の更新
**所要時間**: 約15分

1. 新機能の概要を追記
2. 設定ファイルのパスを明記
3. カスタマイズガイドへのリンクを追加

---

## 📅 タイムライン

| Phase | タスク | 所要時間 | 累積時間 |
|-------|-------|---------|---------|
| Phase 1 | HighlightSettings.cs 実装 | 1.0h | 1.0h |
| Phase 1 | SyntaxHighlighter.cs 修正 | 1.5h | 2.5h |
| Phase 1 | MainWindow.xaml.cs 修正 | 0.2h | 2.7h |
| Phase 1 | 統合テスト | 0.5h | 3.2h |
| Phase 2 | バリデーション強化 | 1.0h | 4.2h |
| Phase 2 | エラーハンドリング強化 | 0.5h | 4.7h |
| Phase 2 | 後方互換性確保 | 0.5h | 5.2h |
| Phase 3 | カスタマイズガイド作成 | 1.0h | 6.2h |
| Phase 3 | サンプル設定提供 | 0.5h | 6.7h |
| Phase 3 | ドキュメント更新 | 0.3h | 7.0h |

**合計推定時間**: 約7時間

---

## 🎯 成功基準

### 機能要件
- [ ] 設定ファイルでシンタックスハイライトの色・スタイルをカスタマイズできる
- [ ] 初回起動時、デフォルト設定ファイルが自動生成される
- [ ] 無効な設定ファイルでもアプリがクラッシュしない
- [ ] デフォルト設定は現在の表示と同一である

### 非機能要件
- [ ] ビルドエラー: 0
- [ ] ビルド警告: 0
- [ ] アプリ起動時間: 1秒以内（設定読み込み含む）
- [ ] コード可読性: コメント充実、命名規則遵守
- [ ] テスト網羅率: 主要メソッドを手動テストでカバー

### ユーザビリティ
- [ ] 設定ファイルの場所が明確（ドキュメント記載）
- [ ] カスタマイズ例が提供されている
- [ ] エラー時の動作が予測可能（デフォルトにフォールバック）

---

## 🔮 今後の拡張候補

以下は本実装の範囲外だが、将来的に検討可能な機能:

### 短期（バージョン1.2候補）
- **テーマプリセット機能**: UI上で複数テーマを切り替え
- **テーマエディタ**: 設定ファイルをGUIで編集
- **カラーピッカー**: 色選択をビジュアルに

### 中期（バージョン2.0候補）
- **背景色全体の変更**: エディタ背景色もカスタマイズ可能に
- **行番号の色変更**: 行番号表示の色をカスタマイズ
- **選択範囲の色変更**: テキスト選択時の色をカスタマイズ

### 長期（バージョン3.0候補）
- **CSS/Sassライクな記法**: より柔軟な色定義
- **プラグインシステム**: 独自のハイライトルールを追加可能に
- **オンラインテーマギャラリー**: コミュニティ作成テーマの共有

---

## 📚 参考資料

### AvalonEdit
- [AvalonEdit GitHub - Syntax Highlighting](https://github.com/icsharpcode/AvalonEdit/wiki/Syntax-Highlighting)
- [XSHD Format Reference](https://github.com/icsharpcode/AvalonEdit/blob/master/ICSharpCode.AvalonEdit/Highlighting/Xshd/V2Syntax.xsd)

### .NET
- [System.Text.Json Documentation](https://docs.microsoft.com/ja-jp/dotnet/standard/serialization/system-text-json-overview)
- [JSON Serialization Options](https://docs.microsoft.com/ja-jp/dotnet/standard/serialization/system-text-json-customize-properties)

### 設計パターン
- [Settings Pattern in WPF](https://docs.microsoft.com/ja-jp/dotnet/desktop/wpf/advanced/how-to-persist-and-restore-application-scope-properties)
- [JSON Configuration Best Practices](https://docs.microsoft.com/ja-jp/dotnet/core/extensions/configuration)

---

## ✅ チェックリスト

### 実装開始前
- [ ] 要件定義の確認
- [ ] 既存コードの理解
- [ ] 設計方針の合意

### Phase 1 完了後
- [ ] ビルド成功（0エラー、0警告）
- [ ] 基本機能テスト合格
- [ ] デフォルト設定が以前と同一
- [ ] 設定ファイル自動生成の確認

### Phase 2 完了後
- [ ] バリデーションテスト合格
- [ ] エラーリカバリーテスト合格
- [ ] 後方互換性テスト合格

### Phase 3 完了後
- [ ] カスタマイズガイド作成完了
- [ ] サンプルテーマ提供完了
- [ ] ドキュメント更新完了

### リリース前
- [ ] 全テスト項目合格
- [ ] コードレビュー完了
- [ ] ドキュメントレビュー完了
- [ ] パフォーマンス確認完了

---

**計画作成日**: 2025-11-10
**計画作成者**: Claude Code
**対象バージョン**: 1.1
**推定リリース日**: Phase 1完了後（約1週間）
