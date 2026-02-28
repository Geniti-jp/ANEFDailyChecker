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
        TimerTick(null, EventArgs.Empty);
    }

    private void ParentCheckPreview(object sender, MouseButtonEventArgs e)
    {
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

        RemainingText.Text = $"次回更新まで {remain.Hours}時間{remain.Minutes}分（{_state.ResetTime:hh\\:mm} 更新）";

        if (_lastKnownUpdateTime != currentUpdateTime)
        {
            ResetAllChecks();
            _lastKnownUpdateTime = currentUpdateTime;
        }
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
            m.IsChecked = false;
            foreach (var child in m.Children) child.IsChecked = false;
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