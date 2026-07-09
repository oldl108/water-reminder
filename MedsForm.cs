namespace WaterReminder;

/// <summary>吃药提醒管理：药名 + 每天固定的提醒时间（可多个）。</summary>
class MedsForm : Form
{
    readonly DataStore _store;
    readonly ListBox _list = new();

    public MedsForm(DataStore store)
    {
        _store = store;

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "吃药提醒";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(380, 300);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Font = new Font("Microsoft YaHei UI", 9f);
        BackColor = Color.White;

        var hint = new Label
        {
            Text = "到点会弹气泡提醒，点\"已吃了\"前每 10 分钟催一次",
            ForeColor = Color.FromArgb(110, 110, 110),
            AutoSize = true,
            Location = new Point(20, 14),
        };

        _list.Location = new Point(20, 42);
        _list.Size = new Size(340, 190);
        _list.BorderStyle = BorderStyle.FixedSingle;
        _list.Font = new Font("Microsoft YaHei UI", 10f);

        var bAdd = MakeButton("添加", new Point(20, 246));
        bAdd.Click += (_, _) => AddOrEdit(null);
        var bEdit = MakeButton("编辑", new Point(116, 246));
        bEdit.Click += (_, _) => { if (Selected() is { } m) AddOrEdit(m); };
        var bDel = MakeButton("删除", new Point(212, 246));
        bDel.Click += (_, _) =>
        {
            if (Selected() is { } m &&
                MessageBox.Show($"删除「{m.Name}」的提醒？", "吃药提醒",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _store.Config.Meds.Remove(m);
                _store.SaveConfig();
                RefreshList();
            }
        };

        Controls.AddRange(new Control[] { hint, _list, bAdd, bEdit, bDel });
        RefreshList();
    }

    static Button MakeButton(string text, Point loc)
    {
        var b = new Button
        {
            Text = text,
            Location = loc,
            Size = new Size(88, 30),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
        };
        b.FlatAppearance.BorderColor = Color.FromArgb(220, 220, 220);
        return b;
    }

    MedSchedule? Selected() =>
        _list.SelectedIndex >= 0 && _list.SelectedIndex < _store.Config.Meds.Count
            ? _store.Config.Meds[_list.SelectedIndex]
            : null;

    void RefreshList()
    {
        _list.Items.Clear();
        foreach (var m in _store.Config.Meds)
            _list.Items.Add($"{m.Name}  —  {string.Join("、", m.Times)}");
        if (_store.Config.Meds.Count == 0)
            _list.Items.Add("（还没有添加吃药提醒，点\"添加\"）");
    }

    void AddOrEdit(MedSchedule? existing)
    {
        using var dlg = new Form
        {
            AutoScaleDimensions = new SizeF(96F, 96F),
            AutoScaleMode = AutoScaleMode.Dpi,
            Text = existing == null ? "添加吃药提醒" : "编辑吃药提醒",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(320, 170),
            MaximizeBox = false,
            MinimizeBox = false,
            Font = Font,
        };
        var lblName = new Label { Text = "药名", AutoSize = true, Location = new Point(20, 20) };
        var tbName = new TextBox { Location = new Point(90, 16), Size = new Size(210, 28), Text = existing?.Name ?? "" };
        var lblTimes = new Label { Text = "提醒时间", AutoSize = true, Location = new Point(20, 58) };
        var tbTimes = new TextBox
        {
            Location = new Point(90, 54),
            Size = new Size(210, 28),
            Text = existing != null ? string.Join(", ", existing.Times) : "08:00, 21:00",
        };
        var lblHint = new Label
        {
            Text = "多个时间用逗号分隔，24 小时制",
            ForeColor = Color.FromArgb(110, 110, 110),
            AutoSize = true,
            Location = new Point(90, 86),
        };
        var ok = new Button
        {
            Text = "保存",
            Location = new Point(90, 118),
            Size = new Size(100, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(230, 241, 251),
            ForeColor = Color.FromArgb(12, 68, 124),
        };
        ok.FlatAppearance.BorderColor = Color.FromArgb(181, 212, 244);
        ok.Click += (_, _) =>
        {
            string name = tbName.Text.Trim();
            if (name.Length == 0)
            {
                MessageBox.Show("药名不能为空。", "吃药提醒", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var times = new List<string>();
            foreach (var raw in tbTimes.Text.Split(new[] { ',', '，', '、' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (TimeOnly.TryParse(raw.Trim(), out var t))
                    times.Add(t.ToString("HH:mm"));
                else
                {
                    MessageBox.Show($"看不懂这个时间：{raw.Trim()}\n请用 24 小时制，比如 08:00 或 21:30。",
                        "吃药提醒", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            if (times.Count == 0)
            {
                MessageBox.Show("至少填一个提醒时间。", "吃药提醒", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            times.Sort();
            if (existing == null)
                _store.Config.Meds.Add(new MedSchedule { Name = name, Times = times });
            else
            {
                existing.Name = name;
                existing.Times = times;
            }
            _store.SaveConfig();
            RefreshList();
            dlg.Close();
        };

        dlg.Controls.AddRange(new Control[] { lblName, tbName, lblTimes, tbTimes, lblHint, ok });
        dlg.AcceptButton = ok;
        dlg.ShowDialog(this);
    }
}
