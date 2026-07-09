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

        Text = "设置";
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(340, 400);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Font = new Font("Microsoft YaHei UI", 9f);
        BackColor = Color.White;

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
        y += 36;

        var save = new Button
        {
            Text = "保存",
            Size = new Size(120, 32),
            Location = new Point(20, y),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(230, 241, 251),
            ForeColor = Color.FromArgb(12, 68, 124),
        };
        save.FlatAppearance.BorderColor = Color.FromArgb(181, 212, 244);
        save.Click += (_, _) =>
        {
            c.IntervalMinutes = (int)interval.Value;
            c.SnoozeMinutes = (int)snooze.Value;
            c.CupMl = (int)cup.Value;
            c.DailyGoalMl = (int)goal.Value;
            c.ActiveStartHour = (int)start.Value;
            c.ActiveEndHour = (int)end.Value;
            c.DeferWhenFullscreen = fullscreen.Checked;
            _store.SaveConfig();
            SetAutoStart(autostart.Checked);
            Close();
        };

        Controls.AddRange(new Control[] { fullscreen, autostart, save });
    }

    NumericUpDown AddRow(string label, int value, int min, int max, ref int y)
    {
        var lbl = new Label { Text = label, AutoSize = true, Location = new Point(20, y + 4) };
        var num = new NumericUpDown
        {
            Minimum = min, Maximum = max,
            Value = Math.Clamp(value, min, max),
            Location = new Point(200, y),
            Size = new Size(100, 28),
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
