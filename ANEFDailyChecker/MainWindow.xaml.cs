using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ANEFDailyChecker.Services;
using ANEFDailyChecker.Models;

namespace ANEFDailyChecker;

public partial class MainWindow : Window
{
    private AppState _state;
    private readonly DispatcherTimer _timer = new();
    private DateTime _lastKnownUpdateTime;
    private DateTime _lastKnownDate = DateTime.Today;

    private static readonly string BuiltinSoundFile =
        Path.Combine(AppContext.BaseDirectory, "chime01.wav");

    public MainWindow()
    {
        InitializeComponent();
        _state = AppStateService.Load();
        MemoList.ItemsSource = _state.Memos;
        TimerPanel.ItemsSource = _state.Timers;

        _lastKnownUpdateTime = GetCurrentUpdateTime(DateTime.Now);

        ProcessMissedResets();
        ProcessMissedTimers();

        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += TimerTick;
        _timer.Start();
        UpdateRemainingDisplay();
    }

    // ─── タイマー Tick ───────────────────────────────────────────

    private void TimerTick(object? sender, EventArgs e)
    {
        var now = DateTime.Now;
        var currentUpdateTime = GetCurrentUpdateTime(now);
        UpdateRemainingDisplay();

        if (_lastKnownUpdateTime != currentUpdateTime)
        {
            ProcessReset();
            _lastKnownUpdateTime = currentUpdateTime;
        }

        if (_lastKnownDate != DateTime.Today)
        {
            _lastKnownDate = DateTime.Today;
            RefreshAllDayTexts();
        }

        List<TimerConfig>? finished = null;
        foreach (var tc in _state.Timers)
        {
            if (tc.State != TimerState.Running) continue;
            tc.RemainingSeconds--;
            if (tc.RemainingSeconds <= 0)
            {
                tc.State = TimerState.Stopped;
                tc.StartedAt = null;
                finished ??= new();
                finished.Add(tc);
            }
        }

        if (finished != null)
        {
            AppStateService.Save(_state);
            foreach (var tc in finished)
            {
                var captured = tc;
                Dispatcher.InvokeAsync(() => ShowTimerFinished(captured));
            }
        }
    }

    // ─── メモリセット関連 ─────────────────────────────────────────

    private void UpdateRemainingDisplay()
    {
        var now = DateTime.Now;
        var currentUpdateTime = GetCurrentUpdateTime(now);
        var next = currentUpdateTime.AddDays(1);
        var remain = next - now;
        RemainingText.Text =
            $"次回更新まで{remain.Hours}時間{remain.Minutes}分({_state.ResetTime:hh\\:mm}更新)";
    }

    private DateTime GetCurrentUpdateTime(DateTime now)
    {
        var updateTime = now.Date + _state.ResetTime;
        if (now < updateTime) updateTime = updateTime.AddDays(-1);
        return updateTime;
    }

    private void ProcessMissedResets()
    {
        if (_state.LastClosedAt == null) return;
        var lastClosed = _state.LastClosedAt.Value;
        var now = DateTime.Now;
        if (now <= lastClosed) return;

        var checkTime = lastClosed;
        int count = 0;
        while (count < 365)
        {
            var todayReset = checkTime.Date + _state.ResetTime;
            DateTime nextReset = todayReset > checkTime ? todayReset : todayReset.AddDays(1);
            if (nextReset > now) break;
            count++;
            checkTime = nextReset;
        }
        for (int i = 0; i < count; i++) ProcessReset();
        if (count > 0) AppStateService.Save(_state);
    }

    private void ProcessMissedTimers()
    {
        var now = DateTime.Now;

        // 経過時間の基準は「前回アプリを閉じた時刻」。
        // StartedAt（タイマー開始時刻）を使うと、閉じる前にすでに動いていた分も
        // 二重に減算されてしまうため使用しない。
        if (_state.LastClosedAt == null) return;

        double elapsedSec = (now - _state.LastClosedAt.Value).TotalSeconds;
        if (elapsedSec <= 0) return;

        bool changed = false;

        foreach (var tc in _state.Timers)
        {
            if (tc.State != TimerState.Running) continue;

            int elapsed = (int)Math.Min(elapsedSec, tc.RemainingSeconds + 1);
            if (elapsed <= 0) continue;

            tc.RemainingSeconds -= elapsed;
            changed = true;

            if (tc.RemainingSeconds <= 0)
            {
                tc.State = TimerState.Stopped;
                tc.StartedAt = null;
                // 音声は ShowTimerFinished 内で鳴らす。ここで直接呼ぶと二重再生になる。
                var captured = tc;
                Dispatcher.InvokeAsync(() => ShowTimerFinished(captured));
            }
            else
            {
                // 再起動後も Tick が正しく動くよう StartedAt を現在時刻に更新
                tc.StartedAt = now;
            }
        }

        if (changed) AppStateService.Save(_state);
    }

