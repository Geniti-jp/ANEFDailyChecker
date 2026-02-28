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
        GroupCheckBox.IsChecked = item.IsGroup;
        ChildListBox.ItemsSource = item.Children;

        RefreshVisibility();
    }

    private void GroupModeChanged(object sender, RoutedEventArgs e)
    {
        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        var isGroup = GroupCheckBox.IsChecked ?? false;

        // 各要素の表示切り替え
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

    private void EditChild(object sender, RoutedEventArgs e)
    {
        if (ChildListBox.SelectedItem is MemoItem target)
        {
            var result = Interaction.InputBox("子要素の編集", "編集", target.Text);
            if (!string.IsNullOrWhiteSpace(result))
            {
                target.Text = result;
                ChildListBox.Items.Refresh();
            }
        }
    }

    private void DeleteChild(object sender, RoutedEventArgs e)
    {
        if (ChildListBox.SelectedItem is MemoItem target)
        {
            _item.Children.Remove(target);
        }
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
        _item.Text = ParentTextBox.Text;
        _item.IsGroup = GroupCheckBox.IsChecked ?? false;
        if (!_item.IsGroup) _item.Children.Clear();

        DialogResult = true;
        Close();
    }
}