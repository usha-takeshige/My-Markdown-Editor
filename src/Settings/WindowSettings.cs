using System.IO;
using System.Text.Json;
using System.Windows;

namespace MyMarkdownEditor.Settings;

/// <summary>
/// ウィンドウの状態（サイズ、位置、最大化状態）を保存・復元するクラス
/// </summary>
public class WindowSettings
{
    public double Width { get; set; } = 900;
    public double Height { get; set; } = 600;
    public double Left { get; set; } = 100;
    public double Top { get; set; } = 100;
    public WindowState WindowState { get; set; } = WindowState.Normal;
    public string BackgroundColor { get; set; } = "#FFFFFF"; // デフォルトは白

    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MyMarkdownEditor"
    );
    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "window-settings.json");

    /// <summary>
    /// 設定ファイルから設定を読み込む
    /// </summary>
    /// <returns>WindowSettingsオブジェクト（ファイルが存在しない場合はデフォルト値）</returns>
    public static WindowSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                return JsonSerializer.Deserialize<WindowSettings>(json) ?? new WindowSettings();
            }
        }
        catch
        {
            // エラーの場合はデフォルト値を返す
        }
        return new WindowSettings();
    }

    /// <summary>
    /// 現在の設定を保存する
    /// </summary>
    public void Save()
    {
        try
        {
            // ディレクトリが存在しない場合は作成
            if (!Directory.Exists(SettingsDirectory))
            {
                Directory.CreateDirectory(SettingsDirectory);
            }

            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);
        }
        catch
        {
            // エラーは無視（設定の保存に失敗してもアプリは動作する）
        }
    }

    /// <summary>
    /// Windowオブジェクトに設定を適用する
    /// </summary>
    /// <param name="window">適用先のウィンドウ</param>
    public void ApplyToWindow(Window window)
    {
        window.Width = Width;
        window.Height = Height;
        window.Left = Left;
        window.Top = Top;
        window.WindowState = WindowState;
    }

    /// <summary>
    /// Windowオブジェクトから現在の状態を取得する
    /// </summary>
    /// <param name="window">取得元のウィンドウ</param>
    /// <returns>WindowSettingsオブジェクト</returns>
    public static WindowSettings FromWindow(Window window)
    {
        // 既存の設定を読み込んでBackgroundColorを保持
        var existingSettings = Load();

        return new WindowSettings
        {
            Width = window.Width,
            Height = window.Height,
            Left = window.Left,
            Top = window.Top,
            WindowState = window.WindowState,
            BackgroundColor = existingSettings.BackgroundColor
        };
    }
}
