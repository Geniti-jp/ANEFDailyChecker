using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace ANEFDailyChecker;

public enum TimerInputMode { Input, Recovery }

public partial class TimerInputWindow : Window
{
    private readonly TimerInputMode _mode;
    private readonly int _recoveryIntervalSeconds;

    /// <summary>入力された合計秒数（OK 時に設定）</summary>
    public int InputSeconds { get; private set; }

    /// <summary>互換用：分数（秒を60で割り切り捨て）</summary>
    public int InputMinutes => InputSeconds / 60;

    public TimerInputWindow(string timerName,
        TimerInputMode timerMode = TimerInputMode.Input,
        int recoveryIntervalSeconds = 0)
    {
        InitializeComponent();
        _mode = timerMode;
        _recoveryIntervalSeconds = recoveryIntervalSeconds;

        TitleLabel.Text = timerName;

        if (_mode == TimerInputMode.Recovery)
        {
            // 回復モード用UIを表示、通常UIを隠す
            NormalLabel.Visibility = Visibility.Collapsed;
            TimeBox.Visibility = Visibility.Collapsed;
            RecoverySpinPanel.Visibility = Visibility.Visible;
            RecoveryTimePanel.Visibility = Visibility.Visible;
            TotalLabel.Visibility = Visibility.Visible;

            // 1回復の時間をラベル表示
            IntervalLabel.Text = $"× {FormatSeconds(_recoveryIntervalSeconds)}/回";

            UpdateTotalLabel();
            RecoveryCountBox.Focus();
        }
        else
        {
            TimeBox.Focus();
        }
    }

    private static string FormatSeconds(int totalSec)
    {
        int h = totalSec / 3600, m = (totalSec % 3600) / 60, s = totalSec % 60;
        if (h > 0) return $"{h}時間{m:D2}分{s:D2}秒";
        if (s > 0) return $"{m}分{s:D2}秒";
        return $"{m}分";
    }

    // ─── 区切り文字正規化（HH:MM / MM:SS / 秒/分） ───────────────────────
    // 区切り文字: : ： . ． 。 、 ・ , ， / ／ * ＊ - － + ＋ スペース
    private static int? ParseTimeInput(string text, bool allowSeconds = false)
    {
        text = text.Trim();
        var normalized = Regex.Replace(text,
            @"[：:．.。、・,，/／*＊\-－+＋\s]+", ":");
        normalized = normalized.Trim(':');

        var parts = normalized.Split(':');
        return parts.Length switch
        {
            1 when int.TryParse(parts[0], out int n) && n >= 0
                => allowSeconds ? n : n * 60,  // 秒モードは秒、分モードは分→秒
            2 when int.TryParse(parts[0], out int a) &&
                   int.TryParse(parts[1], out int b) && b < 60
                => allowSeconds ? a * 60 + b : a * 3600 + b * 60,
            3 when int.TryParse(parts[0], out int h) &&
                   int.TryParse(parts[1], out int m) && m < 60 &&
                   int.TryParse(parts[2], out int s) && s < 60
                => h * 3600 + m * 60 + s,
            _ => null
        };
    }

    // ─── スピンボックス ──────────────────────────────────────────────────

    private void SpinDown(object sender, RoutedEventArgs e)
    {
        int v = GetSpinValue();
        if (v > 0) { RecoveryCountBox.Text = (v - 1).ToString(); RecoveryCountBox.SelectAll(); }
    }

    private void SpinUp(object sender, RoutedEventArgs e)
    {
        int v = GetSpinValue();
        RecoveryCountBox.Text = (v + 1).ToString();
        RecoveryCountBox.SelectAll();
    }

    private int GetSpinValue()
        => int.TryParse(RecoveryCountBox.Text, out int v) && v >= 0 ? v : 0;

    private void RecoveryCountBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
    }

    private void RecoveryCountBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        => UpdateTotalLabel();

    // ホイールスクロールで増減（Word/Excel/PowerPoint 仕様）
    private void RecoveryCountBox_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Delta > 0) SpinUp(sender, new RoutedEventArgs());
        else SpinDown(sender, new RoutedEventArgs());
        e.Handled = true;
    }

    // フォーカス離脱時: 空欄・0 以下なら 1 に補正（Word/Excel/PowerPoint 仕様）
    private void RecoveryCountBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(RecoveryCountBox.Text, out int v) || v < 0)
            RecoveryCountBox.Text = "0";
        UpdateTotalLabel();
    }

    private void UpdateTotalLabel()
    {
        // InitializeComponent 中に TextChanged が発火する場合、コントロールがまだ null
        if (TotalLabel == null || RecoveryTimeBox == null) return;

        int count = GetSpinValue();
        var parsed = ParseTimeInput(RecoveryTimeBox.Text ?? "", allowSeconds: true);
        int addSec = parsed ?? 0;
        int total = count * _recoveryIntervalSeconds + addSec;
        TotalLabel.Text = $"合計: {count}回 × {FormatSeconds(_recoveryIntervalSeconds)} + {FormatSeconds(addSec)} = {FormatSeconds(total)}";
    }

    // ─── キー・ボタン ────────────────────────────────────────────────────

    private void TimeBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return) { TryCommit(); e.Handled = true; }
    }

    private void OkClick(object sender, RoutedEventArgs e) => TryCommit();

    private void TryCommit()
    {
        if (_mode == TimerInputMode.Recovery)
        {
            TryCommitRecovery();
            return;
        }

        // 通常モード（都度入力）: HH:MM 形式→分換算、または分数
        var text = TimeBox.Text.Trim();
        var sec = ParseTimeInput(text, allowSeconds: false);

        if (sec == null || sec < 60) // 最低1分
        {
            ShowError("HH:MM 形式（例: 01:30）または分数（例: 90）で入力してください。\n" +
                      "区切り文字として使用できる文字: : ： . ． 。 、 ・ , ， / ／ * ＊ - － + ＋ スペース");
            TimeBox.Focus(); TimeBox.SelectAll();
            return;
        }

        InputSeconds = sec.Value;
        DialogResult = true;
    }

    private void TryCommitRecovery()
    {
        int count = GetSpinValue();

        // 追加時間を解析（空欄は 0 秒扱い）
        int addSec = 0;
        string addText = RecoveryTimeBox.Text.Trim();
        if (!string.IsNullOrEmpty(addText))
        {
            var parsed = ParseTimeInput(addText, allowSeconds: true);
            if (parsed == null)
            {
                ShowError("追加時間の形式が正しくありません。\n例: 1:30（1分30秒）/ 90（秒数）\n区切り文字として使用できる文字: : ： . ． 。 、 ・ , ， / ／ * ＊ - － + ＋ スペース");
                RecoveryTimeBox.Focus(); RecoveryTimeBox.SelectAll();
                return;
            }
            addSec = parsed.Value;
        }

        // 両方 0 のときのみエラー
        if (count == 0 && addSec == 0)
        {
            ShowError("回復数と追加時間がどちらも 0 です。\n少なくとも一方に 1 以上の値を入力してください。");
            RecoveryCountBox.Focus(); RecoveryCountBox.SelectAll();
            return;
        }

        int total = count * _recoveryIntervalSeconds + addSec;
        if (total < 1)
        {
            ShowError("合計時間が 0 秒になっています。回復数または追加時間を確認してください。");
            return;
        }

        InputSeconds = total;
        DialogResult = true;
    }

    private void ShowError(string message)
    {
        MessageBox.Show(message, "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
