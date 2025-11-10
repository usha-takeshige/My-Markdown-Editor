# メモリ使用状況の調査報告

**調査日**: 2025-11-11
**現状のメモリ使用量**: 約800MB
**目標**: メモリ使用量の削減

## 調査概要

軽量なマークダウンエディタとして800MBのメモリ消費は過大であるため、コード全体を調査してメモリ消費の原因を特定し、改善案を策定しました。

## メモリ消費の内訳

### ベースライン（削減困難な部分）

| コンポーネント | メモリ使用量 |
|--------------|------------|
| .NET 9 ランタイム | 150-200MB |
| WPF Framework | 100-150MB |
| AvalonEdit 基本 | 100-150MB |
| **合計** | **350-500MB** |

### 最適化可能な部分

残りの **300-450MB** が最適化可能な領域です。

## 主な原因の詳細

### 1. シンタックスハイライトの動的生成の非効率性

**影響度**: 🔴 高（推定50-100MB）

**問題箇所**: `src/Editor/SyntaxHighlighter.cs:21-46`

**問題点**:
- 起動時に毎回XSHD(XML)を`StringBuilder`で動的生成
- `StringReader` → `XmlReader` → `HighlightingLoader.Load()` という多段階の変換処理
- `HighlightingManager.Instance`を使用しているが、生成したハイライト定義がキャッシュされていない
- 新しいウィンドウを開くたびに再生成される（`MainWindow.xaml.cs:71-72`の`Ctrl+N`処理）

**現在の実装**:
```csharp
public static IHighlightingDefinition GetMarkdownHighlighting(HighlightSettings settings)
{
    try
    {
        var xshdContent = GenerateXshd(settings); // 毎回生成
        using var reader = new StringReader(xshdContent);
        using var xmlReader = XmlReader.Create(reader);
        return HighlightingLoader.Load(xmlReader, HighlightingManager.Instance);
    }
    // ...
}
```

### 2. Regexインスタンスの非効率な生成

**影響度**: 🟡 中（推定10-20MB）

**問題箇所**: `src/Editor/MarkdownAssistant.cs:11-13`

**問題点**:
- 3つのRegexがstaticフィールドで保持されているのは良い設計
- しかし`RegexOptions.Compiled`が指定されていない
- .NET 9では`GeneratedRegex`属性を使えばさらに最適化可能

**現在の実装**:
```csharp
private static readonly Regex UnorderedListPattern = new(@"^(\s*)[-*+]\s+(.*)$");
private static readonly Regex OrderedListPattern = new(@"^(\s*)(\d+)\.\s+(.*)$");
private static readonly Regex QuotePattern = new(@"^(\s*)>\s+(.*)$");
```

### 3. RelayCommandのイベント購読パターン

**影響度**: 🟡 中（推定5-15MB）

**問題箇所**: `src/MainWindow.xaml.cs:382-386`

**問題点**:
- `CommandManager.RequerySuggested`に対する購読
- 各コマンドインスタンス（8個）が個別にCommandManagerに接続
- このイベントはWPF全体で頻繁に発火するため、メモリとCPUの両方に負荷

**現在の実装**:
```csharp
public event EventHandler? CanExecuteChanged
{
    add { CommandManager.RequerySuggested += value; }
    remove { CommandManager.RequerySuggested -= value; }
}
```

### 4. AvalonEditの設定最適化不足

**影響度**: 🟡 中（推定20-50MB）

**問題箇所**: `src/MainWindow.xaml.cs:64-65`

**問題点**:
- Undo/Redoスタックがデフォルト（無制限）のまま
- 一部の不要な機能が有効化されたまま
- 仮想スペースなどのオプション機能が有効

**現在の実装**:
```csharp
TextEditor.Options.EnableHyperlinks = false; // 実装済み
TextEditor.Options.EnableEmailHyperlinks = false; // 実装済み
// その他の最適化が未実装
```

### 5. TextChangedイベントの頻繁な処理

**影響度**: 🟢 低（推定5-10MB、ただしCPU削減効果が大きい）

**問題箇所**: `src/MainWindow.xaml.cs:178-183`

**問題点**:
- TextChangedイベントで毎回文字列長と行数を計算
- タイピング中も毎回更新される
- デバウンス（debounce）処理がない

**現在の実装**:
```csharp
private void TextEditor_TextChanged(object? sender, EventArgs e)
{
    _isModified = true;
    UpdateTitle();
    UpdateStatus(); // 毎回計算
}
```

## 改善案（優先度順）

### 優先度1: シンタックスハイライトの最適化

**推定削減**: 50-100MB

**改善方法A: ハイライト定義のキャッシュ化**

