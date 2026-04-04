using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace ANEFDailyChecker.Models;

public enum TimerState { Stopped, Running, Paused }

public class TimerConfig : INotifyPropertyChanged
{
    private string _name = "タイマー";
    private bool _isFixedMode = true;
    private int _fixedMinutes = 5;
    private bool _useBuiltinSound = true;
    private string _soundPath = "";
    private int _soundRepeatCount = 1;

    // ── 保存対象のランタイム状態（閉じていた間の経過処理に必要）────────
    private TimerState _state = TimerState.Stopped;
    private int _remainingSeconds = 0;
    private int _currentSetMinutes = 0;
    private DateTime? _startedAt = null;

    // ── 設定プロパティ ───────────────────────────────────────────────────
    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public bool IsFixedMode
    {
        get => _isFixedMode;
        set
        {
            _isFixedMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsInputMode));
            OnPropertyChanged(nameof(DisplayText));
        }
    }

    [JsonIgnore] public bool IsInputMode => !IsFixedMode;

    public int FixedMinutes
    {
        get => _fixedMinutes;
        set { _fixedMinutes = Math.Max(1, value); OnPropertyChanged(); OnPropertyChanged(nameof(DisplayText)); }
    }

    public bool UseBuiltinSound
    {
        get => _useBuiltinSound;
        set { _useBuiltinSound = value; OnPropertyChanged(); OnPropertyChanged(nameof(UseCustomSound)); }
    }

    [JsonIgnore] public bool UseCustomSound => !UseBuiltinSound;

    public string SoundPath
    {
        get => _soundPath;
        set { _soundPath = value; OnPropertyChanged(); }
    }

    public int SoundRepeatCount
    {
        get => _soundRepeatCount;
        set { _soundRepeatCount = Math.Max(1, value); OnPropertyChanged(); }
    }

    // ── ランタイム状態（state.json に保存）──────────────────────────────
    public TimerState State
    {
        get => _state;
        set
        {
            _state = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsStopped));
            OnPropertyChanged(nameof(IsRunning));
            OnPropertyChanged(nameof(IsPaused));
            OnPropertyChanged(nameof(IsActive));
            OnPropertyChanged(nameof(DisplayText));
            OnPropertyChanged(nameof(PlayPauseLabel));
        }
    }

    public int RemainingSeconds
    {
        get => _remainingSeconds;
        set
        {
            _remainingSeconds = Math.Max(0, value);
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayText));
        }
    }

    public int CurrentSetMinutes
    {
        get => _currentSetMinutes;
        set { _currentSetMinutes = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayText)); }
    }

    /// <summary>タイマー再生開始日時。閉じていた間の経過計算に使用。停止・一時停止時は null。</summary>
    public DateTime? StartedAt
    {
        get => _startedAt;
        set { _startedAt = value; OnPropertyChanged(); }
    }

    // ── 算出プロパティ ───────────────────────────────────────────────────
    [JsonIgnore] public bool IsStopped => _state == TimerState.Stopped;
    [JsonIgnore] public bool IsRunning => _state == TimerState.Running;
    [JsonIgnore] public bool IsPaused => _state == TimerState.Paused;
    [JsonIgnore] public bool IsActive => !IsStopped;

    [JsonIgnore]
    public string DisplayText
    {
        get
        {
            if (IsStopped)
            {
                int mins = IsFixedMode ? FixedMinutes : CurrentSetMinutes;
                if (mins <= 0) return IsInputMode ? "(時間を入力)" : "0分";
                int h = mins / 60, m = mins % 60;
                if (h > 0 && m > 0) return $"{h}時間{m}分";
                if (h > 0) return $"{h}時間";
                return $"{m}分";
            }
            else
            {
                int total = RemainingSeconds;
                int h = total / 3600, m = (total % 3600) / 60, s = total % 60;
                return h > 0 ? $"{h:D2}:{m:D2}:{s:D2}" : $"{m:D2}:{s:D2}";
            }
        }
    }

    [JsonIgnore] public string PlayPauseLabel => IsRunning ? "⏸" : "▶";

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
