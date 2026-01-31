using System.IO;
using System.Text.Json;
using ANEFDailyChecker.Models;


namespace ANEFDailyChecker.Services;

public class AppState
{
    public List<MemoItem> Memos { get; set; } = new();
    public TimeSpan ResetTime { get; set; } = new(0, 0, 0);
}

public static class AppStateService
{
    private static readonly string FilePath =
        Path.Combine(AppContext.BaseDirectory, "state.json");

    public static AppState Load()
    {
        if (!File.Exists(FilePath)) return new AppState();
        return JsonSerializer.Deserialize<AppState>(File.ReadAllText(FilePath))!;
    }

    public static void Save(AppState state)
    {
        File.WriteAllText(FilePath,
            JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
    }
}
