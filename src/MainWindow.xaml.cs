using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using MyMarkdownEditor.Commands;
using MyMarkdownEditor.Editor;
using MyMarkdownEditor.FileManager;
using MyMarkdownEditor.Handlers;
using MyMarkdownEditor.Services;
using MyMarkdownEditor.Settings;

namespace MyMarkdownEditor;

/// <summary>
/// MainWindow.xaml の相互作用ロジック
/// リファクタリング後: DocumentServiceとKeyboardInputHandlerを使用した責任の分離
/// </summary>
public partial class MainWindow : Window
{
    // サービス
    private readonly DocumentService _documentService;
    private readonly KeyboardInputHandler _keyboardHandler;

    // コマンド
    public ICommand NewCommand { get; }
    public ICommand OpenCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CloseCommand { get; }

    // テキスト整形コマンド
    public ICommand FormatBoldCommand { get; }
    public ICommand FormatItalicCommand { get; }
    public ICommand FormatQuoteCommand { get; }
    public ICommand FormatInlineCodeCommand { get; }

    // インデント操作コマンド
    public ICommand IncreaseIndentCommand { get; }
    public ICommand DecreaseIndentCommand { get; }

    public MainWindow()
    {
        InitializeComponent();

        // サービスの初期化
        _documentService = new DocumentService();

        // KeyboardInputHandlerの初期化
        _keyboardHandler = new KeyboardInputHandler(
            getCaretOffset: () => TextEditor.CaretOffset,
            setCaretOffset: (offset) => TextEditor.CaretOffset = offset,
            getDocument: () => TextEditor.Document,
            formatBold: FormatBold,
            formatItalic: FormatItalic,
            formatQuote: FormatQuote,
            formatInlineCode: FormatInlineCode,
            increaseIndent: IncreaseIndent,
            decreaseIndent: DecreaseIndent
        );

        // DocumentServiceのイベント購読
        _documentService.DocumentChanged += (s, e) => UpdateTitle();
        _documentService.FilePathChanged += (s, e) => UpdateTitle();

        // コマンドの初期化
        NewCommand = new RelayCommand(NewFile);
        OpenCommand = new RelayCommand(OpenFile);
        SaveCommand = new RelayCommand(SaveFile);
        CloseCommand = new RelayCommand(CloseWindow);

        // テキスト整形コマンドの初期化
        FormatBoldCommand = new RelayCommand(FormatBold);
        FormatItalicCommand = new RelayCommand(FormatItalic);
        FormatQuoteCommand = new RelayCommand(FormatQuote);
        FormatInlineCodeCommand = new RelayCommand(FormatInlineCode);

        // インデント操作コマンドの初期化
        IncreaseIndentCommand = new RelayCommand(IncreaseIndent);
        DecreaseIndentCommand = new RelayCommand(DecreaseIndent);

        DataContext = this;

        // シンタックスハイライトの設定
        var highlightSettings = HighlightSettings.Load();
        TextEditor.SyntaxHighlighting = SyntaxHighlighter.GetMarkdownHighlighting(highlightSettings);

        // ウィンドウ設定の復元
        var settings = WindowSettings.Load();
        settings.ApplyToWindow(this);
        ApplyBackgroundColor(settings.BackgroundColor);

        // Undo/Redoの有効化（AvalonEditはデフォルトで有効）
        TextEditor.Options.EnableHyperlinks = false;
        TextEditor.Options.EnableEmailHyperlinks = false;
    }

    #region ファイル操作メソッド

    /// <summary>
    /// 新しいウィンドウを開く
    /// </summary>
    private void NewFile()
    {
        var newWindow = new MainWindow();
        newWindow.Show();
    }

