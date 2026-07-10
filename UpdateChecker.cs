using System.Net.Http;
using System.Text.Json;

namespace WaterReminder;

/// <summary>
/// 检查 GitHub Releases 上的最新版本。只在用户手动点"检查更新"时才联网，
/// 平时程序不发任何网络请求。
/// </summary>
static class UpdateChecker
{
    const string Api = "https://api.github.com/repos/oldl108/water-reminder/releases/latest";
    public const string ReleasesPage = "https://github.com/oldl108/water-reminder/releases";

    public record Result(Version Latest, string Page, string? ZipUrl);

    public static async Task<Result?> FetchLatestAsync()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("WaterReminder-UpdateCheck");
        string json = await http.GetStringAsync(Api);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        string tag = root.GetProperty("tag_name").GetString() ?? "";
        string page = root.TryGetProperty("html_url", out var h) ? h.GetString() ?? ReleasesPage : ReleasesPage;
        string? zip = null;
        if (root.TryGetProperty("assets", out var assets) && assets.GetArrayLength() > 0)
            zip = assets[0].GetProperty("browser_download_url").GetString();
        if (!Version.TryParse(tag.TrimStart('v', 'V'), out var v)) return null;
        return new Result(Normalize(v), page, zip);
    }

    public static Version Current =>
        Normalize(typeof(UpdateChecker).Assembly.GetName().Version ?? new Version(0, 0, 0));

    /// <summary>统一成三段（0.3.1.0 和 0.3.1 比较时不出岔子）。</summary>
    static Version Normalize(Version v) =>
        new(v.Major, v.Minor, Math.Max(v.Build, 0));
}
