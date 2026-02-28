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
        ChildInputArea.Visibility = isGroup ? Visibility.Visible : Visibility.Collapsed;
        ChildListBox.Visibility = isGroup ? Visibility.Visible : Visibility.Collapsed;
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

    private void OkClick(object sender, RoutedEventArgs e)
    {
        _item.Text = ParentTextBox.Text;
        _item.IsGroup = GroupCheckBox.IsChecked ?? false;
        if (!_item.IsGroup) _item.Children.Clear();

        DialogResult = true;
        Close();
    }
}