```csharp
public static class SyntaxHighlighter
{
    private static IHighlightingDefinition? _cachedDefinition;
    private static HighlightSettings? _cachedSettings;

    public static IHighlightingDefinition GetMarkdownHighlighting(HighlightSettings settings)
    {
        // 設定が変更されていない場合はキャッシュを返す
        if (_cachedDefinition != null &&
            _cachedSettings != null &&
            SettingsEqual(_cachedSettings, settings))
        {
            return _cachedDefinition;
        }

        // 生成処理（既存のコード）
        var definition = /* 既存の生成コード */;

        _cachedDefinition = definition;
        _cachedSettings = settings;

        return definition;
    }

    private static bool SettingsEqual(HighlightSettings a, HighlightSettings b)
    {
        // 設定の比較ロジック
    }
}
```

**改善方法B: XSHD XMLファイルを静的リソース化（より効果的）**

1. `src/Resources/Markdown.xshd`としてXMLファイルを作成
2. プロジェクトに埋め込みリソースとして追加
3. 起動時に一度だけ読み込み
4. 色のカスタマイズは生成後のColorオブジェクトを直接変更

### 優先度2: Regexの最適化

**推定削減**: 10-20MB

**改善方法A: .NET 7以降の推奨パターン（GeneratedRegex）**

```csharp
public static partial class MarkdownAssistant
{
    [GeneratedRegex(@"^(\s*)[-*+]\s+(.*)$")]
    private static partial Regex UnorderedListPattern();

    [GeneratedRegex(@"^(\s*)(\d+)\.\s+(.*)$")]
    private static partial Regex OrderedListPattern();

    [GeneratedRegex(@"^(\s*)>\s+(.*)$")]
    private static partial Regex QuotePattern();

    // 使用箇所をUnorderedListPattern()に変更（メソッド呼び出し）
}
```

**改善方法B: 最小限の対応（Compiledオプション追加）**

```csharp
private static readonly Regex UnorderedListPattern =
    new(@"^(\s*)[-*+]\s+(.*)$", RegexOptions.Compiled);
private static readonly Regex OrderedListPattern =
    new(@"^(\s*)(\d+)\.\s+(.*)$", RegexOptions.Compiled);
private static readonly Regex QuotePattern =
    new(@"^(\s*)>\s+(.*)$", RegexOptions.Compiled);
```

### 優先度3: RelayCommandの改善

**推定削減**: 5-15MB

**改善方法**:

```csharp
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add
        {
            // canExecuteが存在する場合のみ購読
            if (_canExecute != null)
                CommandManager.RequerySuggested += value;
        }
        remove
        {
            if (_canExecute != null)
                CommandManager.RequerySuggested -= value;
        }
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => _execute();
}
```

### 優先度4: AvalonEditの設定最適化

**推定削減**: 20-50MB

**改善方法**:

```csharp
// MainWindow.xaml.cs の InitializeComponent() 後に追加
public MainWindow()
{
    InitializeComponent();

    // ... 既存のコード ...

    // AvalonEditの最適化設定
    TextEditor.Options.EnableHyperlinks = false; // 既存
    TextEditor.Options.EnableEmailHyperlinks = false; // 既存
    TextEditor.Options.EnableVirtualSpace = false; // 仮想スペース無効化
    TextEditor.Options.HighlightCurrentLine = false; // 現在行ハイライト無効化（必要に応じて）
    TextEditor.Options.ShowBoxForControlCharacters = false; // 制御文字表示無効化

    // Undo/Redoスタックのサイズ制限（重要）
    TextEditor.Document.UndoStack.SizeLimit = 100; // デフォルトは無制限
}
```

### 優先度5: TextChangedイベントの最適化

**推定削減**: 5-10MB（CPU削減効果の方が大きい）

**改善方法: デバウンス処理の実装**

```csharp
private System.Windows.Threading.DispatcherTimer? _updateStatusTimer;

public MainWindow()
{
    // ... 既存のコード ...

    // ステータス更新用タイマーの初期化
    _updateStatusTimer = new System.Windows.Threading.DispatcherTimer
    {
        Interval = TimeSpan.FromMilliseconds(300) // 300ms待機
    };
    _updateStatusTimer.Tick += (s, e) =>
    {
        _updateStatusTimer.Stop();
        UpdateStatus();
    };
}

private void TextEditor_TextChanged(object? sender, EventArgs e)
{
    _isModified = true;
    UpdateTitle();

    // ステータス更新をデバウンス
    _updateStatusTimer?.Stop();
    _updateStatusTimer?.Start();
}
```

### 優先度6: プロジェクト設定の見直し

