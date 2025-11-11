# Issue #14: ファイルを開く動作の改善

## 概要

Ctrl+O（ファイルを開く）の挙動を改善し、ウィンドウの状態に応じて適切な動作を行うようにしました。

## 変更内容

### 変更前の動作

- Ctrl+O → 常に現在のウィンドウでファイルを開く
- 編集中の内容がある場合は保存確認ダイアログを表示

### 変更後の動作

#### ケース1: 新規ファイルで未編集の場合
**条件:**
- 現在のウィンドウが新規ファイル（ファイルパスが未設定）
- かつ、編集されていない（`_isModified`がfalse）
- かつ、テキストが空白

**動作:**
- 現在のウィンドウでファイルを開く
- 空のウィンドウを有効活用

#### ケース2: それ以外の場合
**条件:**
- 既にファイルを開いている
- または、編集中の内容がある
- または、テキストが入力されている

**動作:**
- 新しいウィンドウを開いてファイルを表示
- 現在の作業を保持したまま、別のファイルを参照可能

## 実装の詳細

### 変更ファイル
- `src/MainWindow.xaml.cs`

### 追加/変更メソッド

#### 1. `OpenFile()` メソッドの再構築

```csharp
private void OpenFile()
{
    // 現在のウィンドウの状態を判定
    bool isNewFileAndEmpty = string.IsNullOrEmpty(_currentFilePath) &&
                             !_isModified &&
                             string.IsNullOrEmpty(TextEditor.Text);

    // 新規ファイルで未編集の場合は現在のウィンドウでファイルを開く
    if (isNewFileAndEmpty)
    {
        OpenFileInCurrentWindow();
    }
    else
    {
        // それ以外は新しいウィンドウを開いてファイルを開く
        OpenFileInNewWindow();
    }
}
```

**判定ロジック:**
- `_currentFilePath`が空 → 新規ファイル
- `_isModified`がfalse → 未編集
- `TextEditor.Text`が空 → 空白状態

#### 2. `OpenFileInCurrentWindow()` メソッド（新規）

```csharp
private void OpenFileInCurrentWindow()
{
    var dialog = new OpenFileDialog
    {
        Filter = "Markdown files (*.md)|*.md|Text files (*.txt)|*.txt|All files (*.*)|*.*",
        Title = "ファイルを開く"
    };

    if (dialog.ShowDialog() == true)
    {
        try
        {
            var content = FileService.ReadFile(dialog.FileName);
            TextEditor.Text = content;
            _currentFilePath = dialog.FileName;
            _isModified = false;
            UpdateTitle();
        }
        catch (IOException ex)
        {
            MessageBox.Show(ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
```

**機能:**
- 従来のファイルを開く処理を維持
- 現在のウィンドウでファイルを読み込む

#### 3. `OpenFileInNewWindow()` メソッド（新規）

```csharp
private void OpenFileInNewWindow()
{
    var dialog = new OpenFileDialog
    {
        Filter = "Markdown files (*.md)|*.md|Text files (*.txt)|*.txt|All files (*.*)|*.*",
        Title = "ファイルを開く"
    };

    if (dialog.ShowDialog() == true)
    {
        try
        {
            // 新しいウィンドウを開いてファイルを読み込む
            var newWindow = new MainWindow();
            var content = FileService.ReadFile(dialog.FileName);
            newWindow.TextEditor.Text = content;
            newWindow._currentFilePath = dialog.FileName;
            newWindow._isModified = false;
            newWindow.UpdateTitle();
            newWindow.Show();
        }
        catch (IOException ex)
        {
            MessageBox.Show(ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
```

**機能:**
- 新しいウィンドウインスタンスを作成
- ファイルを読み込んでウィンドウに設定
- `_isModified`をfalseに設定（保存済み状態）
- タイトルを更新してファイルパスを表示
- ウィンドウを表示

## ユーザーエクスペリエンスの改善

### 改善点1: 空のウィンドウの有効活用
アプリケーション起動直後やCtrl+Nで新しいウィンドウを開いた直後、何も編集していない状態でファイルを開く場合、不要なウィンドウが残らず、スマートに動作します。

**シナリオ例:**
1. アプリケーションを起動（空のウィンドウが表示）
2. Ctrl+Oでファイルを開く
3. → 現在のウィンドウでファイルが開かれる（新しいウィンドウは作られない）

### 改善点2: 作業中の内容を保持
既にファイルを編集している、または保存済みファイルを開いている状態で別のファイルを参照したい場合、現在の作業を保持したまま新しいウィンドウで開くことができます。

**シナリオ例:**
1. ファイルAを編集中
2. Ctrl+OでファイルBを開く
3. → 新しいウィンドウでファイルBが開かれる（ファイルAの編集は継続可能）

### 改善点3: VSCodeライクな動作
主要なテキストエディタ（VSCode、Sublime Textなど）と同様の直感的な動作を提供します。

## テストシナリオ

### テスト1: 新規ファイルで空白の場合
1. アプリケーションを起動
2. 何も入力せずにCtrl+O
3. ファイルを選択
4. **期待結果:** 現在のウィンドウでファイルが開かれる

### テスト2: 新規ファイルで編集中の場合
1. アプリケーションを起動
2. テキストを入力
3. Ctrl+O
4. ファイルを選択
5. **期待結果:** 新しいウィンドウでファイルが開かれる

### テスト3: 保存済みファイルを開いている場合
1. ファイルAを開く
2. Ctrl+OでファイルBを開く
3. **期待結果:** 新しいウィンドウでファイルBが開かれる

### テスト4: ファイルを編集中の場合
1. ファイルAを開く
2. 内容を編集
3. Ctrl+OでファイルBを開く
4. **期待結果:** 新しいウィンドウでファイルBが開かれる

## 技術的な考慮事項

### TextChangedイベントの処理
新しいウィンドウでファイルを開く際、`TextEditor.Text = content`を実行するとTextChangedイベントが発火し、`_isModified = true`が設定されます。しかし、その直後に`_isModified = false`を明示的に設定しているため、最終的には正しい状態（保存済み）になります。

### イベントハンドラの登録タイミング
- `new MainWindow()`でコンストラクタが実行され、`InitializeComponent()`が呼ばれる
- XAMLで定義されたTextChangedイベントハンドラが登録される
- その後、テキストとファイルパスを設定
- `Show()`でウィンドウを表示

この順序により、すべてのイベントハンドラが正しく機能します。

## 今後の拡張案

### 複数ファイルの同時表示
現在は各ファイルが独立したウィンドウで開かれますが、将来的にタブ機能を追加する場合、この動作を基盤として活用できます。

### ドラッグ&ドロップ対応
ファイルをウィンドウにドラッグ&ドロップした際も、同様のロジックを適用できます。

### 最近開いたファイル
新規ウィンドウでファイルを開く際、最近開いたファイルのリストから選択できる機能を追加できます。

## 関連Issue

- Issue #14: ファイルを開く方法の変更

## 変更履歴

| 日付 | バージョン | 変更内容 |
|------|-----------|---------|
| 2025-11-11 | 1.0 | 初回リリース - Ctrl+Oの動作改善 |

## 参考資料

- VSCode: [ウィンドウ管理の動作](https://code.visualstudio.com/docs/getstarted/userinterface#_command-palette)
- Sublime Text: [ファイル操作](https://www.sublimetext.com/docs/file_management.html)

---

**作成日:** 2025-11-11
**作成者:** Claude Code
**関連ブランチ:** `claude/issue-14-change-open-method-011CV1VfTPzUXmy8A4JkyVPP`
