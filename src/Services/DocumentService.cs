using System;
using System.IO;
using Microsoft.Win32;
using MyMarkdownEditor.FileManager;

namespace MyMarkdownEditor.Services;

/// <summary>
/// ドキュメントの状態管理とファイル操作を担当するサービス
/// ファイルパスと変更状態を管理し、ファイルダイアログとファイルI/Oを処理
/// </summary>
public class DocumentService
{
    private string? _currentFilePath;
    private bool _isModified;

    /// <summary>
    /// 現在のファイルパスを取得
    /// </summary>
    public string? CurrentFilePath => _currentFilePath;

    /// <summary>
    /// ドキュメントが変更されているかどうかを取得
    /// </summary>
    public bool IsModified => _isModified;

    /// <summary>
    /// ファイルパスが設定されているかどうかを取得
    /// </summary>
    public bool HasFilePath => !string.IsNullOrEmpty(_currentFilePath);

    /// <summary>
    /// ドキュメントが変更されたときに発生するイベント
    /// </summary>
    public event EventHandler? DocumentChanged;

    /// <summary>
    /// ファイルパスが変更されたときに発生するイベント
    /// </summary>
    public event EventHandler? FilePathChanged;

    /// <summary>
    /// ファイルを開くダイアログを表示し、選択されたファイルパスを返す
    /// </summary>
    /// <returns>選択されたファイルパス（キャンセルされた場合はnull）</returns>
    public string? ShowOpenFileDialog()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Markdown files (*.md)|*.md|Text files (*.txt)|*.txt|All files (*.*)|*.*",
            Title = "ファイルを開く"
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    /// <summary>
    /// ファイルを保存するダイアログを表示し、選択されたファイルパスを返す
    /// </summary>
    /// <returns>選択されたファイルパス（キャンセルされた場合はnull）</returns>
    public string? ShowSaveFileDialog()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Markdown files (*.md)|*.md|Text files (*.txt)|*.txt|All files (*.*)|*.*",
            DefaultExt = ".md",
            Title = "名前を付けて保存"
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    /// <summary>
    /// 指定されたファイルを読み込む
    /// </summary>
    /// <param name="filePath">読み込むファイルのパス</param>
    /// <returns>ファイルの内容</returns>
    /// <exception cref="IOException">ファイル読み込みに失敗した場合</exception>
    public string LoadFile(string filePath)
    {
        return FileService.ReadFile(filePath);
    }

    /// <summary>
    /// 指定されたファイルに内容を保存する
    /// </summary>
    /// <param name="filePath">保存先のファイルパス</param>
    /// <param name="content">保存する内容</param>
    /// <exception cref="IOException">ファイル保存に失敗した場合</exception>
    public void SaveFile(string filePath, string content)
    {
        FileService.SaveFile(filePath, content);
    }

    /// <summary>
    /// 現在のファイルパスを設定する
    /// </summary>
    /// <param name="filePath">ファイルパス</param>
    public void SetFilePath(string? filePath)
    {
        if (_currentFilePath != filePath)
        {
            _currentFilePath = filePath;
            FilePathChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// 変更状態を設定する
    /// </summary>
    /// <param name="isModified">変更されている場合はtrue</param>
    public void SetModified(bool isModified)
    {
        if (_isModified != isModified)
        {
            _isModified = isModified;
            DocumentChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// 現在のドキュメントが新規ファイルで空白かどうかを判定する
    /// </summary>
    /// <param name="currentText">現在のテキスト内容</param>
    /// <returns>新規ファイルで空白の場合はtrue</returns>
    public bool IsNewAndEmpty(string currentText)
    {
        return !HasFilePath && !IsModified && string.IsNullOrEmpty(currentText);
    }

    /// <summary>
    /// ドキュメントの状態をリセットする（新規ファイル状態に戻す）
    /// </summary>
    public void Reset()
    {
        SetFilePath(null);
        SetModified(false);
    }
}
