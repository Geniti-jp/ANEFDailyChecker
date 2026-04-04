using System.Windows;

namespace ANEFDailyChecker;

public partial class TimerNotificationWindow : Window
{
    public TimerNotificationWindow(string timerName, Window owner)
    {
        InitializeComponent();
        TimerNameText.Text = timerName;

        // メインウィンドウの位置に概ね重なるように配置
        try
        {
            double offsetX = (owner.ActualWidth - Width) / 2;
            double offsetY = (owner.ActualHeight - Height) / 2;
            Left = owner.Left + offsetX;
            Top = owner.Top + offsetY;
        }
        catch
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }

    private void OkClick(object sender, RoutedEventArgs e) => Close();
}
