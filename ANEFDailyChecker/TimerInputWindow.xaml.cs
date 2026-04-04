using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace ANEFDailyChecker;

public partial class TimerInputWindow : Window
{
    /// <summary>入力された合計分数（OK 時に設定）</summary>
    public int InputMinutes { get; private set; }

    public TimerInputWindow(string timerName)
    {
        InitializeComponent();
        TitleLabel.Text = timerName;
        TimeBox.Focus();
    }

    private void TimeBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return)
        {
            TryCommit();
            e.Handled = true;
        }
    }

    private void OkClick(object sender, RoutedEventArgs e) => TryCommit();

    private void TryCommit()
    {
        var text = TimeBox.Text.Trim();

        // HH:MM または MM 形式を受け付ける
        if (Regex.IsMatch(text, @"^\d{1,2}:\d{2}$"))
        {
            var parts = text.Split(':');
            int h = int.Parse(parts[0]);
            int m = int.Parse(parts[1]);
            if (m > 59 || (h == 0 && m == 0))
            {
                ShowError();
                return;
            }
            InputMinutes = h * 60 + m;
            DialogResult = true;
        }
        else if (Regex.IsMatch(text, @"^\d{1,4}$"))
        {
            int m = int.Parse(text);
            if (m < 1)
            {
                ShowError();
                return;
            }
            InputMinutes = m;
            DialogResult = true;
        }
        else
        {
            ShowError();
        }
    }

    private void ShowError()
    {
        MessageBox.Show(
            "HH:MM 形式（例: 01:30）または分数（例: 90）で入力してください。",
            "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
        TimeBox.Focus();
        TimeBox.SelectAll();
    }
}
