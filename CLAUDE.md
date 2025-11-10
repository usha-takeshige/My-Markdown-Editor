# My Markdown Editor - 開発ガイド

## プロジェクト概要

Windowsデスクトップ向けのシンプルなマークダウンエディタ。
C#で実装し、マークダウンファイルの直接編集に特化した軽量なメモアプリケーションです。

## 要件サマリー

詳細は [docs/reqirements.md](docs/reqirements.md) を参照してください。

### 主な特徴
- プレビュー機能なし（マークダウン記法を直接編集）
- 単一ファイル編集モデル
- シンタックスハイライト対応
- マークダウン補助機能（リスト自動継続など）
- キーボードショートカット中心の操作

## 技術スタック

### 開発環境
- **言語**: C# (.NET 9.0)
- **UI フレームワーク**: WPF (Windows Presentation Foundation)
- **IDE**: VS Code + C# Dev Kit

### 主要ライブラリ（候補）
- **テキストエディタコントロール**: AvalonEdit
  - シンタックスハイライト機能を標準サポート
  - 行番号表示、折り返し、Undo/Redo対応
  - マークダウン用のカスタムハイライト定義が可能

### ファイル構造案
```
My-Markdown-Editor/
├── src/
│   ├── App.xaml / App.xaml.cs           # アプリケーションエントリポイント
│   ├── MainWindow.xaml / MainWindow.xaml.cs  # メインウィンドウ
│   ├── Editor/
│   │   ├── MarkdownEditor.cs            # エディタコントロールのラッパー
│   │   ├── SyntaxHighlighter.cs         # マークダウンハイライト定義
│   │   └── AutoCompletionHelper.cs      # リスト自動継続など
│   ├── FileManager/
│   │   └── FileService.cs               # ファイル開く/保存/新規作成
│   ├── Settings/
│   │   └── WindowSettings.cs            # ウィンドウサイズ記憶
│   └── Utils/
│       └── EncodingHelper.cs            # UTF-8(BOMなし)固定
├── docs/
│   └── reqirements.md                   # 要件定義書
├── CLAUDE.md                            # このファイル
└── README.md                            # プロジェクト説明
```

## 実装の優先順位

### フェーズ1: 基本機能
1. **プロジェクト作成**
   - WPFアプリケーションのセットアップ
   - AvalonEditのNuGetパッケージ導入

2. **基本UI構築**
   - メニューバーなしのシンプルなウィンドウ
   - エディタエリアの配置
   - ステータスバー（文字数・行数表示）

3. **ファイル操作**
   - 新規作成 (Ctrl+N)
   - 開く (Ctrl+O)
   - 保存 (Ctrl+S)
   - UTF-8(BOMなし)での読み書き
   - 未保存時の確認ダイアログ

### フェーズ2: エディタ機能強化
1. **基本エディタ機能**
   - 行番号表示
   - Undo/Redo (Ctrl+Z / Ctrl+Y)
   - タイトルバーにファイルパス表示

2. **ウィンドウ設定**
   - ウィンドウサイズの記憶・復元
   - フォント設定（Meiryo UI, 12pt固定）

### フェーズ3: マークダウン対応
1. **シンタックスハイライト**
   - 見出し (#, ##, ###)
   - 太字 (**text**)
   - 斜体 (*text*)
   - コードブロック (```, `inline`)

2. **マークダウン補助機能**
   - リスト自動継続 (`- ` + Enter)
   - 番号付きリスト自動インクリメント (`1. ` + Enter → `2. `)
   - 引用の自動継続 (`> ` + Enter)

## 開発時の注意点

### シンタックスハイライト実装
- AvalonEditの`IHighlightingDefinition`を使用
- 正規表現ベースでマークダウン記法をパターンマッチ
- カラースキームは視認性重視（例: 見出しは太字+青、コードブロックはグレー背景）

### ファイル操作の注意
- 常にUTF-8(BOMなし)で保存
- ファイルが開けない場合のエラーハンドリング
- 大きなファイルのパフォーマンス考慮（ただし個人メモ用途なので優先度低）

### キーボードショートカット
- WPFの`InputBindings`で実装
- すべての操作をキーボードで完結できるように

### ウィンドウ状態の永続化
- `Application Settings`または独自の設定ファイル（JSON等）を使用
- 保存項目: Width, Height, WindowState（最大化/通常）

## テスト戦略

### 手動テスト項目
- [ ] 新規作成→編集→保存の基本フロー
- [ ] 既存ファイルを開く→編集→上書き保存
- [ ] 未保存状態でウィンドウを閉じる（確認ダイアログ表示）
- [ ] Ctrl+Nで新しいウィンドウが開く
- [ ] ウィンドウサイズを変更して終了→再起動で復元
- [ ] 各種マークダウン記法のハイライト表示
- [ ] リスト・番号付きリスト・引用の自動継続


## リリース計画

### MVP (Minimum Viable Product)
- フェーズ1の基本機能
- フェーズ2のエディタ機能
- 最低限のシンタックスハイライト（見出しのみ）

### バージョン1.0
- すべてのマークダウンハイライト対応
- マークダウン補助機能完全実装
- ウィンドウ状態の記憶機能

## 参考資料

### AvalonEdit
- 公式: https://github.com/icsharpcode/AvalonEdit
- カスタムハイライト定義: ドキュメント内の`Highlighting`セクション参照

### マークダウン記法
- CommonMark仕様: https://commonmark.org/
- GitHub Flavored Markdown: https://github.github.com/gfm/

### WPF
- Microsoft Docs: https://docs.microsoft.com/ja-jp/dotnet/desktop/wpf/

## 今後の拡張案（要件外）

以下は現時点の要件には含まれないが、将来的に検討可能な機能：
- ダークモード対応
- 複数ファイルのタブ切り替え
- 検索・置換機能 (Ctrl+F)
- マークダウンテーブルのサポート
- 簡易的なプレビュー機能（オプション）
- ドラッグ&ドロップでファイルを開く

---

**Last Updated**: 2025-11-10
**Target Platform**: Windows 10/11
**License**: (未定)