    private void RefreshAllDayTexts()
    {
        foreach (var m in _state.Memos)
        {
            m.RefreshDayText();
            foreach (var child in m.Children)
            {
                child.RefreshDayText();
                foreach (var grandchild in child.Children)
                    grandchild.RefreshDayText();
            }
        }
    }

    private void ProcessReset()
    {
        foreach (var m in _state.Memos)
        {
            // ── 親レベルのリセット処理 ──────────────────────────────
            bool parentHasCheck = m.IsGroup
                ? m.Children.Any(c => c.IsChecked || c.Children.Any(gc => gc.IsChecked))
                : m.IsItemChecked;

            if (m.RemainingCount <= 0)
            {
                if (parentHasCheck) ResetMemoAndDescendants(m);
            }
            else
            {
                m.RemainingCount--;
                if (m.RemainingCount == 0)
                    ResetMemoAndDescendants(m);
            }

            // ── 子レベルのリセット処理 ─────────────────────────────
            // 孫の RemainingCount は「子のリセットが発火したとき」にのみ減算する。
            // 子が日数を残している間は孫には手を付けない。
            foreach (var child in m.Children)
            {
                bool childHasCheck = child.IsGroup
                    ? child.Children.Any(gc => gc.IsChecked)
                    : child.IsItemChecked;

                bool childResetFires = false;

                if (child.RemainingCount <= 0)
                {
                    // すでに 0 かつチェックありならリセット対象
                    if (childHasCheck) childResetFires = true;
                }
                else
                {
                    child.RemainingCount--;
                    if (child.RemainingCount == 0) childResetFires = true;
                }

                if (childResetFires)
                {
                    // 子自身をリセット
                    child.IsItemChecked = false;

                    // 孫は子のリセット発火時にのみ自身の RemainingCount を減算
                    foreach (var grandchild in child.Children)
                    {
                        if (grandchild.RemainingCount <= 0)
                        {
                            if (grandchild.IsItemChecked)
                                grandchild.IsItemChecked = false;
                        }
                        else
                        {
                            grandchild.RemainingCount--;
                            if (grandchild.RemainingCount == 0)
                                grandchild.IsItemChecked = false;
                        }
                    }
                    child.UpdateStatusFromChildren();
                }
            }
            m.UpdateStatusFromChildren();
        }
        AppStateService.Save(_state);
    }

    private static void ResetChildAndDescendants(MemoItem child)
    {
        child.IsItemChecked = false;
        foreach (var gc in child.Children)
            gc.IsItemChecked = false;
        child.UpdateStatusFromChildren();
    }

    /// <summary>
    /// 親（トップレベル）自身のみリセットする。
    /// 子・孫は ProcessReset 内の各ループが RemainingCount に基づいて独立して管理するため
    /// ここでは手を付けない。
    /// </summary>
    private static void ResetMemoAndDescendants(MemoItem m)
    {
        if (!m.IsGroup)
        {
            m.IsItemChecked = false;
        }
        // IsGroup の場合は子のチェック状態によって IsChecked が決まるため
        // 子をリセットせずに UpdateStatusFromChildren のみ呼ぶ
        m.UpdateStatusFromChildren();
    }

    // ─── メモチェックボックス操作 ────────────────────────────────

