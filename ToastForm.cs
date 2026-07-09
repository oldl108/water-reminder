namespace WaterReminder;

/// <summary>
/// 右下角提醒气泡。核心约束：永远不抢焦点（WS_EX_NOACTIVATE），
/// 不出现在任务栏和 Alt+Tab（WS_EX_TOOLWINDOW），置顶但可点击。
/// </summary>
class ToastForm : Form
{
    public event Action? Drank;
    public event Action? Snoozed;

    readonly System.Windows.Forms.Timer _lifeTimer = new();
    readonly System.Windows.Forms.Timer _fadeTimer = new();
    bool _fadingOut;

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= NativeMethods.WS_EX_NOACTIVATE
                        | NativeMethods.WS_EX_TOOLWINDOW
                        | NativeMethods.WS_EX_TOPMOST;
            return cp;
        }
    }

    public ToastForm(int sitMinutes, int cupMl)
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = Color.White;
        Size = new Size(300, 108);
        Font = new Font("Microsoft YaHei UI", 9f);

        var wa = Screen.PrimaryScreen!.WorkingArea;
        Location = new Point(wa.Right - Width - 16, wa.Bottom - Height - 16);

        var title = new Label
        {
            Text = "💧 该喝口水了",
            Font = new Font("Microsoft YaHei UI", 10.5f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(14, 12),
        };
        var body = new Label
        {
            Text = sitMinutes > 0
                ? $"已经坐了 {sitMinutes} 分钟，顺便站起来动一动"
                : "喝口水，顺便站起来动一动",
            ForeColor = Color.FromArgb(110, 110, 110),
            AutoSize = true,
            Location = new Point(15, 38),
        };
        var btnDrank = new Button
        {
            Text = $"已喝 +{cupMl}ml",
            Size = new Size(170, 30),
            Location = new Point(14, 64),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(230, 241, 251),
            ForeColor = Color.FromArgb(12, 68, 124),
            Cursor = Cursors.Hand,
        };
        btnDrank.FlatAppearance.BorderColor = Color.FromArgb(181, 212, 244);
        btnDrank.Click += (_, _) => { Drank?.Invoke(); CloseToast(); };

        var btnLater = new Button
        {
            Text = "稍后",
            Size = new Size(96, 30),
            Location = new Point(190, 64),
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.FromArgb(110, 110, 110),
            Cursor = Cursors.Hand,
        };
        btnLater.FlatAppearance.BorderColor = Color.FromArgb(220, 220, 220);
        btnLater.Click += (_, _) => { Snoozed?.Invoke(); CloseToast(); };

        Controls.AddRange(new Control[] { title, body, btnDrank, btnLater });

        Paint += (_, e) => e.Graphics.DrawRectangle(
            new Pen(Color.FromArgb(210, 210, 210)), 0, 0, Width - 1, Height - 1);

        Opacity = 0;
        _fadeTimer.Interval = 30;
        _fadeTimer.Tick += (_, _) =>
        {
            if (_fadingOut)
            {
                Opacity -= 0.08;
                if (Opacity <= 0) { _fadeTimer.Stop(); Close(); }
            }
            else
            {
                Opacity += 0.12;
                if (Opacity >= 0.97) { Opacity = 0.97; _fadeTimer.Stop(); }
            }
        };

        _lifeTimer.Interval = 10_000;
        _lifeTimer.Tick += (_, _) => { Snoozed?.Invoke(); CloseToast(); };

        // 鼠标悬停时不倒计时，移开后重新计时
        MouseEnter += (_, _) => _lifeTimer.Stop();
        MouseLeave += (_, _) => { if (!ClientRectangle.Contains(PointToClient(Cursor.Position))) _lifeTimer.Start(); };
    }

    public void ShowToast()
    {
        Show();
        _fadeTimer.Start();
        _lifeTimer.Start();
    }

    void CloseToast()
    {
        _lifeTimer.Stop();
        _fadingOut = true;
        _fadeTimer.Start();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { _lifeTimer.Dispose(); _fadeTimer.Dispose(); }
        base.Dispose(disposing);
    }
}
