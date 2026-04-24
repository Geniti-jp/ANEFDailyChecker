using System.Windows;
using System.Windows.Controls;
using ANEFDailyChecker.Models;

namespace ANEFDailyChecker;

public partial class EditChildWindow : Window
{
    private MemoItem _item;
    private TextBox[] _dayBoxes = null!;

    /// <param name="item">編集対象の MemoItem。</param>
    /// <param name="showGroupMode">
    /// true（デフォルト）= 子項目編集：グループモード設定を表示する。
    /// false = 孫項目編集：グループモード設定を非表示にする（これ以上の入れ子不可）。
    /// </param>
    public EditChildWindow(MemoItem item, bool showGroupMode = true)
    {
        InitializeComponent();
        _item = item;

        _dayBoxes = new[] { SunBox, MonBox, TueBox, WedBox, ThuBox, FriBox, SatBox };

        Title = showGroupMode ? "子項目の編集" : "孫項目の編集";
        // 孫編集時はグループモードを非表示にし、高さを縮める
        if (!showGroupMode)
        {
            GroupCheckBox.Visibility = Visibility.Collapsed;
            Height = 460;
        }

        ItemTextBox.Text = item.Text;
        ResetCountBox.Text = item.ResetCount.ToString();
        GroupCheckBox.IsChecked = item.IsGroup;
        DayOfWeekCheckBox.IsChecked = item.UseDayOfWeekMode;
        GrandChildListBox.ItemsSource = item.Children;

        for (int i = 0; i < _dayBoxes.Length; i++)
        {
            if (item.DayOfWeekTexts.TryGetValue(i, out var t))
                _dayBoxes[i].Text = t;
        }

        RefreshVisibility();
    }

    private void GroupModeChanged(object sender, RoutedEventArgs e) => RefreshVisibility();
    private void DayModeChanged(object sender, RoutedEventArgs e)   => RefreshVisibility();

    private void RefreshVisibility()
    {
        bool isGroup   = GroupCheckBox.IsChecked    ?? false;
        bool isDayMode = DayOfWeekCheckBox.IsChecked ?? false;

        if (GrandChildInputArea != null) GrandChildInputArea.Visibility = isGroup   ? Visibility.Visible : Visibility.Collapsed;
        if (GrandChildListBox   != null) GrandChildListBox.Visibility   = isGroup   ? Visibility.Visible : Visibility.Collapsed;
        if (MoveUpBtn           != null) MoveUpBtn.Visibility           = isGroup   ? Visibility.Visible : Visibility.Collapsed;
        if (MoveDownBtn         != null) MoveDownBtn.Visibility         = isGroup   ? Visibility.Visible : Visibility.Collapsed;
        if (DayOfWeekPanel      != null) DayOfWeekPanel.Visibility      = isDayMode ? Visibility.Visible : Visibility.Collapsed;
    }

    // ─── 孫項目 CRUD ─────────────────────────────────────────────

    private void AddGrandChild(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(GrandChildTextBox.Text))
        {
            _item.Children.Add(new MemoItem { Text = GrandChildTextBox.Text });
            GrandChildTextBox.Clear();
        }
    }

    private void GrandChildTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Return)
        {
            AddGrandChild(sender, e);
            e.Handled = true;
        }
    }

    private void EditGrandChild(object sender, RoutedEventArgs e)
    {
        if (GrandChildListBox.SelectedItem is MemoItem target)
        {
            // showGroupMode=false → 孫は曾孫を持てない
            var win = new EditChildWindow(target, showGroupMode: false) { Owner = this };
            win.ShowDialog();
            GrandChildListBox.Items.Refresh();
        }
    }

    private void DeleteGrandChild(object sender, RoutedEventArgs e)
    {
        if (GrandChildListBox.SelectedItem is MemoItem target)
            _item.Children.Remove(target);
    }

    private void MoveGrandChildUp(object sender, RoutedEventArgs e)
    {
        int i = GrandChildListBox.SelectedIndex;
        if (i > 0)
        {
            var t = _item.Children[i];
            _item.Children.RemoveAt(i);
            _item.Children.Insert(i - 1, t);
            GrandChildListBox.SelectedIndex = i - 1;
        }
    }

    private void MoveGrandChildDown(object sender, RoutedEventArgs e)
    {
        int i = GrandChildListBox.SelectedIndex;
        if (i >= 0 && i < _item.Children.Count - 1)
        {
            var t = _item.Children[i];
            _item.Children.RemoveAt(i);
            _item.Children.Insert(i + 1, t);
            GrandChildListBox.SelectedIndex = i + 1;
        }
    }

    // ─── OK ──────────────────────────────────────────────────────

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

        _item.Text = ItemTextBox.Text;
        _item.IsGroup = GroupCheckBox.IsChecked ?? false;
        if (_item.ResetCount != resetCount)
        {
            _item.ResetCount    = resetCount;
            _item.RemainingCount = resetCount;
        }
        _item.UseDayOfWeekMode = DayOfWeekCheckBox.IsChecked ?? false;

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
