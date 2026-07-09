using System.Drawing.Drawing2D;

namespace WaterReminder;

/// <summary>纸张质感主题：米白纸底、墨色文字、黛蓝点缀、细噪点纹理。</summary>
static class PaperTheme
{
    public static readonly Color Bg = Color.FromArgb(250, 246, 238);        // 米白纸
    public static readonly Color BgDeep = Color.FromArgb(243, 236, 222);    // 按钮纸色
    public static readonly Color Field = Color.FromArgb(253, 251, 245);     // 输入框
    public static readonly Color Border = Color.FromArgb(214, 204, 185);    // 纸边
    public static readonly Color Ink = Color.FromArgb(62, 53, 41);          // 墨
    public static readonly Color InkLight = Color.FromArgb(140, 128, 108);  // 淡墨
    public static readonly Color Accent = Color.FromArgb(91, 125, 163);     // 黛蓝
    public static readonly Color AccentPale = Color.FromArgb(199, 212, 226);// 淡黛蓝
    public static readonly Color AccentBg = Color.FromArgb(231, 237, 244);  // 黛蓝纸底
    public static readonly Color AccentBorder = Color.FromArgb(180, 197, 216);
    public static readonly Color AccentInk = Color.FromArgb(47, 75, 104);   // 黛蓝墨
    public static readonly Color Track = Color.FromArgb(235, 228, 213);     // 进度条底

    public static void Style(Form f)
    {
        f.BackColor = Bg;
        f.ForeColor = Ink;
    }

    /// <summary>纸纹：稀疏的暖色细噪点，随机但对同一尺寸稳定（避免重绘时闪烁）。</summary>
    public static void PaintGrain(Control c, Graphics g)
    {
        var rnd = new Random(c.Width * 397 ^ c.Height);
        int n = c.Width * c.Height / 240;
        using var dark = new SolidBrush(Color.FromArgb(13, 120, 100, 70));
        using var light = new SolidBrush(Color.FromArgb(9, 90, 78, 58));
        for (int i = 0; i < n; i++)
            g.FillRectangle(rnd.Next(2) == 0 ? dark : light,
                rnd.Next(c.Width), rnd.Next(c.Height), rnd.Next(1, 3), 1);
    }

    static GraphicsPath RoundPath(Rectangle r, int radius)
    {
        int d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d - 1, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d - 1, r.Bottom - d - 1, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d - 1, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    /// <summary>无边框窗体的圆角 + 纸边描线 + 纸纹，一次配齐。</summary>
    public static void MakePaperCard(Form f, int radius = 10)
    {
        void Apply()
        {
            using var path = RoundPath(f.ClientRectangle, radius);
            f.Region = new Region(path);
        }
        f.Load += (_, _) => Apply();
        f.Resize += (_, _) => Apply();
        f.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            PaintGrain(f, e.Graphics);
            using var path = RoundPath(f.ClientRectangle, radius);
            using var pen = new Pen(Border, 1.5f);
            e.Graphics.DrawPath(pen, path);
        };
    }

    public static Button PaperButton(string text, Point loc, Size size, bool accent = false)
    {
        var b = new Button
        {
            Text = text,
            Location = loc,
            Size = size,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            BackColor = accent ? AccentBg : BgDeep,
            ForeColor = accent ? AccentInk : Ink,
        };
        b.FlatAppearance.BorderColor = accent ? AccentBorder : Border;
        b.FlatAppearance.MouseOverBackColor = accent
            ? Color.FromArgb(219, 228, 238)
            : Color.FromArgb(236, 228, 211);
        return b;
    }
}
