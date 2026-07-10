using System.Text.Json;
using System.Text.Json.Serialization;

namespace WaterReminder;

class AppConfig
{
    public int IntervalMinutes { get; set; } = 50;
    public int SnoozeMinutes { get; set; } = 20;
    public int DailyGoalMl { get; set; } = 2000;
    public int CupMl { get; set; } = 200;
    public int ActiveStartHour { get; set; } = 9;
    public int ActiveEndHour { get; set; } = 23;
    public int IdleResetMinutes { get; set; } = 10;
    public bool DeferWhenFullscreen { get; set; } = false;
    public int MedNagMinutes { get; set; } = 10;
    public List<MedSchedule> Meds { get; set; } = new();
    public bool BedtimeEnabled { get; set; } = true;
    public string BedtimeWorkday { get; set; } = "23:00";
    public string BedtimeRestday { get; set; } = "23:30";
    public int BedtimeNagMinutes { get; set; } = 30;
}

class MedSchedule
{
    public string Name { get; set; } = "";
    public List<string> Times { get; set; } = new();
}

class WaterEntry
{
    [JsonPropertyName("t")] public string Time { get; set; } = "";
    [JsonPropertyName("ml")] public int Ml { get; set; }
}

class MedEntry
{
    public string Name { get; set; } = "";
    public string Sched { get; set; } = "";
    public string TakenAt { get; set; } = "";
}

class DayRecord
{
    public List<WaterEntry> Water { get; set; } = new();
    public int Stands { get; set; }
    public List<MedEntry> Meds { get; set; } = new();

    [JsonIgnore] public int TotalMl => Water.Sum(w => w.Ml);
}

class DataStore
{
    static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WaterReminder");
    static readonly string ConfigPath = Path.Combine(Dir, "config.json");
    static readonly string DataPath = Path.Combine(Dir, "data.json");
    static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public AppConfig Config { get; private set; } = new();
    public Dictionary<string, DayRecord> Days { get; private set; } = new();

    public DataStore()
    {
        Directory.CreateDirectory(Dir);
        Config = Load<AppConfig>(ConfigPath) ?? new AppConfig();
        Days = Load<Dictionary<string, DayRecord>>(DataPath) ?? new();
        if (!File.Exists(ConfigPath)) SaveConfig();
    }

    static T? Load<T>(string path) where T : class
    {
        try
        {
            if (File.Exists(path))
                return JsonSerializer.Deserialize<T>(File.ReadAllText(path));
        }
        catch { }
        return null;
    }

    public void SaveConfig() => File.WriteAllText(ConfigPath, JsonSerializer.Serialize(Config, JsonOpts));
    public void SaveData() => File.WriteAllText(DataPath, JsonSerializer.Serialize(Days, JsonOpts));

    public DayRecord Today()
    {
        string key = DateTime.Now.ToString("yyyy-MM-dd");
        if (!Days.TryGetValue(key, out var rec))
        {
            rec = new DayRecord();
            Days[key] = rec;
        }
        return rec;
    }

    public void AddWater(int ml)
    {
        Today().Water.Add(new WaterEntry { Time = DateTime.Now.ToString("HH:mm"), Ml = ml });
        SaveData();
    }

    public void AddStand()
    {
        Today().Stands++;
        SaveData();
    }

    public void AddMedTaken(string name, string sched)
    {
        Today().Meds.Add(new MedEntry { Name = name, Sched = sched, TakenAt = DateTime.Now.ToString("HH:mm") });
        SaveData();
    }

    public bool IsMedTaken(string name, string sched) =>
        Today().Meds.Any(m => m.Name == name && m.Sched == sched);

    /// <summary>调试日志，方便排查"提醒到底弹没弹"。</summary>
    public void Log(string msg)
    {
        try { File.AppendAllText(Path.Combine(Dir, "log.txt"), $"{DateTime.Now:MM-dd HH:mm:ss} {msg}\r\n"); }
        catch { }
    }

    public DayRecord? Day(DateTime date) =>
        Days.TryGetValue(date.ToString("yyyy-MM-dd"), out var rec) ? rec : null;
}
