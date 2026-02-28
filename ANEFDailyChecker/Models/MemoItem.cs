using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ANEFDailyChecker.Models;

public class MemoItem : INotifyPropertyChanged
{
    private string _text = "";
    private bool _isChecked;
    private bool _isGroup;

    public string Text
    {
        get => _text;
        set { _text = value; OnPropertyChanged(); }
    }

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (IsGroup && Children.Count > 0)
            {
                var allChecked = Children.All(c => c.IsChecked);
                if (_isChecked != allChecked)
                {
                    _isChecked = allChecked;
                    OnPropertyChanged();
                }
            }
            else
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnPropertyChanged();
                }
            }
        }
    }

    public bool IsGroup
    {
        get => _isGroup;
        set { _isGroup = value; OnPropertyChanged(); }
    }

    public ObservableCollection<MemoItem> Children { get; set; } = new();

    public void UpdateStatusFromChildren()
    {
        if (!IsGroup) return;
        var allChecked = Children.Count > 0 && Children.All(c => c.IsChecked);
        if (_isChecked != allChecked)
        {
            _isChecked = allChecked;
            OnPropertyChanged(nameof(IsChecked));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}