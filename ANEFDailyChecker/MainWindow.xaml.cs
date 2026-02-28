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
            parent?.UpdateStatusFromChildren();
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
            ResetAllChecks();
            _lastKnownUpdateTime = currentUpdateTime;
        }
    }

    private void UpdateRemainingDisplay()
    {
        var now = DateTime.Now;
        var currentUpdateTime = GetCurrentUpdateTime(now);
        var next = currentUpdateTime.AddDays(1);
        var remain = next - now;

        RemainingText.Text = $"次回更新まで{remain.Hours}時間{remain.Minutes}分({_state.ResetTime:hh\\:mm}後更新)";
    }

    private DateTime GetCurrentUpdateTime(DateTime now)
    {
        var updateTime = now.Date + _state.ResetTime;
        if (now < updateTime) updateTime = updateTime.AddDays(-1);
        return updateTime;
    }

    private void ResetAllChecks()
    {
        foreach (var m in _state.Memos)
        {
            m.IsItemChecked = false;
            foreach (var child in m.Children) child.IsItemChecked = false;
            m.UpdateStatusFromChildren();
        }
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