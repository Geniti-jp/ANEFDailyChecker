using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace ANEFDailyChecker.Models;

public class MemoItem : INotifyPropertyChanged
{
    private string _text = "";
    private bool _isItemChecked;
    private bool _isGroup;
    private bool _isExpanded = false;
    private int _resetCount = 1;
    private int _remainingCount = 1;

    public string Text
    {
        get => _text;
        set { _text = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayText)); }
    }

    public bool IsItemChecked
    {
        get => _isItemChecked;
        set
        {
            if (_isItemChecked != value)
            {
                _isItemChecked = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsChecked));
            }
        }
    }

    [JsonIgnore]
    public bool IsChecked
    {
        get => IsGroup ? (Children.Count > 0 && Children.All(c => c.IsChecked)) : IsItemChecked;
        set { if (!IsGroup) IsItemChecked = value; OnPropertyChanged(); }
    }

    public bool IsGroup
    {
        get => _isGroup;
        set { _isGroup = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsChecked)); }
    }

    /// <summary>グループの展開状態（state.json に保存）</summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set { _isExpanded = value; OnPropertyChanged(); }
    }

    public int ResetCount
    {
        get => _resetCount;
        set
        {
            if (_resetCount != value)
            {
                _resetCount = Math.Max(1, value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayPrefix));
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    public int RemainingCount
    {
        get => _remainingCount;
        set
        {
            if (_remainingCount != value)
            {
                _remainingCount = Math.Max(0, value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayPrefix));
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    [JsonIgnore]
    public string DisplayPrefix =>
        ResetCount <= 1 ? "" :
        RemainingCount == 0 ? "(今日)" :
        $"({RemainingCount}日後)";

    [JsonIgnore]
    public string DisplayText => DisplayPrefix + Text;

    public ObservableCollection<MemoItem> Children { get; set; } = new();

    public void UpdateStatusFromChildren() => OnPropertyChanged(nameof(IsChecked));

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
