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

    public string Text
    {
        get => _text;
        set { _text = value; OnPropertyChanged(); }
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

    public ObservableCollection<MemoItem> Children { get; set; } = new();

    public void UpdateStatusFromChildren()
    {
        OnPropertyChanged(nameof(IsChecked));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}