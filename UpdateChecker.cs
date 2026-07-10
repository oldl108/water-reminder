using System.Net.Http;
using System.Text.Json;

namespace WaterReminder;

/// <summary>
/// 检查最新版本。只在用户手动点"检查更新"时才联网，平时零网络请求。
/// 数据源按国内可达性排序：自有服务器 → jsDelivr CDN → GitHub API。
/// 下载走蓝奏云（国内快），GitHub Releases 作为备用。
/// </summary>
static class UpdateChecker
{
    const string ServerUrl = "http://124.223.155.194/water/version.json";
    const string JsdelivrUrl = "https://cdn.jsdelivr.net/gh/oldl108/water-reminder@main/version.json";
    const string GithubApi = "https://api.github.com/repos/oldl108/water-reminder/releases/latest";
    public const string LanzouPage = "https://gg999.lanzouv.com/s/heshui";
    public const string ReleasesPage = "https://github.com/oldl108/water-reminder/releases";

    public record Result(Version Latest, string DownloadUrl);

    public static async Task<Result?> FetchLatestAsync()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(6) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("WaterReminder-UpdateCheck");

        foreach (var source in new[] { ServerUrl, JsdelivrUrl })
        {
            try
            {
                using var doc = JsonDocument.Parse(await http.GetStringAsync(source));
                string tag = doc.RootElement.GetProperty("version").GetString() ?? "";
                string url = doc.RootElement.TryGetProperty("url", out var u)
                    ? u.GetString() ?? LanzouPage : LanzouPage;
                if (Version.TryParse(tag.TrimStart('v', 'V'), out var v))
                    return new Result(Normalize(v), url);
            }
            catch { }
        }

        try
        {
            using var doc = JsonDocument.Parse(await http.GetStringAsync(GithubApi));
            string tag = doc.RootElement.GetProperty("tag_name").GetString() ?? "";
            if (Version.TryParse(tag.TrimStart('v', 'V'), out var v))
                return new Result(Normalize(v), LanzouPage);
        }
        catch { }

        return null;
    }

    public static Version Current =>
        Normalize(typeof(UpdateChecker).Assembly.GetName().Version ?? new Version(0, 0, 0));

    /// <summary>统一成三段（0.3.1.0 和 0.3.1 比较时不出岔子）。</summary>
    static Version Normalize(Version v) =>
        new(v.Major, v.Minor, Math.Max(v.Build, 0));
}
