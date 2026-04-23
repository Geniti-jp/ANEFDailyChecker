using System.Windows;
using System.Windows.Controls;
using ANEFDailyChecker.Models;

namespace ANEFDailyChecker;

public partial class EditChildWindow : Window
{
    private MemoItem _item;
    private TextBox[] _dayBoxes = null!;

    public EditChildWindow(MemoItem item)
    {
        InitializeComponent();
        _item = item;

        _dayBoxes = new[] { SunBox, MonBox, TueBox, WedBox, ThuBox, FriBox, SatBox };

        TextBox.Text = item.Text;
        DayOfWeekCheckBox.IsChecked = item.UseDayOfWeekMode;

        for (int i = 0; i < _dayBoxes.Length; i++)
        {
            if (item.DayOfWeekTexts.TryGetValue(i, out var t))
                _dayBoxes[i].Text = t;
        }

        RefreshVisibility();
    }

    private void DayModeChanged(object sender, RoutedEventArgs e) => RefreshVisibility();

    private void RefreshVisibility()
    {
        bool isDayMode = DayOfWeekCheckBox.IsChecked ?? false;
        if (DayOfWeekPanel != null)
            DayOfWeekPanel.Visibility = isDayMode ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OkClick(object sender, RoutedEventArgs e)
    {
        _item.Text = TextBox.Text;
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
