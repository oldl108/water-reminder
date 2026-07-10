namespace WaterReminder;

/// <summary>托盘常驻主逻辑：计时、场景判断、弹提醒。</summary>
class TrayContext : ApplicationContext
{
    readonly DataStore _store = new();
    readonly NotifyIcon _tray = new();
    readonly System.Windows.Forms.Timer _tick = new();
    readonly PanelForm _panel;

    DateTime _nextReminderAt;
    DateTime _sitStartAt = DateTime.Now;
    DateTime _pausedUntil = DateTime.MinValue;
    bool _wasAway;
    ToastForm? _toast;
    StatsForm? _stats;
    MedsForm? _meds;
    readonly Dictionary<string, DateTime> _medNagAt = new();
    string? _bedtimeAckNight;
    DateTime _bedtimeNagAt = DateTime.MinValue;

    public TrayContext()
    {
        _panel = new PanelForm(_store, OnWaterRecorded);

        _tray.Icon = MakeDropIcon();
        _tray.Text = "早睡早起多喝水";
        _tray.Visible = true;
        _tray.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) _panel.ShowNearTray(); };

        var menu = new ContextMenuStrip();
        menu.Items.Add("记录 / 查看面板", null, (_, _) => _panel.ShowNearTray());
        menu.Items.Add("统计", null, (_, _) => ShowStats());
        menu.Items.Add("吃药提醒…", null, (_, _) => ShowMeds());
        menu.Items.Add("设置", null, (_, _) => ShowSettings());
        menu.Items.Add(new ToolStripSeparator());
        var pauseItem = new ToolStripMenuItem("暂停提醒 1 小时");
        pauseItem.Click += (_, _) =>
        {
            if (DateTime.Now < _pausedUntil)
            {
                _pausedUntil = DateTime.MinValue;
                ScheduleNext();
                Notify("提醒已恢复");
            }
            else PauseFor(TimeSpan.FromHours(1));
        };
        menu.Items.Add(pauseItem);
        menu.Items.Add("测试提醒（5 秒后弹出）", null, (_, _) =>
        {
            var t = new System.Windows.Forms.Timer { Interval = 5000 };
            t.Tick += (_, _) =>
            {
                t.Dispose();
                if (_toast == null || _toast.IsDisposed) ShowToast();
            };
            t.Start();
        });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => Exit());
        menu.Opening += (_, _) => pauseItem.Text = DateTime.Now < _pausedUntil
            ? $"恢复提醒（已暂停到 {_pausedUntil:HH:mm}）"
            : "暂停提醒 1 小时";
        _tray.ContextMenuStrip = menu;

        ScheduleNext();

        _tick.Interval = 30_000;
        _tick.Tick += (_, _) => OnTick();
        _tick.Start();
    }

    void OnTick()
    {
        var now = DateTime.Now;
        var idle = NativeMethods.GetIdleTime();

        // 离开电脑视为已活动：回来后久坐计时和提醒都重置
        if (idle.TotalMinutes >= _store.Config.IdleResetMinutes)
        {
            _wasAway = true;
            return;
        }
        if (_wasAway)
        {
            _wasAway = false;
            _sitStartAt = now;
            ScheduleNext();
            return;
        }

        if (_toast != null && !_toast.IsDisposed) return;

        // 全屏延后：每 30 秒重查一次，退出全屏后很快补弹
        if (_store.Config.DeferWhenFullscreen && NativeMethods.IsForegroundFullscreen())
            return;

        // 吃药提醒优先，且无视暂停和提醒时段
        if (CheckMeds(now)) return;

        if (now < _pausedUntil) return;

        // 早睡提醒在提醒时段之外（深夜）也要生效
        if (CheckBedtime(now)) return;

        if (now < _nextReminderAt) return;
        if (!InActiveHours(now)) return;

        ShowToast();
    }

    /// <summary>
    /// 早睡提醒：周日～周四晚用工作日时间，周五、周六晚用周末时间
    /// （按"第二天要不要上班"区分）。只提醒不记录，确认后当晚不再弹，
    /// 忽略则每半小时温和再催，凌晨 4 点后自动停。
    /// </summary>
    bool CheckBedtime(DateTime now)
    {
        var c = _store.Config;
        if (!c.BedtimeEnabled) return false;

        var nightDate = now.Hour < 12 ? DateTime.Today.AddDays(-1) : DateTime.Today;
        string nightKey = nightDate.ToString("yyyy-MM-dd");
        if (_bedtimeAckNight == nightKey) return false;

        bool restNight = nightDate.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday;
        if (!TimeOnly.TryParse(restNight ? c.BedtimeRestday : c.BedtimeWorkday, out var bt)) return false;

        var bedAt = nightDate.Add(bt.ToTimeSpan());
        var endAt = nightDate.AddDays(1).AddHours(4);
        if (now < bedAt || now >= endAt) return false;
        if (now < _bedtimeNagAt) return false;

        _store.Log("弹出早睡提醒");
        _toast = new ToastForm("🌙 该睡觉了", $"已经 {now:HH:mm} 了，放下手头的事，早点休息", "知道了，这就睡");
        _toast.Confirmed += () => { _bedtimeAckNight = nightKey; _store.Log("早睡提醒已确认"); };
        _toast.Snoozed += () => _bedtimeNagAt = DateTime.Now.AddMinutes(c.BedtimeNagMinutes);
        _toast.FormClosed += (_, _) => _toast = null;
        _toast.ShowToast();
        return true;
    }

    /// <summary>有到点没吃的药就弹提醒；一次只弹一条。</summary>
    bool CheckMeds(DateTime now)
    {
        foreach (var med in _store.Config.Meds)
        {
            foreach (var t in med.Times)
            {
                if (!TimeOnly.TryParse(t, out var tod)) continue;
                if (now < DateTime.Today.Add(tod.ToTimeSpan())) continue;
                if (_store.IsMedTaken(med.Name, t)) continue;
                string key = $"{med.Name}|{t}";
                if (_medNagAt.TryGetValue(key, out var nagAt) && now < nagAt) continue;

                _store.Log($"弹出吃药提醒：{med.Name} {t}");
                _toast = new ToastForm("💊 该吃药了", $"{med.Name}（{t} 这一顿）", "已吃了");
                _toast.Confirmed += () => { _store.AddMedTaken(med.Name, t); _medNagAt.Remove(key); _store.Log($"已确认吃药：{med.Name} {t}"); };
                _toast.Snoozed += () => _medNagAt[key] = DateTime.Now.AddMinutes(_store.Config.MedNagMinutes);
                _toast.FormClosed += (_, _) => _toast = null;
                _toast.ShowToast();
                return true;
            }
        }
        return false;
    }

    bool InActiveHours(DateTime now)
    {
        int h = now.Hour;
        int start = _store.Config.ActiveStartHour, end = _store.Config.ActiveEndHour;
        return start <= end ? h >= start && h < end : h >= start || h < end;
    }

    void ShowToast()
    {
        int sitMinutes = (int)(DateTime.Now - _sitStartAt).TotalMinutes;
        string body = sitMinutes > 0
            ? $"已经坐了 {sitMinutes} 分钟，顺便站起来动一动"
            : "喝口水，顺便站起来动一动";
        _store.Log($"弹出喝水提醒（已坐 {sitMinutes} 分钟）");
        _toast = new ToastForm("💧 该喝口水了", body, $"已喝 +{_store.Config.CupMl}ml");
        _toast.Confirmed += () =>
        {
            _store.AddWater(_store.Config.CupMl);
            _store.AddStand();          // 起身接水，顺便算一次活动
            _sitStartAt = DateTime.Now;
            ScheduleNext();
        };
        _toast.Snoozed += () => _nextReminderAt = DateTime.Now.AddMinutes(_store.Config.SnoozeMinutes);
        _toast.FormClosed += (_, _) => _toast = null;
        _toast.ShowToast();
    }

    void OnWaterRecorded()
    {
        // 手动记录后没必要马上再提醒
        if (_nextReminderAt < DateTime.Now.AddMinutes(15))
            ScheduleNext();
    }

    void ScheduleNext() => _nextReminderAt = DateTime.Now.AddMinutes(_store.Config.IntervalMinutes);

    void PauseFor(TimeSpan span)
    {
        _pausedUntil = DateTime.Now.Add(span);
        Notify($"提醒已暂停到 {_pausedUntil:HH:mm}");
    }

    void Notify(string text)
    {
        _tray.BalloonTipTitle = "早睡早起多喝水";
        _tray.BalloonTipText = text;
        _tray.ShowBalloonTip(2000);
    }

    void ShowStats()
    {
        if (_stats == null || _stats.IsDisposed) _stats = new StatsForm(_store);
        _stats.Show();
        _stats.Activate();
    }

    void ShowMeds()
    {
        if (_meds == null || _meds.IsDisposed) _meds = new MedsForm(_store);
        _meds.Show();
        _meds.Activate();
    }

    void ShowSettings()
    {
        using var dlg = new SettingsForm(_store);
        dlg.ShowDialog();
        ScheduleNext();
    }

    static Icon MakeDropIcon()
    {
        using var bmp = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(Color.FromArgb(55, 138, 221));
        g.FillEllipse(brush, 6, 12, 20, 18);
        g.FillPolygon(brush, new[] { new Point(16, 1), new Point(7, 18), new Point(25, 18) });
        return Icon.FromHandle(bmp.GetHicon());
    }

    void Exit()
    {
        _tray.Visible = false;
        _tray.Dispose();
        _tick.Dispose();
        Application.Exit();
    }
}
