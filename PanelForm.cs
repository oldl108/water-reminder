namespace WaterReminder;

/// <summary>点击托盘图标弹出的快捷记录面板，失焦自动收起。保持极简：进度 + 快捷记录 + 一行状态。</summary>
class PanelForm : Form
{
    readonly DataStore _store;
    readonly Action _onChanged;
    readonly Label _progressText = new();
    readonly Panel _progressBar = new();
    readonly Panel _progressFill = new();
    readonly Label _statusText = new();
    readonly Label _nextMedText = new();

    public PanelForm(DataStore store, Action onChanged)
    {
        _store = store;
        _onChanged = onChanged;

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        PaperTheme.Style(this);
        Size = new Size(320, 176);
        Font = new Font("Microsoft YaHei UI", 9f);

        var header = new Label
        {
            Text = "今日饮水",
            ForeColor = PaperTheme.InkLight,
            AutoSize = true,
            Location = new Point(16, 14),
        };
        _progressText.Font = new Font("Microsoft YaHei UI", 12f, FontStyle.Bold);
        _progressText.ForeColor = PaperTheme.Ink;
        _progressText.AutoSize = true;
        _progressText.Location = new Point(120, 10);

        _progressBar.Location = new Point(16, 44);
        _progressBar.Size = new Size(288, 8);
        _progressBar.BackColor = PaperTheme.Track;
        _progressFill.Location = new Point(0, 0);
        _progressFill.Size = new Size(0, 8);
        _progressFill.BackColor = PaperTheme.Accent;
        _progressBar.Controls.Add(_progressFill);

        int y = 66;
        int[] amounts = { 100, 200, 300 };
        for (int i = 0; i < amounts.Length; i++)
        {
            int ml = amounts[i];
            var b = MakeButton($"+{ml}", new Point(16 + i * 74, y), new Size(66, 30));
            b.Click += (_, _) => { _store.AddWater(ml); Refresh_(); _onChanged(); };
            Controls.Add(b);
        }
        var bCustom = MakeButton("…", new Point(16 + 3 * 74, y), new Size(66, 30));
        bCustom.Click += (_, _) => CustomAmount();
        Controls.Add(bCustom);

        _statusText.AutoSize = true;
        _statusText.Location = new Point(16, y + 44);
        _statusText.ForeColor = PaperTheme.InkLight;
        _statusText.Cursor = Cursors.Hand;
        _statusText.Click += (_, _) => { _store.AddStand(); Refresh_(); };

        _nextMedText.AutoSize = true;
        _nextMedText.Location = new Point(16, y + 68);
        _nextMedText.ForeColor = PaperTheme.AccentInk;

        Controls.AddRange(new Control[] { header, _progressText, _progressBar, _statusText, _nextMedText });

        PaperTheme.MakePaperCard(this);

        Deactivate += (_, _) => Hide();
    }

    static Button MakeButton(string text, Point loc, Size size) =>
        PaperTheme.PaperButton(text, loc, size);

    void CustomAmount()
    {
        using var dlg = new Form
        {
            AutoScaleDimensions = new SizeF(96F, 96F),
            AutoScaleMode = AutoScaleMode.Dpi,
            Text = "记录饮水量",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
            ClientSize = new Size(240, 100),
            MaximizeBox = false,
            MinimizeBox = false,
            Font = Font,
        };
        var num = new NumericUpDown
        {
            Minimum = 10, Maximum = 2000, Value = _store.Config.CupMl, Increment = 50,
            Location = new Point(16, 16), Size = new Size(120, 28),
            BackColor = PaperTheme.Field, ForeColor = PaperTheme.Ink,
        };
        var lbl = new Label { Text = "ml", AutoSize = true, Location = new Point(142, 19) };
        var ok = PaperTheme.PaperButton("记录", new Point(16, 54), new Size(90, 28), accent: true);
        ok.DialogResult = DialogResult.OK;
        dlg.Controls.AddRange(new Control[] { num, lbl, ok });
        PaperTheme.PaperWindow(dlg, "记录饮水量");
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

        int medTotal = _store.Config.Meds.Sum(m => m.Times.Count);
        string medPart = medTotal > 0 ? $" · 吃药 {today.Meds.Count}/{medTotal}" : "";
        _statusText.Text = $"站起活动 {today.Stands} 次（点我 +1）{medPart}";

        _nextMedText.Text = NextMedLine();
        _nextMedText.Visible = _nextMedText.Text.Length > 0;
    }

    string NextMedLine()
    {
        var now = TimeOnly.FromDateTime(DateTime.Now);
        (string Name, TimeOnly At)? next = null;
        foreach (var med in _store.Config.Meds)
            foreach (var t in med.Times)
            {
                if (!TimeOnly.TryParse(t, out var tod)) continue;
                if (_store.IsMedTaken(med.Name, t)) continue;
                if (next == null || tod < next.Value.At) next = (med.Name, tod);
            }
        if (next == null) return "";
        return next.Value.At <= now
            ? $"待吃：{next.Value.Name}（{next.Value.At:HH:mm} 那顿）"
            : $"下一顿：{next.Value.Name} {next.Value.At:HH:mm}";
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
