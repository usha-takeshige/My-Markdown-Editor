using System;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace MyMarkdownEditor.Settings;

/// <summary>
/// ウィンドウの状態（サイズ、位置、最大化状態）を保存・復元するクラス
/// バージョン1.1: 初期読み込みのキャッシングでメモリ効率を改善
/// </summary>
public class WindowSettings
{
    public double Width { get; set; } = 900;
    public double Height { get; set; } = 600;
    public double Left { get; set; } = 100;
    public double Top { get; set; } = 100;
    public WindowState WindowState { get; set; } = WindowState.Normal;

    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MyMarkdownEditor"
    );
    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "window-settings.json");

    // 初期設定のキャッシュ（最初の読み込み時のみディスクアクセス）
    private static WindowSettings? _cachedInitialSettings;
    private static readonly object _cacheLock = new object();

    /// <summary>
    /// 設定ファイルから設定を読み込む（初回のみディスクから読み込み、以降はキャッシュを複製）
    /// </summary>
    /// <returns>WindowSettingsオブジェクト（ファイルが存在しない場合はデフォルト値）</returns>
    public static WindowSettings Load()
    {
        lock (_cacheLock)
        {
            // キャッシュがあれば複製を返す（各ウィンドウが独立して変更できるように）
            if (_cachedInitialSettings != null)
            {
                return Clone(_cachedInitialSettings);
            }

            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<WindowSettings>(json) ?? new WindowSettings();
                    _cachedInitialSettings = settings;
                    return Clone(settings);
                }
            }
            catch
            {
                // エラーの場合はデフォルト値を返す
            }

            var defaultSettings = new WindowSettings();
            _cachedInitialSettings = defaultSettings;
            return Clone(defaultSettings);
        }
    }

    /// <summary>
    /// キャッシュをクリアして次回読み込み時にファイルから再読み込みする
    /// </summary>
    public static void ClearCache()
    {
        lock (_cacheLock)
        {
            _cachedInitialSettings = null;
        }
    }

    /// <summary>
    /// WindowSettingsのクローンを作成する
    /// </summary>
    private static WindowSettings Clone(WindowSettings source)
    {
        return new WindowSettings
        {
            Width = source.Width,
            Height = source.Height,
            Left = source.Left,
            Top = source.Top,
            WindowState = source.WindowState
        };
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
        return new WindowSettings
        {
            Width = window.Width,
            Height = window.Height,
            Left = window.Left,
            Top = window.Top,
            WindowState = window.WindowState
        };
    }
}
