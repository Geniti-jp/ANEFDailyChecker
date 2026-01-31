using System.Windows;
using System.Windows.Threading;
using ANEFDailyChecker.Services;

namespace ANEFDailyChecker;

public partial class MainWindow : Window
{
    private AppState _state;
    private DispatcherTimer _timer = new();

    // UI が最後に認識した更新基準時刻
    private DateTime _lastKnownUpdateTime;

    public MainWindow()
    {
        InitializeComponent();

        _state = AppStateService.Load();
        MemoList.ItemsSource = _state.Memos;

        _lastKnownUpdateTime = GetCurrentUpdateTime(DateTime.Now);

        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += TimerTick;
        _timer.Start();

        TimerTick(null, EventArgs.Empty);
    }

    /// <summary>
    /// 再読込ボタン：時刻を跨いでいればリセット、そうでなければ何もしない
    /// </summary>
    private void ReloadState(object sender, RoutedEventArgs e)
    {
        var now = DateTime.Now;
        var currentUpdateTime = GetCurrentUpdateTime(now);

        if (_lastKnownUpdateTime != currentUpdateTime)
        {
            ResetAllChecks();
            _lastKnownUpdateTime = currentUpdateTime;
        }

        TimerTick(null, EventArgs.Empty);
    }

    private void TimerTick(object? sender, EventArgs e)
    {
        var now = DateTime.Now;
        var currentUpdateTime = GetCurrentUpdateTime(now);

        var next = currentUpdateTime.AddDays(1);
        var remain = next - now;

        RemainingText.Text =
            $"次回更新まで {remain.Hours}時間{remain.Minutes}分（{_state.ResetTime:hh\\:mm} 更新）";

        // 自動判定（放置時）
        if (_lastKnownUpdateTime != currentUpdateTime)
        {
            ResetAllChecks();
            _lastKnownUpdateTime = currentUpdateTime;
        }
    }

    /// <summary>
    /// 現在時刻に対する「直近の更新基準時刻」を返す
    /// </summary>
    private DateTime GetCurrentUpdateTime(DateTime now)
    {
        var updateTime = now.Date + _state.ResetTime;
        if (now < updateTime)
            updateTime = updateTime.AddDays(-1);

        return updateTime;
    }

    private void ResetAllChecks()
    {
        foreach (var m in _state.Memos)
            m.IsChecked = false;

        MemoList.Items.Refresh();
        AppStateService.Save(_state);
    }

    private void OpenSettings(object sender, RoutedEventArgs e)
    {
        new SettingsWindow(_state).ShowDialog();
        MemoList.Items.Refresh();
        AppStateService.Save(_state);
    }

    protected override void OnClosed(EventArgs e)
    {
        AppStateService.Save(_state);
        base.OnClosed(e);
    }
}
