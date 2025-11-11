using System;
using System.Windows.Input;

namespace MyMarkdownEditor.Commands;

/// <summary>
/// シンプルなICommandの実装
/// Action デリゲートをラップしてWPFのコマンドバインディングを可能にする
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    /// <summary>
    /// RelayCommandの新しいインスタンスを作成します
    /// </summary>
    /// <param name="execute">実行するアクション</param>
    /// <param name="canExecute">実行可能かどうかを判定する関数（オプション）</param>
    /// <exception cref="ArgumentNullException">executeがnullの場合</exception>
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>
    /// コマンドの実行可能状態が変更されたときに発生します
    /// </summary>
    public event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    /// <summary>
    /// コマンドが実行可能かどうかを判定します
    /// </summary>
    /// <param name="parameter">コマンドパラメータ（未使用）</param>
    /// <returns>実行可能な場合はtrue、それ以外はfalse</returns>
    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    /// <summary>
    /// コマンドを実行します
    /// </summary>
    /// <param name="parameter">コマンドパラメータ（未使用）</param>
    public void Execute(object? parameter) => _execute();
}
