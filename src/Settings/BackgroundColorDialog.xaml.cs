using System.Windows;
using System.Windows.Media;

namespace MyMarkdownEditor.Settings;

/// <summary>
/// BackgroundColorDialog.xaml の相互作用ロジック
/// </summary>
public partial class BackgroundColorDialog : Window
{
    public string SelectedColor { get; private set; }

    public BackgroundColorDialog(string currentColor)
    {
        InitializeComponent();
        SelectedColor = currentColor;
        ColorTextBox.Text = currentColor;
        UpdatePreview(currentColor);
    }

    private void ColorTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        UpdatePreview(ColorTextBox.Text);
    }

    private void UpdatePreview(string hexColor)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hexColor);
            PreviewBorder.Background = new SolidColorBrush(color);
        }
        catch
        {
            // 無効な色の場合はプレビューを更新しない
            PreviewBorder.Background = new SolidColorBrush(Colors.White);
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // 色が有効か検証
            var color = (Color)ColorConverter.ConvertFromString(ColorTextBox.Text);
            SelectedColor = ColorTextBox.Text;
            DialogResult = true;
        }
        catch
        {
            MessageBox.Show("無効な色の形式です。16進数で入力してください（例: #FFFFFF）",
                "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
