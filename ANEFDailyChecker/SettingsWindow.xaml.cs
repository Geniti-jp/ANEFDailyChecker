using System.Windows;
using Microsoft.VisualBasic;
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
        var index = MemoList.SelectedIndex;
        if (index < 0) return;

        var current = _state.Memos[index].Text;

        var result = Interaction.InputBox(
            "メモ内容を編集してください",
            "編集",
            current);

        if (!string.IsNullOrWhiteSpace(result))
        {
            _state.Memos[index].Text = result;
            MemoList.Items.Refresh();
        }
    }

    private void DeleteMemo(object sender, RoutedEventArgs e)
    {
        var index = MemoList.SelectedIndex;
        if (index < 0) return;

        var text = _state.Memos[index].Text;

        var result = MessageBox.Show(
            $"以下のメモを削除しますか？\n\n{text}",
            "確認",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            _state.Memos.RemoveAt(index);
            MemoList.Items.Refresh();
        }
    }

    private void MoveUp(object sender, RoutedEventArgs e)
    {
        var i = MemoList.SelectedIndex;
        if (i > 0)
        {
            (_state.Memos[i - 1], _state.Memos[i]) =
            (_state.Memos[i], _state.Memos[i - 1]);
            MemoList.SelectedIndex = i - 1;
            MemoList.Items.Refresh();
        }
    }

    private void MoveDown(object sender, RoutedEventArgs e)
    {
        var i = MemoList.SelectedIndex;
        if (i >= 0 && i < _state.Memos.Count - 1)
        {
            (_state.Memos[i + 1], _state.Memos[i]) =
            (_state.Memos[i], _state.Memos[i + 1]);
            MemoList.SelectedIndex = i + 1;
            MemoList.Items.Refresh();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        if (TimeSpan.TryParse(ResetTimeBox.Text, out var t))
            _state.ResetTime = t;

        AppStateService.Save(_state);
        base.OnClosed(e);
    }
}
