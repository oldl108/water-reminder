namespace WaterReminder;

/// <summary>
/// 右下角提醒气泡。核心约束：永远不抢焦点（WS_EX_NOACTIVATE），
/// 不出现在任务栏和 Alt+Tab（WS_EX_TOOLWINDOW），置顶但可点击。
/// </summary>
class ToastForm : Form
{
    public event Action? Confirmed;
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

    public ToastForm(string titleText, string bodyText, string okText)
    {
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        PaperTheme.Style(this);
        Size = new Size(340, 116);
        Font = new Font("Microsoft YaHei UI", 9f);

        var title = new Label
        {
            Text = titleText,
            Font = new Font("Microsoft YaHei UI", 10.5f, FontStyle.Bold),
            ForeColor = PaperTheme.Ink,
            AutoSize = true,
            Location = new Point(14, 12),
        };
        var body = new Label
        {
            Text = bodyText,
            ForeColor = PaperTheme.InkLight,
            AutoSize = true,
            Location = new Point(15, 40),
        };
        var btnDrank = PaperTheme.PaperButton(okText, new Point(14, 72), new Size(190, 32), accent: true);
        btnDrank.Click += (_, _) => { Confirmed?.Invoke(); CloseToast(); };

        var btnLater = PaperTheme.PaperButton("稍后", new Point(212, 72), new Size(114, 32));
        btnLater.ForeColor = PaperTheme.InkLight;
        btnLater.Click += (_, _) => { Snoozed?.Invoke(); CloseToast(); };

        Controls.AddRange(new Control[] { title, body, btnDrank, btnLater });

        PaperTheme.MakePaperCard(this);

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
        var wa = Screen.PrimaryScreen!.WorkingArea;
        Location = new Point(wa.Right - Width - 16, wa.Bottom - Height - 16);
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
