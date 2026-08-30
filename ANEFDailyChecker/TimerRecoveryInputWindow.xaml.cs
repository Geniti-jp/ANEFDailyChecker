using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace ANEFDailyChecker;

public partial class TimerRecoveryInputWindow : Window
{
    private readonly int _maxValue;

    /// <summary>入力された現在値（OK 時に設定）</summary>
    public int CurrentValue { get; private set; }

    /// <summary>次の1ポイント回復までの残り秒数（OK 時に設定）</summary>
    public int RemainingSecondsToNextPoint { get; private set; }

    public TimerRecoveryInputWindow(string timerName, int maxValue)
    {
        InitializeComponent();
        _maxValue = maxValue;
        TitleLabel.Text = timerName;
        MaxLabel.Text = $"最大値: {maxValue}";
        CurrentValueBox.Focus();
    }

    private void RemainingBox_KeyDown(object sender, KeyEventArgs e)
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
        if (!int.TryParse(CurrentValueBox.Text.Trim(), out int current) || current < 0)
        {
            ShowError("現在値は 0 以上の整数で入力してください。");
            CurrentValueBox.Focus(); CurrentValueBox.SelectAll();
            return;
        }
        if (current >= _maxValue)
        {
            ShowError("現在値は最大値未満で入力してください。");
            CurrentValueBox.Focus(); CurrentValueBox.SelectAll();
            return;
        }

        var text = RemainingBox.Text.Trim();
        if (!Regex.IsMatch(text, @"^\d{1,4}:\d{2}$"))
        {
            ShowError("残り時間は MM:SS 形式（例: 03:45）で入力してください。");
            RemainingBox.Focus(); RemainingBox.SelectAll();
            return;
        }
        var parts = text.Split(':');
        int m = int.Parse(parts[0]);
        int s = int.Parse(parts[1]);
        if (s > 59)
        {
            ShowError("秒は 00〜59 の範囲で入力してください。");
            RemainingBox.Focus(); RemainingBox.SelectAll();
            return;
        }

        CurrentValue = current;
        RemainingSecondsToNextPoint = m * 60 + s;
        DialogResult = true;
    }

    private void ShowError(string message) =>
        MessageBox.Show(message, "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
}
