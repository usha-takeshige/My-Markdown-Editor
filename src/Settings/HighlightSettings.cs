using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyMarkdownEditor.Settings;

/// <summary>
/// シンタックスハイライトの色・スタイル設定を管理するクラス
/// バージョン1.1: シングルトンパターンでメモリ効率を改善
/// </summary>
public class HighlightSettings
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MyMarkdownEditor"
    );
    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "highlight-settings.json");

    // シングルトンインスタンス
    private static HighlightSettings? _instance;
    private static readonly object _instanceLock = new object();

    /// <summary>
    /// 見出しのスタイル定義
    /// </summary>
    public ColorDefinition Heading { get; set; } = new();

    /// <summary>
    /// 太字のスタイル定義
    /// </summary>
    public ColorDefinition Bold { get; set; } = new();

    /// <summary>
    /// 斜体のスタイル定義
    /// </summary>
    public ColorDefinition Italic { get; set; } = new();

    /// <summary>
    /// コードブロックのスタイル定義
    /// </summary>
    public ColorDefinition Code { get; set; } = new();

    /// <summary>
    /// インラインコードのスタイル定義
    /// </summary>
    public ColorDefinition InlineCode { get; set; } = new();

    /// <summary>
    /// 設定ファイルから設定を読み込む（キャッシュ対応）
    /// </summary>
    /// <returns>HighlightSettingsオブジェクト（ファイルが存在しない場合はデフォルト値）</returns>
    public static HighlightSettings Load()
    {
        lock (_instanceLock)
        {
            // キャッシュされたインスタンスがあればそれを返す
            if (_instance != null)
            {
                return _instance;
            }

            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<HighlightSettings>(json);
                    if (settings != null)
                    {
                        _instance = settings;
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load highlight settings: {ex.Message}");
            }

            // ファイルが存在しない場合や読み込みエラー時はデフォルト設定を生成して保存
            var defaultSettings = GetDefault();
            try
            {
                defaultSettings.Save();
            }
            catch
            {
                // 保存に失敗してもデフォルト設定を返す
            }
            _instance = defaultSettings;
            return defaultSettings;
        }
    }

    /// <summary>
    /// キャッシュをクリアして次回読み込み時にファイルから再読み込みする
    /// </summary>
    public static void ClearCache()
    {
        lock (_instanceLock)
        {
            _instance = null;
        }
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

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save highlight settings: {ex.Message}");
            // エラーは無視（設定の保存に失敗してもアプリは動作する）
        }
    }

    /// <summary>
    /// デフォルト設定を取得する
    /// </summary>
    /// <returns>デフォルト設定のHighlightSettingsオブジェクト</returns>
    public static HighlightSettings GetDefault()
    {
        return new HighlightSettings
        {
            Heading = new ColorDefinition
            {
                Foreground = "#0066CC",
                FontWeight = "bold"
            },
            Bold = new ColorDefinition
            {
                FontWeight = "bold"
            },
            Italic = new ColorDefinition
            {
                FontStyle = "italic"
            },
            Code = new ColorDefinition
            {
                Foreground = "#D14",
                Background = "#F5F5F5",
                FontFamily = "Consolas"
            },
            InlineCode = new ColorDefinition
            {
                Foreground = "#D14",
                Background = "#F5F5F5",
                FontFamily = "Consolas"
            }
        };
    }

    /// <summary>
    /// ハッシュコードを取得する（キャッシング用）
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(
            Heading.GetHashCode(),
            Bold.GetHashCode(),
            Italic.GetHashCode(),
            Code.GetHashCode(),
            InlineCode.GetHashCode()
        );
    }

    /// <summary>
    /// 等価性を判定する
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is not HighlightSettings other)
            return false;

        return Heading.Equals(other.Heading) &&
               Bold.Equals(other.Bold) &&
               Italic.Equals(other.Italic) &&
               Code.Equals(other.Code) &&
               InlineCode.Equals(other.InlineCode);
    }
}

/// <summary>
/// 個々の要素の色・スタイル定義
/// </summary>
public class ColorDefinition
{
    /// <summary>
    /// 前景色（テキスト色） - #RRGGBB または #RGB 形式
    /// </summary>
    public string? Foreground { get; set; }

    /// <summary>
    /// 背景色 - #RRGGBB または #RGB 形式
    /// </summary>
    public string? Background { get; set; }

    /// <summary>
    /// フォントウェイト - "normal" または "bold"
    /// </summary>
    public string? FontWeight { get; set; }

    /// <summary>
    /// フォントスタイル - "normal" または "italic"
    /// </summary>
    public string? FontStyle { get; set; }

    /// <summary>
    /// フォントファミリー - フォント名（例: "Consolas", "Meiryo UI"）
    /// </summary>
    public string? FontFamily { get; set; }

    /// <summary>
    /// ハッシュコードを取得する（キャッシング用）
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(Foreground, Background, FontWeight, FontStyle, FontFamily);
    }

    /// <summary>
    /// 等価性を判定する
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is not ColorDefinition other)
            return false;

        return Foreground == other.Foreground &&
               Background == other.Background &&
               FontWeight == other.FontWeight &&
               FontStyle == other.FontStyle &&
               FontFamily == other.FontFamily;
    }
}
