# My Markdown Editor

シンプルで軽量なWindowsデスクトップ向けマークダウンエディタです。
マークダウン記法を直接編集することに特化し、プレビュー機能を持たない潔いデザインで、快適な執筆体験を提供します。

![License](https://img.shields.io/badge/license-未定-lightgrey)
![.NET](https://img.shields.io/badge/.NET-9.0-blue)
![Platform](https://img.shields.io/badge/platform-Windows-lightblue)

---

## 特徴

### シンプルな編集体験
- **プレビューなし**: マークダウン記法を直接編集する潔いデザイン
- **軽量**: 単一ファイル編集モデルで高速起動
- **キーボード中心**: すべての操作をキーボードショートカットで完結

### 充実したエディタ機能
- **シンタックスハイライト**: 見出し、太字、斜体、コードブロックなどをハイライト表示
- **マークダウン補助**: リスト自動継続、番号付きリスト自動インクリメント、引用自動継続
- **行番号表示**: 編集中のドキュメントの行数を一目で確認
- **文字数・行数カウント**: ステータスバーでリアルタイム表示

### カスタマイズ可能
- **ハイライト設定**: 色、フォント、スタイルを自由にカスタマイズ可能
- **背景色**: お好みの背景色に変更可能
- **ウィンドウ状態の記憶**: サイズと位置を記憶し、次回起動時に復元

---

## システム要件

- **OS**: Windows 10/11
- **.NET Runtime**: .NET 9.0以降
- **ディスプレイ**: 解像度 1280×720 以上推奨

---

## インストール

### 1. リポジトリのクローン
```bash
git clone https://github.com/usha-takeshige/My-Markdown-Editor.git
cd My-Markdown-Editor
```

### 2. ビルド
```bash
cd src
dotnet build
```

### 3. 実行
```bash
dotnet run
```

または、リリースビルドを作成:
```bash
dotnet publish -c Release -o ./publish
```

---

## 使い方

### 基本操作

#### ファイル操作
| 操作 | ショートカット | 説明 |
|------|----------------|------|
| **新規作成** | `Ctrl+N` | 新しいウィンドウで新規ファイルを作成 |
| **開く** | `Ctrl+O` | ファイルを開く（新規空白の場合は現在のウィンドウ、それ以外は新しいウィンドウで開く） |
| **保存** | `Ctrl+S` | 現在のファイルを保存（初回は「名前を付けて保存」ダイアログが表示） |
| **閉じる** | `Ctrl+W` | 現在のウィンドウを閉じる（未保存の場合は確認ダイアログ表示） |

#### テキスト整形
| 操作 | ショートカット | 説明 |
|------|----------------|------|
| **太字** | `Ctrl+B` | 選択範囲を `**` で囲む（選択がない場合は `****` を挿入し、カーソルを中央に配置） |
| **斜体** | `Ctrl+I` | 選択範囲を `*` で囲む（選択がない場合は `**` を挿入し、カーソルを中央に配置） |
| **引用符** | `Ctrl+2` | 選択範囲を `"` で囲む |
| **インラインコード** | ``Ctrl+` `` | 選択範囲を `` ` `` で囲む |

#### インデント操作
| 操作 | ショートカット | 説明 |
|------|----------------|------|
| **インデント増加** | `Ctrl+]` または `Tab`（リスト行のみ） | 現在行のインデントを2スペース増やす |
| **インデント減少** | `Ctrl+[` | 現在行のインデントを2スペース減らす |

#### エディタ機能
| 操作 | ショートカット | 説明 |
|------|----------------|------|
| **元に戻す** | `Ctrl+Z` | 直前の操作を元に戻す |
| **やり直し** | `Ctrl+Y` | 元に戻した操作をやり直す |

---

### マークダウン補助機能

#### リスト自動継続
リスト行（`-`, `*`, `+` で始まる行）で `Enter` を押すと、次の行も自動的にリストとして継続します。

```markdown
- アイテム1 [Enter]
- （自動で挿入される）
```

空のリスト行で `Enter` を押すと、リストマーカーが削除されます。

#### 番号付きリスト自動インクリメント
番号付きリスト行（`1.`, `2.` など）で `Enter` を押すと、次の番号が自動的にインクリメントされます。

```markdown
1. 最初のアイテム [Enter]
2. （自動で挿入される）
```

#### 引用自動継続
引用行（`>` で始まる行）で `Enter` を押すと、次の行も自動的に引用として継続します。

```markdown
> 引用テキスト [Enter]
> （自動で挿入される）
```

#### リスト行でのTab動作
リストまたは引用行で `Tab` を押すと、インデントが増加します（通常行では従来通りタブ文字が挿入されます）。

---

## シンタックスハイライト

以下のマークダウン記法がハイライト表示されます：

| 記法 | 例 | 説明 |
|------|-----|------|
| **見出し** | `# Heading` | 1〜6レベルの見出し |
| **太字** | `**bold**` | 太字テキスト |
| **斜体** | `*italic*` | 斜体テキスト |
| **インラインコード** | `` `code` `` | インラインコード |
| **コードブロック** | ` ``` ` | コードブロックマーカー |

---

## カスタマイズ

### 設定ファイルの場所

設定ファイルは以下のディレクトリに保存されます：
```
%APPDATA%\MyMarkdownEditor\
```

### ハイライト設定のカスタマイズ

`highlight-settings.json` を編集することで、シンタックスハイライトの色やスタイルをカスタマイズできます。

#### デフォルト設定
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

#### 設定可能なプロパティ
- `Foreground`: テキスト色（例: `"#FF0000"`, `"#F00"`）
- `Background`: 背景色（例: `"#FFFF00"`, `"#FF0"`）
- `FontWeight`: フォントの太さ（`"normal"` または `"bold"`）
- `FontStyle`: フォントスタイル（`"normal"` または `"italic"`）
- `FontFamily`: フォント名（例: `"Consolas"`, `"Meiryo UI"`）

### 背景色のカスタマイズ

`window-settings.json` を編集することで、エディタの背景色を変更できます。

```json
{
  "Width": 900,
  "Height": 600,
  "Left": 100,
  "Top": 100,
  "WindowState": 0,
  "BackgroundColor": "#FFFFFF"
}
```

`BackgroundColor` を好みの16進数カラーコード（例: `"#F5F5DC"`, `"#E8E8E8"`）に変更してください。

---

## ファイル構成

```
My-Markdown-Editor/
├── src/
│   ├── App.xaml / App.xaml.cs           # アプリケーションエントリポイント
│   ├── MainWindow.xaml / MainWindow.xaml.cs  # メインウィンドウ
│   ├── Editor/
│   │   ├── MarkdownAssistant.cs         # マークダウン補助機能
│   │   ├── SyntaxHighlighter.cs         # シンタックスハイライト定義
│   │   └── TextFormatHelper.cs          # テキスト整形ヘルパー
│   ├── FileManager/
│   │   └── FileService.cs               # ファイル読み書き (UTF-8 BOMなし)
│   ├── Settings/
│   │   ├── WindowSettings.cs            # ウィンドウ設定管理
│   │   └── HighlightSettings.cs         # ハイライト設定管理
│   ├── Services/
│   │   └── DocumentService.cs           # ドキュメント状態管理
│   ├── Handlers/
│   │   └── KeyboardInputHandler.cs      # キーボード入力処理
│   ├── Commands/
│   │   └── RelayCommand.cs              # WPFコマンドパターン実装
│   └── MyMarkdownEditor.csproj          # プロジェクトファイル
├── docs/
│   ├── reqirements.md                   # 要件定義書
│   └── CLAUDE.md                        # 開発ガイド
├── README.md                            # このファイル
└── CLAUDE.md                            # プロジェクト開発ガイド
```

---

## 技術スタック

- **言語**: C# (.NET 9.0)
- **UIフレームワーク**: WPF (Windows Presentation Foundation)
- **エディタコントロール**: [AvalonEdit](https://github.com/icsharpcode/AvalonEdit) 6.3.1.120
- **開発環境**: VS Code + C# Dev Kit

---

## トラブルシューティング

### ハイライトが正しく表示されない
1. `%APPDATA%\MyMarkdownEditor\highlight-settings.json` を削除
2. アプリケーションを再起動（デフォルト設定が自動生成されます）

### ウィンドウサイズが記憶されない
1. `%APPDATA%\MyMarkdownEditor\window-settings.json` が正しく保存されているか確認
2. ファイルが破損している場合は削除して再起動

### アプリケーションが起動しない
1. .NET 9.0 Runtimeがインストールされているか確認: `dotnet --version`
2. 必要に応じて[こちら](https://dotnet.microsoft.com/download)からダウンロード

---

## 今後の拡張案

以下は現在の実装には含まれていませんが、将来的に検討可能な機能です：

- ダークモード対応
- 複数ファイルのタブ切り替え
- 検索・置換機能 (Ctrl+F)
- マークダウンテーブルのサポート
- オプショナルなプレビュー機能
- ドラッグ&ドロップでファイルを開く
- エクスポート機能（HTML、PDFなど）
- スペルチェック機能
- Git統合

---

## ライセンス

未定（後日追加予定）

---

## コントリビューション

バグ報告や機能リクエストは [Issues](https://github.com/usha-takeshige/My-Markdown-Editor/issues) までお願いします。

---

## 作者

usha-takeshige

---

## 謝辞

- [AvalonEdit](https://github.com/icsharpcode/AvalonEdit) - 優れたテキストエディタコントロール
- [CommonMark](https://commonmark.org/) - マークダウン仕様

---

**Last Updated**: 2025-11-11
