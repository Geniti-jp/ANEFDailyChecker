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
    private bool _useDayOfWeekMode = false;

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

    /// <summary>
    /// true のとき DayOfWeekTexts を参照して曜日別テキストを返す。
    /// false のときは常に Text を返す。
    /// </summary>
    public bool UseDayOfWeekMode
    {
        get => _useDayOfWeekMode;
        set { _useDayOfWeekMode = value; OnPropertyChanged(); OnPropertyChanged(nameof(EffectiveText)); OnPropertyChanged(nameof(DisplayText)); }
    }

    /// <summary>曜日別テキスト。キー = (int)DayOfWeek（0=日〜6=土）。</summary>
    public Dictionary<int, string> DayOfWeekTexts { get; set; } = new();

    /// <summary>
    /// UseDayOfWeekMode が true で今日の曜日に対応するテキストがあればそれを返す。
    /// それ以外は Text を返す。
    /// </summary>
    [JsonIgnore]
    public string EffectiveText
    {
        get
        {
            if (UseDayOfWeekMode)
            {
                int dow = (int)DateTime.Now.DayOfWeek;
                if (DayOfWeekTexts.TryGetValue(dow, out var t) && !string.IsNullOrEmpty(t))
                    return t;
            }
            return Text;
        }
    }

    [JsonIgnore]
    public string DisplayPrefix =>
        ResetCount <= 1 ? "" :
        RemainingCount == 0 ? "(今日)" :
        $"({RemainingCount}日後)";

    [JsonIgnore]
    public string DisplayText => DisplayPrefix + EffectiveText;

    public ObservableCollection<MemoItem> Children { get; set; } = new();

    public void UpdateStatusFromChildren() => OnPropertyChanged(nameof(IsChecked));

    /// <summary>曜日が変わったときに DisplayText を再通知する。</summary>
    public void RefreshDayText()
    {
        OnPropertyChanged(nameof(EffectiveText));
        OnPropertyChanged(nameof(DisplayText));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
