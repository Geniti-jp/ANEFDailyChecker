using System.Windows;
using Microsoft.VisualBasic;
using ANEFDailyChecker.Models;

namespace ANEFDailyChecker;

public partial class EditMemoWindow : Window
{
    private MemoItem _item;

    public EditMemoWindow(MemoItem item)
    {
        InitializeComponent();
        _item = item;
        ParentTextBox.Text = item.Text;
        ResetCountBox.Text = item.ResetCount.ToString();
        GroupCheckBox.IsChecked = item.IsGroup;
        ChildListBox.ItemsSource = item.Children;
        RefreshVisibility();
    }

    private void GroupModeChanged(object sender, RoutedEventArgs e) => RefreshVisibility();

    private void RefreshVisibility()
    {
        var isGroup = GroupCheckBox.IsChecked ?? false;
        if (ChildInputArea != null) ChildInputArea.Visibility = isGroup ? Visibility.Visible : Visibility.Collapsed;
        if (ChildListBox != null) ChildListBox.Visibility = isGroup ? Visibility.Visible : Visibility.Collapsed;
        if (MoveUpButton != null) MoveUpButton.Visibility = isGroup ? Visibility.Visible : Visibility.Collapsed;
        if (MoveDownButton != null) MoveDownButton.Visibility = isGroup ? Visibility.Visible : Visibility.Collapsed;
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
            string result = Interaction.InputBox("内容を編集", "編集", target.Text);
            if (!string.IsNullOrWhiteSpace(result))
            {
                target.Text = result;
                ChildListBox.Items.Refresh();
            }
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
        // ResetCount のバリデーション（1以上の整数のみ）
        if (!int.TryParse(ResetCountBox.Text, out int resetCount) || resetCount < 1)
        {
            MessageBox.Show("リセット日数は 1 以上の整数で入力してください。", "入力エラー",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            ResetCountBox.Focus();
            ResetCountBox.SelectAll();
            return;
        }

        _item.Text = ParentTextBox.Text;
        _item.IsGroup = GroupCheckBox.IsChecked ?? false;

        // ResetCount が変わった場合、RemainingCount も新しい値に合わせる
        if (_item.ResetCount != resetCount)
        {
            _item.ResetCount = resetCount;
            _item.RemainingCount = resetCount;
        }

        DialogResult = true;
    }
}
