using System.Diagnostics;
using Microsoft.Win32;

namespace WaterReminder;

class SettingsForm : Form
{
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunValue = "WaterReminder";

    readonly DataStore _store;

    public SettingsForm(DataStore store)
    {
        _store = store;
        var c = store.Config;

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "设置";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(400, 530);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Font = new Font("Microsoft YaHei UI", 9f);

        int y = 20;
        var interval = AddRow("提醒间隔（分钟）", c.IntervalMinutes, 10, 240, ref y);
        var snooze = AddRow("稍后再提醒（分钟）", c.SnoozeMinutes, 5, 120, ref y);
        var cup = AddRow("默认一杯（ml）", c.CupMl, 50, 1000, ref y);
        var goal = AddRow("每日目标（ml）", c.DailyGoalMl, 500, 5000, ref y);
        var start = AddRow("提醒时段开始（点）", c.ActiveStartHour, 0, 23, ref y);
        var end = AddRow("提醒时段结束（点）", c.ActiveEndHour, 1, 24, ref y);

        var fullscreen = new CheckBox
        {
            Text = "全屏（游戏/视频）时不弹气泡，延后提醒",
            Checked = c.DeferWhenFullscreen,
            AutoSize = true,
            Location = new Point(20, y),
        };
        y += 30;
        var autostart = new CheckBox
        {
            Text = "开机自动启动",
            Checked = IsAutoStartEnabled(),
            AutoSize = true,
            Location = new Point(20, y),
        };
        y += 34;

        var bedtime = new CheckBox
        {
            Text = "早睡提醒（只提醒，不做记录）",
            Checked = c.BedtimeEnabled,
            AutoSize = true,
            Location = new Point(20, y),
        };
        y += 32;
        var bedWork = AddTimeRow("工作日晚上（周日～周四）", c.BedtimeWorkday, ref y);
        var bedRest = AddTimeRow("周末晚上（周五、周六）", c.BedtimeRestday, ref y);

        var save = PaperTheme.PaperButton("保存", new Point(20, y), new Size(120, 32), accent: true);
        save.Click += (_, _) =>
        {
            c.IntervalMinutes = (int)interval.Value;
            c.SnoozeMinutes = (int)snooze.Value;
            c.CupMl = (int)cup.Value;
            c.DailyGoalMl = (int)goal.Value;
            c.ActiveStartHour = (int)start.Value;
            c.ActiveEndHour = (int)end.Value;
            c.DeferWhenFullscreen = fullscreen.Checked;
            c.BedtimeEnabled = bedtime.Checked;
            if (TimeOnly.TryParse(bedWork.Text.Trim(), out var tw)) c.BedtimeWorkday = tw.ToString("HH:mm");
            if (TimeOnly.TryParse(bedRest.Text.Trim(), out var tr)) c.BedtimeRestday = tr.ToString("HH:mm");
            _store.SaveConfig();
            SetAutoStart(autostart.Checked);
            Close();
        };

        y += 46;
        var aboutLine = new Label
        {
            Text = $"早睡早起多喝水 v{Application.ProductVersion}",
            ForeColor = PaperTheme.InkLight,
            AutoSize = true,
            Location = new Point(20, y),
        };
        var authorLine = new LinkLabel
        {
            Text = "作者微信公众号：爱玩的果果",
            AutoSize = true,
            Location = new Point(20, y + 22),
            LinkColor = PaperTheme.AccentInk,
            ActiveLinkColor = PaperTheme.Accent,
            LinkBehavior = LinkBehavior.HoverUnderline,
            LinkArea = new LinkArea(8, 5),
        };
        authorLine.LinkClicked += (_, _) => Process.Start(new ProcessStartInfo(
            "https://mp.weixin.qq.com/s/1P3FnoMtXusX0BdnAk4lGA") { UseShellExecute = true });

        var update = PaperTheme.PaperButton("检查更新", new Point(272, y + 4), new Size(104, 30));
        update.Click += async (_, _) =>
        {
            update.Enabled = false;
            update.Text = "检查中…";
            try
            {
                var latest = await UpdateChecker.FetchLatestAsync();
                if (latest == null)
                {
                    MessageBox.Show(this, "没取到版本信息，稍后再试试。", "检查更新",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else if (latest.Latest > UpdateChecker.Current)
                {
                    if (MessageBox.Show(this,
                        $"发现新版本 v{latest.Latest}（当前 v{UpdateChecker.Current}）。\n\n现在打开下载链接吗？",
                        "检查更新", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo(latest.ZipUrl ?? latest.Page) { UseShellExecute = true });
                    }
                }
                else
                {
                    MessageBox.Show(this, $"当前已是最新版本 v{UpdateChecker.Current}。", "检查更新",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch
            {
                if (MessageBox.Show(this,
                    "检查更新失败（可能是网络问题）。\n\n要直接打开发布页看看吗？",
                    "检查更新", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    Process.Start(new ProcessStartInfo(UpdateChecker.ReleasesPage) { UseShellExecute = true });
                }
            }
            finally
            {
                update.Text = "检查更新";
                update.Enabled = true;
            }
        };
        Paint += (_, e) =>
        {
            using var pen = new Pen(PaperTheme.Border);
            e.Graphics.DrawLine(pen, 20, aboutLine.Top - 10, ClientSize.Width - 20, aboutLine.Top - 10);
        };

        Controls.AddRange(new Control[] { fullscreen, autostart, bedtime, save, aboutLine, authorLine, update });
        PaperTheme.PaperWindow(this, "设置");
    }

    TextBox AddTimeRow(string label, string value, ref int y)
    {
        var lbl = new Label { Text = label, AutoSize = true, Location = new Point(20, y + 4) };
        var tb = new TextBox
        {
            Text = value,
            Location = new Point(280, y),
            Size = new Size(80, 28),
            TextAlign = HorizontalAlignment.Center,
            BackColor = PaperTheme.Field,
            ForeColor = PaperTheme.Ink,
            BorderStyle = BorderStyle.FixedSingle,
        };
        Controls.Add(lbl);
        Controls.Add(tb);
        y += 36;
        return tb;
    }

    NumericUpDown AddRow(string label, int value, int min, int max, ref int y)
    {
        var lbl = new Label { Text = label, AutoSize = true, Location = new Point(20, y + 4) };
        var num = new NumericUpDown
        {
            Minimum = min, Maximum = max,
            Value = Math.Clamp(value, min, max),
            Location = new Point(250, y),
            Size = new Size(110, 28),
            BackColor = PaperTheme.Field,
            ForeColor = PaperTheme.Ink,
        };
        Controls.Add(lbl);
        Controls.Add(num);
        y += 36;
        return num;
    }

    public static bool IsAutoStartEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(RunValue) != null;
    }

    public static void SetAutoStart(bool enable)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (enable)
            key.SetValue(RunValue, $"\"{Application.ExecutablePath}\"");
        else
            key.DeleteValue(RunValue, false);
    }
}
