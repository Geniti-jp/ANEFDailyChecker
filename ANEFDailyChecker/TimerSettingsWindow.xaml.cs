using System.Windows;
using ANEFDailyChecker.Models;
using ANEFDailyChecker.Services;

namespace ANEFDailyChecker;

public partial class TimerSettingsWindow : Window
{
    private AppState _state;

    public TimerSettingsWindow(AppState state)
    {
        InitializeComponent();
        _state = state;
        TimerList.ItemsSource = _state.Timers;
    }

    private void AddTimer(object sender, RoutedEventArgs e)
    {
        var newTimer = new TimerConfig();
        var editWin = new EditTimerWindow(newTimer) { Owner = this };
        if (editWin.ShowDialog() == true)
        {
            _state.Timers.Add(newTimer);
            AppStateService.Save(_state);
        }
    }

    private void EditTimer(object sender, RoutedEventArgs e)
    {
        if (TimerList.SelectedItem is TimerConfig tc)
        {
            var editWin = new EditTimerWindow(tc) { Owner = this };
            editWin.ShowDialog();
            // EditTimerWindow は DialogResult=true で保存済み
            AppStateService.Save(_state);
        }
    }

    private void DeleteTimer(object sender, RoutedEventArgs e)
    {
        if (TimerList.SelectedItem is TimerConfig tc)
        {
            if (MessageBox.Show($"「{tc.Name}」を削除しますか？", "確認",
                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _state.Timers.Remove(tc);
                AppStateService.Save(_state);
            }
        }
    }

    private void MoveUp(object sender, RoutedEventArgs e)
    {
        int i = TimerList.SelectedIndex;
        if (i > 0)
        {
            var item = _state.Timers[i];
            _state.Timers.RemoveAt(i);
            _state.Timers.Insert(i - 1, item);
            TimerList.SelectedIndex = i - 1;
            AppStateService.Save(_state);
        }
    }

    private void MoveDown(object sender, RoutedEventArgs e)
    {
        int i = TimerList.SelectedIndex;
        if (i >= 0 && i < _state.Timers.Count - 1)
        {
            var item = _state.Timers[i];
            _state.Timers.RemoveAt(i);
            _state.Timers.Insert(i + 1, item);
            TimerList.SelectedIndex = i + 1;
            AppStateService.Save(_state);
        }
    }

    private void CloseClick(object sender, RoutedEventArgs e) => Close();
}
