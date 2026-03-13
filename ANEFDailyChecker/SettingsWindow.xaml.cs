using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using ANEFDailyChecker.Models;
using ANEFDailyChecker.Services;

namespace ANEFDailyChecker;

public partial class SettingsWindow : Window
{
    private AppState _state;

    public SettingsWindow(AppState state)
    {
        InitializeComponent();
        _state = state;
        MemoList.ItemsSource = _state.Memos;
        ResetTimeBox.Text = _state.ResetTime.ToString(@"hh\:mm");
    }

    private void AddMemo(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(MemoText.Text))
        {
            _state.Memos.Add(new MemoItem { Text = MemoText.Text });
            MemoText.Clear();
        }
    }

    private void MemoText_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        // Key.Return のみ反応する。日本語IME変換中のEnterは Key.ImeProcessed になるため自然に無視される
        if (e.Key == System.Windows.Input.Key.Return)
        {
            AddMemo(sender, e);
            e.Handled = true;
        }
    }

    private void EditMemo(object sender, RoutedEventArgs e)
    {
        if (MemoList.SelectedItem is MemoItem target)
        {
            new EditMemoWindow(target) { Owner = this }.ShowDialog();
        }
    }

    private void DeleteMemo(object sender, RoutedEventArgs e)
    {
        if (MemoList.SelectedItem is MemoItem target)
        {
            if (MessageBox.Show($"「{target.Text}」を削除しますか？", "確認", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _state.Memos.Remove(target);
            }
        }
    }

    private void MoveUp(object sender, RoutedEventArgs e)
    {
        int i = MemoList.SelectedIndex;
        if (i > 0)
        {
            var item = _state.Memos[i];
            _state.Memos.RemoveAt(i);
            _state.Memos.Insert(i - 1, item);
            MemoList.SelectedIndex = i - 1;
        }
    }

    private void MoveDown(object sender, RoutedEventArgs e)
    {
        int i = MemoList.SelectedIndex;
        if (i >= 0 && i < _state.Memos.Count - 1)
        {
            var item = _state.Memos[i];
            _state.Memos.RemoveAt(i);
            _state.Memos.Insert(i + 1, item);
            MemoList.SelectedIndex = i + 1;
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        var text = ResetTimeBox.Text.Trim();

        // hh:mm 形式の厳密チェック（00:00〜23:59）
        if (!Regex.IsMatch(text, @"^\d{2}:\d{2}$") ||
            !TimeSpan.TryParse(text, out var ts) ||
            ts.TotalHours >= 24 || ts.TotalMinutes < 0)
        {
            MessageBox.Show(
                "リセット時刻はHH:MM形式(例:09:00)で入力してください。\n00:00〜23:59の範囲で入力してください。",
                "入力エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            ResetTimeBox.Focus();
            ResetTimeBox.SelectAll();
            e.Cancel = true;
            return;
        }

        _state.ResetTime = ts;
        base.OnClosing(e);
    }
}
