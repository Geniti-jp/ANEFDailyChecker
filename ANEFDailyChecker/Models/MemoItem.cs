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
    private int _resetCount = 1;
    private int _remainingCount = 1;

    public string Text
    {
        get => _text;
        set
        {
            _text = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayText));
        }
    }

    /// <summary>
    /// state.json に保存される実データ。
    /// IsGroup が false の場合のみ、この値がチェック状態として使用される。
    /// </summary>
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

    /// <summary>
    /// UI バインディング用。
    /// グループの場合は子要素の状態から計算し、単体項目の場合は IsItemChecked を返す。
    /// </summary>
    [JsonIgnore]
    public bool IsChecked
    {
        get => IsGroup ? (Children.Count > 0 && Children.All(c => c.IsChecked)) : IsItemChecked;
        set
        {
            if (!IsGroup) IsItemChecked = value;
            OnPropertyChanged();
        }
    }

    public bool IsGroup
    {
        get => _isGroup;
        set
        {
            _isGroup = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsChecked));
        }
    }

    /// <summary>
    /// リセットまでに必要な日数（時刻跨ぎ回数）。デフォルト1。
    /// </summary>
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

    /// <summary>
    /// リセットまでの残り回数。0 のとき次の時刻跨ぎでリセット済みを意味する。
    /// </summary>
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
    /// ResetCount > 1 のときのみ表示するプレフィックス。
    /// RemainingCount == 0 なら "(今日)"、それ以外は "(X日後)"。
    /// </summary>
    [JsonIgnore]
    public string DisplayPrefix =>
        ResetCount <= 1 ? "" :
        RemainingCount == 0 ? "(今日)" :
        $"({RemainingCount}日後)";

    /// <summary>
    /// UI 表示用テキスト（プレフィックス付き）。
    /// </summary>
    [JsonIgnore]
    public string DisplayText => DisplayPrefix + Text;

    public ObservableCollection<MemoItem> Children { get; set; } = new();

    public void UpdateStatusFromChildren()
    {
        OnPropertyChanged(nameof(IsChecked));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
