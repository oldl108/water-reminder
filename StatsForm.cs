namespace WaterReminder;

/// <summary>统计窗口：今日概况 + 最近 7 天柱状图 + 今日明细。</summary>
class StatsForm : Form
{
    readonly DataStore _store;
    readonly Panel _chart = new();
    readonly ListBox _list = new();
    readonly Label _summary = new();

    public StatsForm(DataStore store)
    {
        _store = store;
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "喝水统计";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(440, 400);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Font = new Font("Microsoft YaHei UI", 9f);
        BackColor = Color.White;

        _summary.AutoSize = true;
        _summary.Location = new Point(20, 16);
        _summary.Font = new Font("Microsoft YaHei UI", 10f);

        _chart.Location = new Point(20, 70);
        _chart.Size = new Size(400, 170);
        _chart.Paint += DrawChart;

        var listLabel = new Label
        {
            Text = "今日记录",
            ForeColor = Color.FromArgb(110, 110, 110),
            AutoSize = true,
            Location = new Point(20, 252),
        };
        _list.Location = new Point(20, 276);
        _list.Size = new Size(400, 100);
        _list.BorderStyle = BorderStyle.FixedSingle;

        Controls.AddRange(new Control[] { _summary, _chart, listLabel, _list });
        Load += (_, _) => RefreshStats();
    }

    void RefreshStats()
    {
        var today = _store.Today();
        int goal = _store.Config.DailyGoalMl;
        _summary.Text = $"今日 {today.TotalMl} / {goal} ml    站起活动 {today.Stands} 次";

        _list.Items.Clear();
        foreach (var m in Enumerable.Reverse(today.Meds))
            _list.Items.Add($"{m.TakenAt}    💊 {m.Name}（{m.Sched} 那顿）");
        foreach (var e in Enumerable.Reverse(today.Water))
            _list.Items.Add($"{e.Time}    {e.Ml} ml");
        if (today.Water.Count == 0 && today.Meds.Count == 0) _list.Items.Add("（还没有记录）");

        _chart.Invalidate();
    }

    void DrawChart(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        int goal = Math.Max(1, _store.Config.DailyGoalMl);
        int w = _chart.Width, h = _chart.Height, labelH = 22;
        int barW = 34, gap = (w - 7 * barW) / 8;

        int maxMl = goal;
        var days = new (string Label, int Ml, bool IsToday)[7];
        for (int i = 0; i < 7; i++)
        {
            var date = DateTime.Today.AddDays(i - 6);
            var rec = _store.Day(date);
            int ml = rec?.TotalMl ?? 0;
            maxMl = Math.Max(maxMl, ml);
            days[i] = (date.ToString("M/d"), ml, i == 6);
        }

        // 目标线
        int goalY = labelH + (int)((h - labelH - 20) * (1 - (double)goal / maxMl));
        using var dashPen = new Pen(Color.FromArgb(200, 200, 200)) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
        g.DrawLine(dashPen, 0, goalY, w, goalY);
        g.DrawString($"目标 {goal}", Font, Brushes.Gray, 0, goalY - 18);

        for (int i = 0; i < 7; i++)
        {
            int x = gap + i * (barW + gap);
            int barH = (int)((h - labelH - 20) * (double)days[i].Ml / maxMl);
            int y = h - 20 - barH;
            bool met = days[i].Ml >= goal;
            using var brush = new SolidBrush(met
                ? Color.FromArgb(55, 138, 221)
                : Color.FromArgb(181, 212, 244));
            if (barH > 0) g.FillRectangle(brush, x, y, barW, barH);
            var labelFont = days[i].IsToday ? new Font(Font, FontStyle.Bold) : Font;
            var size = g.MeasureString(days[i].Label, labelFont);
            g.DrawString(days[i].Label, labelFont, Brushes.Gray, x + (barW - size.Width) / 2, h - 18);
        }
    }
}
