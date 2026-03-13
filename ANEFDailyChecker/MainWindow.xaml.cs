using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ANEFDailyChecker.Services;
using ANEFDailyChecker.Models;

namespace ANEFDailyChecker;

public partial class MainWindow : Window
{
    private AppState _state;
    private DispatcherTimer _timer = new();
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
        UpdateRemainingDisplay();
    }

    private void ReloadClick(object sender, RoutedEventArgs e)
    {
        _state = AppStateService.Load();
        MemoList.ItemsSource = _state.Memos;
        UpdateRemainingDisplay();
    }

    private void ParentCheckChanged(object sender, RoutedEventArgs e)
    {
        // 単体項目がチェックされたら RemainingCount をリセット
        if (sender is CheckBox cb && cb.DataContext is MemoItem item && !item.IsGroup)
        {
            if (item.IsItemChecked)
                item.RemainingCount = item.ResetCount;
        }
        AppStateService.Save(_state);
    }

    private void ParentCheckPreview(object sender, MouseButtonEventArgs e)
    {
        // グループ項目の親チェックボックスは、子要素の状態によってのみ変化させる
        if (sender is CheckBox cb && cb.DataContext is MemoItem item && item.IsGroup)
        {
            e.Handled = true;
        }
    }

    private void ChildCheckChanged(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox cb)
        {
            var parent = FindParentMemoItem(cb);
            if (parent != null)
            {
                parent.UpdateStatusFromChildren();

                // 全子がチェックされて親が完了状態になったときのみカウントダウンを開始
                if (parent.IsChecked)
                    parent.RemainingCount = parent.ResetCount;
            }
            AppStateService.Save(_state);
        }
    }

    private MemoItem? FindParentMemoItem(DependencyObject child)
    {
        DependencyObject parentDep = VisualTreeHelper.GetParent(child);
        while (parentDep != null && !(parentDep is ListBoxItem))
        {
            parentDep = VisualTreeHelper.GetParent(parentDep);
        }
        return (parentDep as ListBoxItem)?.Content as MemoItem;
    }

    private void TimerTick(object? sender, EventArgs e)
    {
        var now = DateTime.Now;
        var currentUpdateTime = GetCurrentUpdateTime(now);

        UpdateRemainingDisplay();

        if (_lastKnownUpdateTime != currentUpdateTime)
        {
            ProcessReset();
            _lastKnownUpdateTime = currentUpdateTime;
        }
    }

    private void UpdateRemainingDisplay()
    {
        var now = DateTime.Now;
        var currentUpdateTime = GetCurrentUpdateTime(now);
        var next = currentUpdateTime.AddDays(1);
        var remain = next - now;

        RemainingText.Text = $"次回更新まで{remain.Hours}時間{remain.Minutes}分({_state.ResetTime:hh\\:mm}更新)";
    }

    private DateTime GetCurrentUpdateTime(DateTime now)
    {
        var updateTime = now.Date + _state.ResetTime;
        if (now < updateTime) updateTime = updateTime.AddDays(-1);
        return updateTime;
    }

    /// <summary>
    /// 時刻を跨いだ際に各親項目の RemainingCount を 1 減算し、
    /// 0 になった項目のチェックをリセットする。
    /// RemainingCount がすでに 0 の項目（リセット済み・未チェック状態）はスキップ。
    /// </summary>
    private void ProcessReset()
    {
        foreach (var m in _state.Memos)
        {
            bool hasAnyCheck = m.IsGroup
                ? m.Children.Any(c => c.IsChecked)
                : m.IsItemChecked;

            if (m.RemainingCount <= 0)
            {
                // カウントダウン未開始（全チェック前）だが一部チェックがある場合はリセットのみ実行
                if (hasAnyCheck)
                {
                    m.IsItemChecked = false;
                    foreach (var child in m.Children) child.IsItemChecked = false;
                    m.UpdateStatusFromChildren();
                }
                continue;
            }

            m.RemainingCount--;

            if (m.RemainingCount == 0)
            {
                m.IsItemChecked = false;
                foreach (var child in m.Children) child.IsItemChecked = false;
                m.UpdateStatusFromChildren();
            }
        }
        AppStateService.Save(_state);
    }

    private void OpenSettings(object sender, RoutedEventArgs e)
    {
        new SettingsWindow(_state).ShowDialog();
        // ResetTime が変わっても TimerTick が誤検知しないよう基準時刻を再計算する
        _lastKnownUpdateTime = GetCurrentUpdateTime(DateTime.Now);
        UpdateRemainingDisplay();
        AppStateService.Save(_state);
    }

    protected override void OnClosed(EventArgs e)
    {
        AppStateService.Save(_state);
        base.OnClosed(e);
    }
}
