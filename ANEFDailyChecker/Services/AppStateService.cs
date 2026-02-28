using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using ANEFDailyChecker.Models;

namespace ANEFDailyChecker.Services;

public class AppState
{
    public List<MemoItem> Memos { get; set; } = new();
    public TimeSpan ResetTime { get; set; } = new(0, 0, 0);
}

public static class AppStateService
{
    private static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, "state.json");

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    public static AppState Load()
    {
        if (!File.Exists(FilePath)) return new AppState();
        try
        {
            string json = File.ReadAllText(FilePath, Encoding.UTF8);
            return JsonSerializer.Deserialize<AppState>(json, Options) ?? new AppState();
        }
        catch { return new AppState(); }
    }

    public static void Save(AppState state)
    {
        string json = JsonSerializer.Serialize(state, Options);
        File.WriteAllText(FilePath, json, new UTF8Encoding(false));
    }
}