    /// <summary>
    /// ファイルを開く
    /// 新規空白の場合は現在のウィンドウで、それ以外は新しいウィンドウで開く
    /// </summary>
    private void OpenFile()
    {
        // 現在のウィンドウの状態を判定
        bool isNewFileAndEmpty = _documentService.IsNewAndEmpty(TextEditor.Text);

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

    /// <summary>
    /// 現在のウィンドウでファイルを開く
    /// </summary>
    private void OpenFileInCurrentWindow()
    {
        var filePath = _documentService.ShowOpenFileDialog();
        if (filePath == null)
            return;

        try
        {
            var content = _documentService.LoadFile(filePath);
            TextEditor.Text = content;
            _documentService.SetFilePath(filePath);
            _documentService.SetModified(false);
        }
        catch (IOException ex)
        {
            MessageBox.Show(ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 新しいウィンドウでファイルを開く
    /// </summary>
    private void OpenFileInNewWindow()
    {
        var filePath = _documentService.ShowOpenFileDialog();
        if (filePath == null)
            return;

        try
        {
            // 新しいウィンドウを開いてファイルを読み込む
            var newWindow = new MainWindow();
            var content = newWindow._documentService.LoadFile(filePath);
            newWindow.TextEditor.Text = content;
            newWindow._documentService.SetFilePath(filePath);
            newWindow._documentService.SetModified(false);
            newWindow.Show();
        }
        catch (IOException ex)
        {
            MessageBox.Show(ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// ファイルを保存する
    /// </summary>
    private void SaveFile()
    {
        string? filePath = _documentService.CurrentFilePath;

        // ファイルパスが未設定の場合は「名前を付けて保存」ダイアログを表示
        if (!_documentService.HasFilePath)
        {
            filePath = _documentService.ShowSaveFileDialog();
            if (filePath == null)
                return;

            _documentService.SetFilePath(filePath);
        }

        try
        {
            _documentService.SaveFile(filePath!, TextEditor.Text);
            _documentService.SetModified(false);
        }
        catch (IOException ex)
        {
            MessageBox.Show(ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// ウィンドウを閉じる
    /// </summary>
    private void CloseWindow()
    {
        // Close() を呼び出すと Window_Closing イベントが発火するため、
        // 未保存の変更がある場合は自動的に確認ダイアログが表示される
        Close();
    }

    /// <summary>
    /// 未保存の変更を保存するか確認する
    /// </summary>
    /// <returns>閉じる処理を続行する場合はtrue、キャンセルする場合はfalse</returns>
    private bool ConfirmSave()
    {
        var result = MessageBox.Show(
            "変更が保存されていません。保存しますか？",
            "確認",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question
        );

        if (result == MessageBoxResult.Yes)
        {
            SaveFile();
            return !_documentService.IsModified; // 保存に成功した場合のみtrue
        }
        else if (result == MessageBoxResult.No)
        {
            return true;
        }

        return false; // Cancel
    }

    #endregion

    #region テキスト整形メソッド

    private void FormatBold()
    {
        int selectionStart = TextEditor.SelectionStart;
        int selectionLength = TextEditor.SelectionLength;
        int newCaretOffset = TextFormatHelper.WrapSelection(
            TextEditor.Document, selectionStart, selectionLength, "**");
        TextEditor.CaretOffset = newCaretOffset;
        TextEditor.Focus();
    }

    private void FormatItalic()
    {
        int selectionStart = TextEditor.SelectionStart;
        int selectionLength = TextEditor.SelectionLength;
        int newCaretOffset = TextFormatHelper.WrapSelection(
            TextEditor.Document, selectionStart, selectionLength, "*");
        TextEditor.CaretOffset = newCaretOffset;
        TextEditor.Focus();
    }

    private void FormatQuote()
    {
        int selectionStart = TextEditor.SelectionStart;
        int selectionLength = TextEditor.SelectionLength;
        int newCaretOffset = TextFormatHelper.WrapSelection(
            TextEditor.Document, selectionStart, selectionLength, "\"");
        TextEditor.CaretOffset = newCaretOffset;
        TextEditor.Focus();
    }

    private void FormatInlineCode()
    {
        int selectionStart = TextEditor.SelectionStart;
        int selectionLength = TextEditor.SelectionLength;
        int newCaretOffset = TextFormatHelper.WrapSelection(
            TextEditor.Document, selectionStart, selectionLength, "`");
        TextEditor.CaretOffset = newCaretOffset;
        TextEditor.Focus();
    }

    #endregion

    #region インデント操作メソッド

    private void IncreaseIndent()
    {
        int caretOffset = TextEditor.CaretOffset;
        int newCaretOffset = TextFormatHelper.IncreaseIndent(
            TextEditor.Document, caretOffset, useSpaces: true);
        TextEditor.CaretOffset = newCaretOffset;
        TextEditor.Focus();
    }

    private void DecreaseIndent()
    {
        int caretOffset = TextEditor.CaretOffset;
        int newCaretOffset = TextFormatHelper.DecreaseIndent(
            TextEditor.Document, caretOffset, useSpaces: true);
        TextEditor.CaretOffset = newCaretOffset;
        TextEditor.Focus();
    }

    #endregion

    #region UI更新メソッド

    /// <summary>
    /// タイトルバーを更新する
    /// </summary>
    private void UpdateTitle()
    {
        var fileName = _documentService.HasFilePath ? _documentService.CurrentFilePath : "無題";
        var modified = _documentService.IsModified ? "*" : "";
        Title = $"{modified}{fileName} - My Markdown Editor";
    }

    /// <summary>
    /// ステータスバーを更新する
    /// </summary>
    private void UpdateStatus()
    {
        var lineCount = TextEditor.LineCount;
        var charCount = TextEditor.Text.Length;
        StatusText.Text = $"文字数: {charCount} / 行数: {lineCount}";
    }

    /// <summary>
    /// 背景色を適用する
    /// </summary>
    /// <param name="hexColor">16進数カラーコード</param>
    private void ApplyBackgroundColor(string hexColor)
    {
        try
        {
            var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hexColor);
            TextEditor.Background = new System.Windows.Media.SolidColorBrush(color);
        }
        catch
        {
            // 無効な色の場合はデフォルトの白を使用
            TextEditor.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
        }
    }

    #endregion

    #region イベントハンドラー

    /// <summary>
    /// テキストが変更されたときの処理
    /// </summary>
    private void TextEditor_TextChanged(object? sender, EventArgs e)
    {
        _documentService.SetModified(true);
        UpdateStatus();
    }

    /// <summary>
    /// キーボード入力の前処理
    /// KeyboardInputHandlerに処理を委譲
    /// </summary>
    private void TextEditor_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        _keyboardHandler.HandlePreviewKeyDown(e);
    }

    /// <summary>
    /// ウィンドウを閉じる前の処理
    /// </summary>
    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_documentService.IsModified)
        {
            if (!ConfirmSave())
            {
                e.Cancel = true;
                return;
            }
        }

        // ウィンドウ設定の保存
        var settings = WindowSettings.FromWindow(this);
        settings.Save();
    }

    #endregion
}
