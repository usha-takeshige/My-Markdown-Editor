using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using MyMarkdownEditor.Editor;
using MyMarkdownEditor.FileManager;
using MyMarkdownEditor.Settings;

namespace MyMarkdownEditor;

/// <summary>
/// MainWindow.xaml の相互作用ロジック
/// </summary>
public partial class MainWindow : Window
{
    private string? _currentFilePath;
    private bool _isModified;

    public ICommand NewCommand { get; }
    public ICommand OpenCommand { get; }
    public ICommand SaveCommand { get; }

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

        // コマンドの初期化
        NewCommand = new RelayCommand(NewFile);
        OpenCommand = new RelayCommand(OpenFile);
        SaveCommand = new RelayCommand(SaveFile);

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

    private void NewFile()
    {
        // 新しいウィンドウを開いて新規作成
        var newWindow = new MainWindow();
        newWindow.Show();
    }

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

    private void SaveFile()
    {
        if (string.IsNullOrEmpty(_currentFilePath))
        {
            // 名前を付けて保存
            var dialog = new SaveFileDialog
            {
                Filter = "Markdown files (*.md)|*.md|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = ".md",
                Title = "名前を付けて保存"
            };

            if (dialog.ShowDialog() == true)
            {
                _currentFilePath = dialog.FileName;
            }
            else
            {
                return;
            }
        }

        try
        {
            FileService.SaveFile(_currentFilePath, TextEditor.Text);
            _isModified = false;
            UpdateTitle();
        }
        catch (IOException ex)
        {
            MessageBox.Show(ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

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
            return !_isModified; // 保存に成功した場合のみtrue
        }
        else if (result == MessageBoxResult.No)
        {
            return true;
        }

        return false; // Cancel
    }

    private void UpdateTitle()
    {
        var fileName = string.IsNullOrEmpty(_currentFilePath) ? "無題" : _currentFilePath;
        var modified = _isModified ? "*" : "";
        Title = $"{modified}{fileName} - My Markdown Editor";
    }

    private void UpdateStatus()
    {
        var lineCount = TextEditor.LineCount;
        var charCount = TextEditor.Text.Length;
        StatusText.Text = $"文字数: {charCount} / 行数: {lineCount}";
    }

    private void TextEditor_TextChanged(object? sender, EventArgs e)
    {
        _isModified = true;
        UpdateTitle();
        UpdateStatus();
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_isModified)
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

    #region 背景色設定メソッド

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

    private void TextEditor_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Ctrl修飾子付きのカスタムショートカットを優先処理
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            switch (e.Key)
            {
                case Key.B:
                    e.Handled = true;
                    FormatBold();
                    return;
                case Key.I:
                    e.Handled = true;
                    FormatItalic();
                    return;
                case Key.D2:
                    e.Handled = true;
                    FormatQuote();
                    return;
                case Key.OemTilde: // Oem3と同じ値
                    e.Handled = true;
                    FormatInlineCode();
                    return;
                case Key.Oem6:
                    e.Handled = true;
                    IncreaseIndent();
                    return;
                case Key.OemOpenBrackets: // Oem4と同じ値
                    e.Handled = true;
                    DecreaseIndent();
                    return;
            }
        }

        // Tabキーの処理（リスト行でのみインデント操作として機能）
        if (e.Key == Key.Tab)
        {
            // 現在行を取得
            int caretOffset = TextEditor.CaretOffset;
            var document = TextEditor.Document;
            var line = document.GetLineByOffset(caretOffset);
            string lineText = document.GetText(line.Offset, line.Length);

            // リストまたは引用の場合はインデント操作
            if (TextFormatHelper.IsListOrQuoteLine(lineText))
            {
                e.Handled = true; // デフォルト動作を抑制
                IncreaseIndent();
                return;
            }
            // 通常行の場合はデフォルト動作（タブ文字挿入）
        }

        // Enterキーが押された場合
        if (e.Key == Key.Enter)
        {
            // 現在のキャレット位置を取得
            int caretOffset = TextEditor.CaretOffset;
            var document = TextEditor.Document;

            // 現在行の番号を取得
            var line = document.GetLineByOffset(caretOffset);
            int lineNumber = line.LineNumber;

            // 現在行のテキストを取得
            string lineText = document.GetText(line.Offset, line.Length);

            // 次行のプレフィックスを取得
            string nextPrefix = MarkdownAssistant.GetNextLinePrefix(lineText, out bool shouldContinue);

            if (shouldContinue && !string.IsNullOrEmpty(nextPrefix))
            {
                // デフォルトのEnter処理を抑制して、自分たちで改行とプレフィックスを挿入
                e.Handled = true;

                // 改行を挿入
                document.Insert(caretOffset, Environment.NewLine);

                // 新しい行の先頭にプレフィックスを挿入
                int newCaretOffset = caretOffset + Environment.NewLine.Length;
                document.Insert(newCaretOffset, nextPrefix);

                // カーソルをプレフィックスの後ろに移動
                TextEditor.CaretOffset = newCaretOffset + nextPrefix.Length;
            }
            else if (MarkdownAssistant.IsMarkdownPattern(lineText) && !shouldContinue)
            {
                // コンテンツが空のリスト行でEnterを押した場合、マーカーを削除
                e.Handled = true;

                // 現在行のマーカー部分を削除
                document.Remove(line.Offset, line.Length);

                // 改行を挿入
                document.Insert(line.Offset, Environment.NewLine);
                TextEditor.CaretOffset = line.Offset + Environment.NewLine.Length;
            }
        }
    }
}

/// <summary>
/// シンプルなRelayCommandの実装
/// </summary>
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
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();
}