**推定削減**: 50-100MB（発行後の実行ファイルサイズ）

**改善方法: csprojファイルへの追加**

```xml
<PropertyGroup>
  <OutputType>WinExe</OutputType>
  <TargetFramework>net9.0-windows</TargetFramework>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  <UseWPF>true</UseWPF>

  <!-- 追加: パフォーマンス最適化 -->
  <PublishTrimmed>true</PublishTrimmed>
  <TrimMode>link</TrimMode>
  <PublishReadyToRun>true</PublishReadyToRun>
  <TieredCompilation>true</TieredCompilation>
</PropertyGroup>

<!-- Releaseビルド専用の設定 -->
<PropertyGroup Condition="'$(Configuration)'=='Release'">
  <DebugType>none</DebugType>
  <DebugSymbols>false</DebugSymbols>
</PropertyGroup>
```

## 期待される削減効果まとめ

| 対策 | 推定削減 | 実装難易度 |
|------|----------|-----------|
| シンタックスハイライト最適化 | 50-100MB | 中 |
| Regex最適化 | 10-20MB | 低 |
| RelayCommand改善 | 5-15MB | 低 |
| AvalonEdit設定 | 20-50MB | 低 |
| TextChanged最適化 | 5-10MB | 低 |
| プロジェクト設定 | 50-100MB | 低 |
| **合計** | **140-295MB** | - |

**最適化後の予想メモリ使用量**: **505-660MB**

## さらなる削減が必要な場合の抜本的対策

メモリを300MB以下に抑えたい場合は、以下の構造的な見直しが必要です。

### 1. AvalonEditの代替検討

**メリット**: 100-150MBの削減が見込める
**デメリット**: 機能の大幅な劣化

**代替案**:
- 標準の`TextBox`（シンプル、機能最小限）
- `RichTextBox`（書式対応、中程度の機能）
- カスタム実装の軽量エディタ（開発コスト大）

### 2. AOT（Ahead-of-Time）コンパイルの採用

**メリット**: ランタイムのメモリ削減、起動速度向上
**デメリット**: WPFとの互換性に課題あり（.NET 9時点）

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
</PropertyGroup>
```

### 3. WPFの代替検討

**代替案A: WinForms**
- メリット: さらに軽量（50-100MB削減）
- デメリット: UIの見た目が古い、カスタマイズ性低い

**代替案B: Avalonia UI**
- メリット: クロスプラットフォーム、モダンなUI、比較的軽量
- デメリット: 学習コスト、AvalonEditの移植が必要

## 推奨される実装順序

### フェーズ1: 低難易度・高効果（即実装可能）
1. AvalonEdit設定最適化（優先度4）
2. Regex最適化（優先度2）
3. RelayCommand改善（優先度3）
4. プロジェクト設定見直し（優先度6）

**期待削減**: 85-185MB

### フェーズ2: 中難易度・高効果
1. シンタックスハイライトのキャッシュ化（優先度1）
2. TextChangedイベント最適化（優先度5）

**期待削減**: 55-110MB

### フェーズ3: 構造的見直し（必要な場合のみ）
- AvalonEditの代替検討
- フレームワークの変更

## メモリリークの可能性について

コード調査の結果、以下の点を確認しました。

### 問題なし（適切に実装されている箇所）

- **イベントハンドラーの登録**: XAMLでバインドされており、Windowのライフサイクルと連動
- **Dispose処理**: `StringReader`, `XmlReader`が`using`で適切に破棄されている
- **静的フィールド**: Regexなど、意図的にキャッシュされるべきものだけが静的

### 潜在的な問題

- **新規ウィンドウ作成**: `MainWindow.xaml.cs:71-72`で新規ウィンドウを開くが、古いウィンドウが閉じられない場合にメモリが蓄積
  - 現状では問題ないが、複数ウィンドウを開きっぱなしにすると累積する

## 補足: メモリプロファイリング方法

実際の効果を測定するには、以下のツールを使用してください。

### Visual Studio Diagnostic Tools
1. Debug > Performance Profiler
2. .NET Object Allocation Tracking を選択
3. アプリを実行して測定

### dotnet-counters（コマンドライン）
```bash
dotnet tool install --global dotnet-counters
dotnet-counters monitor --process-id [PID] --counters System.Runtime
```

### 測定すべき指標
- Working Set（作業セット）: 実際のメモリ使用量
- Private Bytes: プロセス専有メモリ
- Gen 0/1/2 Collections: GCの動作頻度

---

**作成日**: 2025-11-11
**対象バージョン**: My Markdown Editor v1.0
**次回見直し**: 最適化実装後
