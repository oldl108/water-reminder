namespace WaterReminder;

/// <summary>点击托盘图标弹出的快捷记录面板，失焦自动收起。</summary>
class PanelForm : Form
{
    readonly DataStore _store;
    readonly Action _onChanged;
    readonly Label _progressText = new();
    readonly Panel _progressBar = new();
    readonly Panel _progressFill = new();
    readonly Label _standText = new();

    public PanelForm(DataStore store, Action onChanged)
    {
        _store = store;
        _onChanged = onChanged;

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = Color.White;
        Size = new Size(280, 218);
        Font = new Font("Microsoft YaHei UI", 9f);

        var header = new Label
        {
            Text = "今日饮水",
            ForeColor = Color.FromArgb(110, 110, 110),
            AutoSize = true,
            Location = new Point(16, 14),
        };
        _progressText.Font = new Font("Microsoft YaHei UI", 12f, FontStyle.Bold);
        _progressText.AutoSize = true;
        _progressText.Location = new Point(120, 10);
        _progressText.TextAlign = ContentAlignment.MiddleRight;

        _progressBar.Location = new Point(16, 44);
        _progressBar.Size = new Size(248, 8);
        _progressBar.BackColor = Color.FromArgb(240, 240, 240);
        _progressFill.Location = new Point(0, 0);
        _progressFill.Size = new Size(0, 8);
        _progressFill.BackColor = Color.FromArgb(55, 138, 221);
        _progressBar.Controls.Add(_progressFill);

        int y = 66;
        int[] amounts = { 100, 200, 300 };
        for (int i = 0; i < amounts.Length; i++)
        {
            int ml = amounts[i];
            var b = MakeButton($"+{ml}", new Point(16 + i * 62, y), new Size(56, 30));
            b.Click += (_, _) => { _store.AddWater(ml); Refresh_(); _onChanged(); };
            Controls.Add(b);
        }
        var bCustom = MakeButton("…", new Point(16 + 3 * 62, y), new Size(56, 30));
        bCustom.Click += (_, _) => CustomAmount();
        Controls.Add(bCustom);

        _standText.AutoSize = true;
        _standText.Location = new Point(16, y + 44);
        _standText.ForeColor = Color.FromArgb(110, 110, 110);

        var bStand = MakeButton("站起活动 +1", new Point(160, y + 38), new Size(104, 28));
        bStand.Click += (_, _) => { _store.AddStand(); Refresh_(); };
        Controls.Add(bStand);

        var bPause = MakeButton("暂停提醒 1 小时", new Point(16, y + 76), new Size(120, 28));
        bPause.Click += (_, _) => { PauseRequested?.Invoke(); Hide(); };
        var bStats = MakeButton("统计", new Point(144, y + 76), new Size(56, 28));
        bStats.Click += (_, _) => { StatsRequested?.Invoke(); Hide(); };
        var bSettings = MakeButton("设置", new Point(208, y + 76), new Size(56, 28));
        bSettings.Click += (_, _) => { SettingsRequested?.Invoke(); Hide(); };

        Controls.AddRange(new Control[] { header, _progressText, _progressBar, _standText, bPause, bStats, bSettings });

        Paint += (_, e) => e.Graphics.DrawRectangle(
            new Pen(Color.FromArgb(210, 210, 210)), 0, 0, Width - 1, Height - 1);

        Deactivate += (_, _) => Hide();
    }

    public event Action? PauseRequested;
    public event Action? StatsRequested;
    public event Action? SettingsRequested;

    static Button MakeButton(string text, Point loc, Size size)
    {
        var b = new Button
        {
            Text = text,
            Location = loc,
            Size = size,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
        };
        b.FlatAppearance.BorderColor = Color.FromArgb(220, 220, 220);
        return b;
    }

    void CustomAmount()
    {
        using var dlg = new Form
        {
            Text = "记录饮水量",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
            Size = new Size(240, 130),
            MaximizeBox = false,
            MinimizeBox = false,
            Font = Font,
        };
        var num = new NumericUpDown
        {
            Minimum = 10, Maximum = 2000, Value = _store.Config.CupMl, Increment = 50,
            Location = new Point(16, 16), Size = new Size(120, 28),
        };
        var lbl = new Label { Text = "ml", AutoSize = true, Location = new Point(142, 19) };
        var ok = new Button { Text = "记录", DialogResult = DialogResult.OK, Location = new Point(16, 54), Size = new Size(90, 28) };
        dlg.Controls.AddRange(new Control[] { num, lbl, ok });
        dlg.AcceptButton = ok;
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _store.AddWater((int)num.Value);
            Refresh_();
            _onChanged();
        }
    }

    void Refresh_()
    {
        var today = _store.Today();
        int goal = _store.Config.DailyGoalMl;
        _progressText.Text = $"{today.TotalMl} / {goal} ml";
        _progressText.Location = new Point(Width - 16 - _progressText.PreferredWidth, 10);
        _progressFill.Width = Math.Min(_progressBar.Width,
            (int)(_progressBar.Width * Math.Min(1.0, (double)today.TotalMl / Math.Max(1, goal))));
        _standText.Text = $"今日站起活动：{today.Stands} 次";
    }

    public void ShowNearTray()
    {
        Refresh_();
        var wa = Screen.PrimaryScreen!.WorkingArea;
        Location = new Point(wa.Right - Width - 8, wa.Bottom - Height - 8);
        Show();
        Activate();
    }
}
