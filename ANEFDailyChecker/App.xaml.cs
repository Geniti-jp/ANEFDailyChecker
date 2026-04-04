using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ANEFDailyChecker;

public partial class App : Application
{
    /// <summary>
    /// 起動時に exe と同階層の .ico ファイルを探し、見つかれば
    /// MainWindow のアイコン（タイトルバー・タスクバー）に適用する。
    /// 複数ある場合は最初に見つかったものを使用する。
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var icoFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.ico");
            if (icoFiles.Length > 0)
            {
                var icon = new BitmapImage(new Uri(icoFiles[0], UriKind.Absolute));
                // MainWindow は StartupUri で生成されるため、起動後に適用
                Dispatcher.InvokeAsync(() =>
                {
                    if (MainWindow != null)
                        MainWindow.Icon = icon;
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"アイコン読み込みエラー: {ex.Message}");
        }
    }
}
