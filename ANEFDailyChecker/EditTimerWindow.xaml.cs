using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using Microsoft.Win32;
using ANEFDailyChecker.Models;

namespace ANEFDailyChecker;

public partial class EditTimerWindow : Window
{
    private TimerConfig _timer;

    public EditTimerWindow(TimerConfig timer)
    {
        InitializeComponent();
        if (Application.Current.MainWindow is Window mw && mw != this && mw.Topmost) Topmost = true;

        _timer = timer;

        NameBox.Text = timer.Name;
        MinutesBox.Text = timer.FixedMinutes.ToString();
        RecoveryIntervalBox.Text = FormatSeconds(timer.RecoveryIntervalSeconds);
        MaxValueBox.Text = timer.RecoveryMaxValue.ToString();
        BuiltinSoundCheck.IsChecked = timer.UseBuiltinSound;
        SoundPathBox.Text = timer.SoundPath;
        RepeatCountBox.Text = timer.SoundRepeatCount.ToString();

        if (timer.IsFixedMode)
            FixedModeRadio.IsChecked = true;
        else if (timer.IsFullRecoveryMode)
            FullRecoveryModeRadio.IsChecked = true;
        else if (timer.IsRecoveryMode)
            RecoveryModeRadio.IsChecked = true;
        else
            InputModeRadio.IsChecked = true;

        RefreshVisibility();
    }

    private static string FormatSeconds(int totalSeconds)
    {
        int h = totalSeconds / 3600;
        int m = (totalSeconds % 3600) / 60;
        int s = totalSeconds % 60;
        if (h > 0) return $"{h}:{m:D2}:{s:D2}";
        return $"{m}:{s:D2}";
    }

    // 区切り文字正規化（TimerInputWindow と共通仕様）
    private static int? ParseTimeInput(string text)
    {
        text = text.Trim();
        // テンキー記号 / * - + および全角・各種区切りをコロンに正規化
        var normalized = Regex.Replace(text,
            @"[：:．.。、・,，/／*＊\-－+＋\s]+", ":");
        normalized = normalized.Trim(':');

        var parts = normalized.Split(':');
        return parts.Length switch
        {
            1 when int.TryParse(parts[0], out int sec) && sec >= 0 => sec,
            2 when int.TryParse(parts[0], out int m) &&
                   int.TryParse(parts[1], out int s) && s < 60 => m * 60 + s,
            3 when int.TryParse(parts[0], out int h) &&
                   int.TryParse(parts[1], out int m2) && m2 < 60 &&
                   int.TryParse(parts[2], out int s2) && s2 < 60 => h * 3600 + m2 * 60 + s2,
            _ => null
        };
    }

    private void ModeChanged(object sender, RoutedEventArgs e) => RefreshVisibility();
    private void SoundModeChanged(object sender, RoutedEventArgs e) => RefreshVisibility();

    private void RefreshVisibility()
    {
        bool isFixed = FixedModeRadio.IsChecked ?? false;
        bool isRecovery = RecoveryModeRadio.IsChecked ?? false;
        bool isFullRecovery = FullRecoveryModeRadio.IsChecked ?? false;
        bool useBuiltin = BuiltinSoundCheck.IsChecked ?? true;

        if (FixedTimePanel != null)
            FixedTimePanel.Visibility = isFixed ? Visibility.Visible : Visibility.Collapsed;
        if (RecoveryPanel != null)
            RecoveryPanel.Visibility = (isRecovery || isFullRecovery) ? Visibility.Visible : Visibility.Collapsed;
        if (MaxValuePanel != null)
            MaxValuePanel.Visibility = isFullRecovery ? Visibility.Visible : Visibility.Collapsed;
        if (CustomSoundPanel != null)
            CustomSoundPanel.Visibility = useBuiltin ? Visibility.Collapsed : Visibility.Visible;
    }

    private void BrowseSound(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "WAV ファイル (*.wav)|*.wav",
            Title = "音声ファイルを選択"
        };
        if (dlg.ShowDialog() == true)
            SoundPathBox.Text = dlg.FileName;
    }

    private void OkClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            MessageBox.Show("タイマー名を入力してください。", "入力エラー",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            NameBox.Focus();
            return;
        }

        bool isFixed = FixedModeRadio.IsChecked ?? false;
        bool isRecovery = RecoveryModeRadio.IsChecked ?? false;
        bool isFullRecovery = FullRecoveryModeRadio.IsChecked ?? false;

        if (isFixed)
        {
            if (!int.TryParse(MinutesBox.Text, out int mins) || mins < 1)
            {
                MessageBox.Show("時間は 1 以上の整数（分）で入力してください。", "入力エラー",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                MinutesBox.Focus(); MinutesBox.SelectAll();
                return;
            }
            _timer.FixedMinutes = mins;
        }

        if (isRecovery || isFullRecovery)
        {
            var sec = ParseTimeInput(RecoveryIntervalBox.Text);
            if (sec == null || sec < 1)
            {
                MessageBox.Show(
                    "1回復あたりの時間を正しく入力してください。\n" +
                    "例: 7:10（7分10秒）/ 430（秒数）/ 1:07:10（1時間7分10秒）",
                    "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                RecoveryIntervalBox.Focus(); RecoveryIntervalBox.SelectAll();
                return;
            }
            _timer.RecoveryIntervalSeconds = sec.Value;
        }

        if (isFullRecovery)
        {
            if (!int.TryParse(MaxValueBox.Text, out int maxVal) || maxVal < 1)
            {
                MessageBox.Show("最大値は 1 以上の整数で入力してください。", "入力エラー",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                MaxValueBox.Focus(); MaxValueBox.SelectAll();
                return;
            }
            _timer.RecoveryMaxValue = maxVal;
        }

        bool useBuiltin = BuiltinSoundCheck.IsChecked ?? true;
        if (!useBuiltin)
        {
            string path = SoundPathBox.Text.Trim();
            if (!File.Exists(path))
            {
                MessageBox.Show("指定した音声ファイルが見つかりません。\nパスを確認してください。",
                    "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _timer.SoundPath = path;
        }

        if (!int.TryParse(RepeatCountBox.Text, out int repeat) || repeat < 1)
        {
            MessageBox.Show("音声繰り返し回数は 1 以上の整数で入力してください。", "入力エラー",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            RepeatCountBox.Focus(); RepeatCountBox.SelectAll();
            return;
        }

        _timer.Name = NameBox.Text.Trim();
        _timer.IsFixedMode = isFixed;
        _timer.IsRecoveryMode = isRecovery;
        _timer.IsFullRecoveryMode = isFullRecovery;
        _timer.UseBuiltinSound = useBuiltin;
        _timer.SoundRepeatCount = repeat;

        DialogResult = true;
    }
}
