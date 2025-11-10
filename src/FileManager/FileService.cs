using System.IO;
using System.Text;

namespace MyMarkdownEditor.FileManager;

/// <summary>
/// ファイルの読み込み・保存を担当するサービスクラス
/// UTF-8(BOMなし)でのファイル操作を行う
/// </summary>
public static class FileService
{
    /// <summary>
    /// UTF-8(BOMなし)でファイルを読み込む
    /// </summary>
    /// <param name="filePath">読み込むファイルのパス</param>
    /// <returns>ファイルの内容</returns>
    /// <exception cref="IOException">ファイル読み込みに失敗した場合</exception>
    public static string ReadFile(string filePath)
    {
        try
        {
            return File.ReadAllText(filePath, new UTF8Encoding(false));
        }
        catch (Exception ex)
        {
            throw new IOException($"ファイルの読み込みに失敗しました: {filePath}", ex);
        }
    }

    /// <summary>
    /// UTF-8(BOMなし)でファイルを保存する
    /// </summary>
    /// <param name="filePath">保存先のファイルパス</param>
    /// <param name="content">保存する内容</param>
    /// <exception cref="IOException">ファイル保存に失敗した場合</exception>
    public static void SaveFile(string filePath, string content)
    {
        try
        {
            File.WriteAllText(filePath, content, new UTF8Encoding(false));
        }
        catch (Exception ex)
        {
            throw new IOException($"ファイルの保存に失敗しました: {filePath}", ex);
        }
    }
}
