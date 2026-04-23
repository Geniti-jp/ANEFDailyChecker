using System.Windows;
using System.Windows.Controls;
using ANEFDailyChecker.Models;

namespace ANEFDailyChecker;

public partial class EditMemoWindow : Window
{
    private MemoItem _item;
    private TextBox[] _dayBoxes = null!;

    public EditMemoWindow(MemoItem item)
    {
        InitializeComponent();
        _item = item;

        _dayBoxes = new[] { SunBox, MonBox, TueBox, WedBox, ThuBox, FriBox, SatBox };

        ParentTextBox.Text = item.Text;
        ResetCountBox.Text = item.ResetCount.ToString();
        GroupCheckBox.IsChecked = item.IsGroup;
        DayOfWeekCheckBox.IsChecked = item.UseDayOfWeekMode;
        ChildListBox.ItemsSource = item.Children;

        for (int i = 0; i < _dayBoxes.Length; i++)
        {
            if (item.DayOfWeekTexts.TryGetValue(i, out var t))
                _dayBoxes[i].Text = t;
        }

        RefreshVisibility();
    }

    private void GroupModeChanged(object sender, RoutedEventArgs e) => RefreshVisibility();
    private void DayModeChanged(object sender, RoutedEventArgs e) => RefreshVisibility();

    private void RefreshVisibility()
    {
        bool isGroup  = GroupCheckBox.IsChecked    ?? false;
        bool isDayMode = DayOfWeekCheckBox.IsChecked ?? false;

        if (ChildInputArea  != null) ChildInputArea.Visibility  = isGroup   ? Visibility.Visible : Visibility.Collapsed;
        if (ChildListBox    != null) ChildListBox.Visibility    = isGroup   ? Visibility.Visible : Visibility.Collapsed;
        if (MoveUpButton    != null) MoveUpButton.Visibility    = isGroup   ? Visibility.Visible : Visibility.Collapsed;
        if (MoveDownButton  != null) MoveDownButton.Visibility  = isGroup   ? Visibility.Visible : Visibility.Collapsed;
        if (DayOfWeekPanel  != null) DayOfWeekPanel.Visibility  = isDayMode ? Visibility.Visible : Visibility.Collapsed;
    }

    private void AddChild(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(ChildTextBox.Text))
        {
            _item.Children.Add(new MemoItem { Text = ChildTextBox.Text });
            ChildTextBox.Clear();
        }
    }

    private void ChildTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Return)
        {
            AddChild(sender, e);
            e.Handled = true;
        }
    }

    private void EditChild(object sender, RoutedEventArgs e)
    {
        if (ChildListBox.SelectedItem is MemoItem target)
        {
            var win = new EditChildWindow(target) { Owner = this };
            win.ShowDialog();
            // EditChildWindow が target を直接更新するので追加処理不要
            ChildListBox.Items.Refresh();
        }
    }

    private void DeleteChild(object sender, RoutedEventArgs e)
    {
        if (ChildListBox.SelectedItem is MemoItem target) _item.Children.Remove(target);
    }

    private void MoveChildUp(object sender, RoutedEventArgs e)
    {
        var i = ChildListBox.SelectedIndex;
        if (i > 0)
        {
            var target = _item.Children[i];
            _item.Children.RemoveAt(i);
            _item.Children.Insert(i - 1, target);
            ChildListBox.SelectedIndex = i - 1;
        }
    }

    private void MoveChildDown(object sender, RoutedEventArgs e)
    {
        var i = ChildListBox.SelectedIndex;
        if (i >= 0 && i < _item.Children.Count - 1)
        {
            var target = _item.Children[i];
            _item.Children.RemoveAt(i);
            _item.Children.Insert(i + 1, target);
            ChildListBox.SelectedIndex = i + 1;
        }
    }

    private void OkClick(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(ResetCountBox.Text, out int resetCount) || resetCount < 1)
        {
            MessageBox.Show("リセット日数は 1 以上の整数で入力してください。", "入力エラー",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            ResetCountBox.Focus();
            ResetCountBox.SelectAll();
            return;
        }

        _item.Text    = ParentTextBox.Text;
        _item.IsGroup = GroupCheckBox.IsChecked ?? false;
        _item.UseDayOfWeekMode = DayOfWeekCheckBox.IsChecked ?? false;

        if (_item.ResetCount != resetCount)
        {
            _item.ResetCount    = resetCount;
            _item.RemainingCount = resetCount;
        }

        // 曜日別テキストを保存（空欄はキーを削除）
        _item.DayOfWeekTexts.Clear();
        for (int i = 0; i < _dayBoxes.Length; i++)
        {
            string val = _dayBoxes[i].Text.Trim();
            if (!string.IsNullOrEmpty(val))
                _item.DayOfWeekTexts[i] = val;
        }

        _item.RefreshDayText();
        DialogResult = true;
    }
}
