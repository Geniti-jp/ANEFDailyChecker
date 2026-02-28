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
            MemoList.Items.Refresh();
            MemoText.Clear();
        }
    }

    private void EditMemo(object sender, RoutedEventArgs e)
    {
        if (MemoList.SelectedItem is MemoItem target)
        {
            if (new EditMemoWindow(target) { Owner = this }.ShowDialog() == true)
                MemoList.Items.Refresh();
        }
    }

    private void DeleteMemo(object sender, RoutedEventArgs e)
    {
        if (MemoList.SelectedItem is MemoItem target)
        {
            if (MessageBox.Show($"「{target.Text}」を削除しますか？", "確認", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _state.Memos.Remove(target);
                MemoList.Items.Refresh();
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
            MemoList.Items.Refresh();
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
            MemoList.Items.Refresh();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        if (TimeSpan.TryParse(ResetTimeBox.Text, out var ts)) _state.ResetTime = ts;
        base.OnClosed(e);
    }
}