    private void ParentCheckChanged(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox cb && cb.DataContext is MemoItem item && !item.IsGroup)
        {
            if (item.IsItemChecked)
                item.RemainingCount = item.ResetCount;
        }
        AppStateService.Save(_state);
    }

    private void ParentCheckPreview(object sender, MouseButtonEventArgs e)
    {
        if (sender is CheckBox cb && cb.DataContext is MemoItem item && item.IsGroup)
            e.Handled = true;
    }

    /// <summary>グループ子項目のチェックボックス直接クリックを阻止する。</summary>
    private void ChildGroupCheckPreview(object sender, MouseButtonEventArgs e)
    {
        if (sender is CheckBox cb && cb.DataContext is MemoItem item && item.IsGroup)
            e.Handled = true;
    }

    private void ChildCheckChanged(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox cb && cb.DataContext is MemoItem child)
            HandleChildCheckChange(child);
    }

    private void HandleChildCheckChange(MemoItem child)
    {
        // 子自身が単体でチェックされた、またはグループ子の全孫がチェックされた
        if (!child.IsGroup && child.IsItemChecked)
            child.RemainingCount = child.ResetCount;
        else if (child.IsGroup && child.IsChecked)
            child.RemainingCount = child.ResetCount;

        var parent = _state.Memos.FirstOrDefault(m => m.Children.Contains(child));
        if (parent != null)
        {
            parent.UpdateStatusFromChildren();
            if (parent.IsChecked)
                parent.RemainingCount = parent.ResetCount;
        }
        AppStateService.Save(_state);
    }

    private void GrandChildCheckChanged(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox cb && cb.DataContext is MemoItem grandchild)
            HandleGrandChildCheckChange(grandchild);
    }

    private void HandleGrandChildCheckChange(MemoItem grandchild)
    {
        // 孫自身のチェック完了時に RemainingCount をリセット
        if (grandchild.IsItemChecked)
            grandchild.RemainingCount = grandchild.ResetCount;

        MemoItem? parentChild = null;
        MemoItem? parent = null;
        foreach (var m in _state.Memos)
        {
            foreach (var child in m.Children)
            {
                if (child.Children.Contains(grandchild))
                {
                    parentChild = child;
                    parent = m;
                    break;
                }
            }
            if (parent != null) break;
        }

        if (parentChild != null)
        {
            parentChild.UpdateStatusFromChildren();
            if (parentChild.IsGroup && parentChild.IsChecked)
                parentChild.RemainingCount = parentChild.ResetCount;
        }
        if (parent != null)
        {
            parent.UpdateStatusFromChildren();
            if (parent.IsChecked)
                parent.RemainingCount = parent.ResetCount;
        }
        AppStateService.Save(_state);
    }

    // ─── 行全体クリック ───────────────────────────────────────────

    private void RowMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;
        if (e.OriginalSource is DependencyObject orig)
        {
            var p = orig;
            while (p != null)
            {
                if (p is CheckBox || p is ToggleButton || p is Button) return;
                if (p == sender) break;
                p = VisualTreeHelper.GetParent(p);
            }
        }
        if (sender is FrameworkElement fe && fe.DataContext is MemoItem item)
        {
            if (item.IsGroup)
            {
                item.IsExpanded = !item.IsExpanded;
                AppStateService.Save(_state);
            }
            else
            {
                item.IsItemChecked = !item.IsItemChecked;
                if (item.IsItemChecked)
                    item.RemainingCount = item.ResetCount;
                AppStateService.Save(_state);
            }
            e.Handled = true;
        }
    }

    /// <summary>
    /// 子行クリック。グループ子なら展開切替、非グループ子ならチェック切替。
    /// </summary>
    private void ChildRowMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;
        var orig = e.OriginalSource as DependencyObject;
        while (orig != null)
        {
            if (orig is CheckBox || orig is ToggleButton) { e.Handled = true; return; }
            if (orig == sender) break;
            orig = VisualTreeHelper.GetParent(orig);
        }
        if (sender is FrameworkElement fe && fe.DataContext is MemoItem child)
        {
            if (child.IsGroup)
            {
                child.IsExpanded = !child.IsExpanded;
                AppStateService.Save(_state);
            }
            else
            {
                child.IsItemChecked = !child.IsItemChecked;
                HandleChildCheckChange(child);
            }
            e.Handled = true;
        }
    }

    private void GrandChildRowMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;
        var orig = e.OriginalSource as DependencyObject;
        while (orig != null)
        {
            if (orig is CheckBox) { e.Handled = true; return; }
            if (orig == sender) break;
            orig = VisualTreeHelper.GetParent(orig);
        }
        if (sender is FrameworkElement fe && fe.DataContext is MemoItem grandchild)
        {
            grandchild.IsItemChecked = !grandchild.IsItemChecked;
            HandleGrandChildCheckChange(grandchild);
            e.Handled = true;
        }
    }

    // ─── 設定ウィンドウ ───────────────────────────────────────────

    private void OpenSettings(object sender, RoutedEventArgs e)
    {
        new SettingsWindow(_state).ShowDialog();
        TimerPanel.ItemsSource = null;
        TimerPanel.ItemsSource = _state.Timers;
        _lastKnownUpdateTime = GetCurrentUpdateTime(DateTime.Now);
        UpdateRemainingDisplay();
        AppStateService.Save(_state);
    }

    private void OpenTimerSettings(object sender, RoutedEventArgs e)
    {
        new TimerSettingsWindow(_state) { Owner = this }.ShowDialog();
        TimerPanel.ItemsSource = null;
        TimerPanel.ItemsSource = _state.Timers;
        AppStateService.Save(_state);
    }

    // ─── タイマーボタン操作 ──────────────────────────────────────

    private void TimerRowMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;
        if (sender is FrameworkElement fe && fe.DataContext is TimerConfig tc && tc.IsStopped)
        {
            StartTimer(tc);
            e.Handled = true;
        }
    }

    private void TimerPlayClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is TimerConfig tc && tc.IsStopped)
            StartTimer(tc);
        e.Handled = true;
    }

    private void TimerPauseClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is TimerConfig tc && tc.IsRunning)
        {
            tc.State = TimerState.Paused;
            tc.StartedAt = null;
            AppStateService.Save(_state);
        }
        e.Handled = true;
    }

    private void TimerResumeClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is TimerConfig tc && tc.IsPaused)
        {
            tc.State = TimerState.Running;
            tc.StartedAt = DateTime.Now;
            AppStateService.Save(_state);
        }
        e.Handled = true;
    }

    private void TimerResetClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is TimerConfig tc && tc.IsActive)
        {
            var result = MessageBox.Show(
                $"「{tc.Name}」をリセットしますか？",
                "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                tc.State = TimerState.Stopped;
                tc.StartedAt = null;
                AppStateService.Save(_state);
            }
        }
        e.Handled = true;
    }

    private void StartTimer(TimerConfig tc)
    {
        int minutes;
        if (tc.IsFixedMode)
        {
            minutes = tc.FixedMinutes;
        }
        else
        {
            var inputWin = new TimerInputWindow(tc.Name) { Owner = this };
            if (inputWin.ShowDialog() != true) return;
            minutes = inputWin.InputMinutes;
            tc.CurrentSetMinutes = minutes;
        }

        tc.RemainingSeconds = minutes * 60;
        tc.StartedAt = DateTime.Now;
        tc.State = TimerState.Running;
        AppStateService.Save(_state);
    }

    private void ShowTimerFinished(TimerConfig tc)
    {
        PlayTimerSound(tc);
        var notif = new TimerNotificationWindow(tc.Name, this);
        notif.Show();
    }

    private void PlayTimerSound(TimerConfig tc)
    {
        try
        {
            string soundFile = tc.UseBuiltinSound ? BuiltinSoundFile : tc.SoundPath;

            if (!tc.UseBuiltinSound && !File.Exists(soundFile))
            {
                MessageBox.Show(
                    $"音声ファイルが見つかりません:\n{soundFile}\n\n同梱の音声を使用します。",
                    "音声ファイルエラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                soundFile = BuiltinSoundFile;
            }

            if (!File.Exists(soundFile)) return;

            int repeat = Math.Max(1, tc.SoundRepeatCount);
            for (int i = 0; i < repeat; i++)
            {
                using var player = new SoundPlayer(soundFile);
                player.PlaySync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"音声再生エラー: {ex.Message}");
        }
    }

    // ─── 終了処理 ────────────────────────────────────────────────

    protected override void OnClosed(EventArgs e)
    {
        _state.LastClosedAt = DateTime.Now;
        AppStateService.Save(_state);
        base.OnClosed(e);
    }
}
