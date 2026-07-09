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

    public TrayContext()
    {
        _panel = new PanelForm(_store, OnWaterRecorded);
        _panel.PauseRequested += () => PauseFor(TimeSpan.FromHours(1));
        _panel.StatsRequested += ShowStats;
        _panel.SettingsRequested += ShowSettings;

        _tray.Icon = MakeDropIcon();
        _tray.Text = "喝水提醒";
        _tray.Visible = true;
        _tray.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) _panel.ShowNearTray(); };

        var menu = new ContextMenuStrip();
        menu.Items.Add("记录 / 查看面板", null, (_, _) => _panel.ShowNearTray());
        menu.Items.Add("统计", null, (_, _) => ShowStats());
        menu.Items.Add("设置", null, (_, _) => ShowSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("暂停提醒 1 小时", null, (_, _) => PauseFor(TimeSpan.FromHours(1)));
        menu.Items.Add("恢复提醒", null, (_, _) => { _pausedUntil = DateTime.MinValue; ScheduleNext(); Notify("提醒已恢复"); });
        menu.Items.Add("立即提醒（看看效果）", null, (_, _) => { if (_toast == null || _toast.IsDisposed) ShowToast(); });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => Exit());
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

        if (now < _pausedUntil || now < _nextReminderAt) return;
        if (!InActiveHours(now)) return;
        if (_toast != null && !_toast.IsDisposed) return;

        // 全屏延后：不改 _nextReminderAt，每 30 秒重查一次，退出全屏后很快补弹
        if (_store.Config.DeferWhenFullscreen && NativeMethods.IsForegroundFullscreen())
            return;

        ShowToast();
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
        _toast = new ToastForm(sitMinutes, _store.Config.CupMl);
        _toast.Drank += () =>
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
        _tray.BalloonTipTitle = "喝水提醒";
        _tray.BalloonTipText = text;
        _tray.ShowBalloonTip(2000);
    }

    void ShowStats()
    {
        if (_stats == null || _stats.IsDisposed) _stats = new StatsForm(_store);
        _stats.Show();
        _stats.Activate();
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
