using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using ANEFDailyChecker.Models;

namespace ANEFDailyChecker.Services;

public class AppState
{
    public ObservableCollection<MemoItem> Memos { get; set; } = new();
    public TimeSpan ResetTime { get; set; } = new(0, 0, 0);
}

public static class AppStateService
{
    private static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, "state.json");

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static AppState Load()
    {
        if (!File.Exists(FilePath)) return new AppState();
        try
        {
            string json = File.ReadAllText(FilePath, Encoding.UTF8);
            var state = JsonSerializer.Deserialize<AppState>(json, Options) ?? new AppState();

            // 旧バージョンの state.json は ResetCount/RemainingCount が 0 になる場合があるため補正
            foreach (var memo in state.Memos)
            {
                if (memo.ResetCount < 1) memo.ResetCount = 1;
                if (memo.RemainingCount < 1) memo.RemainingCount = memo.ResetCount;
            }

            return state;
        }
        catch { return new AppState(); }
    }

    public static void Save(AppState state)
    {
        try
        {
            string json = JsonSerializer.Serialize(state, Options);
            File.WriteAllText(FilePath, json, new UTF8Encoding(false));
        }
        catch (UnauthorizedAccessException)
        {
            System.Windows.MessageBox.Show(
                $"state.json への書き込みに失敗しました。\n\n" +
                $"現在の保存先:\n{FilePath}\n\n" +
                $"Program Files など書き込みが制限されたフォルダでは保存できません。\n" +
                $"実行ファイルをデスクトップや Documents など書き込み可能な場所に移動してください。",
                "書き込みエラー",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }
}
