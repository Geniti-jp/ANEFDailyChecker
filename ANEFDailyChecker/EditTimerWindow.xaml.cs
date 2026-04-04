using System.IO;
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
        _timer = timer;

        NameBox.Text = timer.Name;
        MinutesBox.Text = timer.FixedMinutes.ToString();
        BuiltinSoundCheck.IsChecked = timer.UseBuiltinSound;
        SoundPathBox.Text = timer.SoundPath;
        RepeatCountBox.Text = timer.SoundRepeatCount.ToString();

        if (timer.IsFixedMode)
            FixedModeRadio.IsChecked = true;
        else
            InputModeRadio.IsChecked = true;

        RefreshVisibility();
    }

    private void ModeChanged(object sender, RoutedEventArgs e) => RefreshVisibility();
    private void SoundModeChanged(object sender, RoutedEventArgs e) => RefreshVisibility();

    private void RefreshVisibility()
    {
        bool isFixed = FixedModeRadio.IsChecked ?? true;
        bool useBuiltin = BuiltinSoundCheck.IsChecked ?? true;

        if (FixedTimePanel != null)
            FixedTimePanel.Visibility = isFixed ? Visibility.Visible : Visibility.Collapsed;
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
        // 名前
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            MessageBox.Show("タイマー名を入力してください。", "入力エラー",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            NameBox.Focus();
            return;
        }

        // 固定モード時の分数
        bool isFixed = FixedModeRadio.IsChecked ?? true;
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

        // カスタム音声
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

        // 繰り返し回数
        if (!int.TryParse(RepeatCountBox.Text, out int repeat) || repeat < 1)
        {
            MessageBox.Show("音声繰り返し回数は 1 以上の整数で入力してください。", "入力エラー",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            RepeatCountBox.Focus(); RepeatCountBox.SelectAll();
            return;
        }

        _timer.Name = NameBox.Text.Trim();
        _timer.IsFixedMode = isFixed;
        _timer.UseBuiltinSound = useBuiltin;
        _timer.SoundRepeatCount = repeat;

        DialogResult = true;
    }
}
