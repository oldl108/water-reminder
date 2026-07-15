using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace WaterReminder;

/// <summary>用 GDI+ 绘制简洁彩色玻璃水杯图标，并打包成多尺寸 .ico。</summary>
static class IconFactory
{
    static readonly Color Paper    = Color.FromArgb(250, 246, 238);
    static readonly Color PaperBd  = Color.FromArgb(214, 204, 185);
    static readonly Color GlassBd  = Color.FromArgb(74, 127, 168);   // 杯壁描边
    static readonly Color WaterTop = Color.FromArgb(126, 190, 240);  // 浅蓝（上层）
    static readonly Color WaterBot = Color.FromArgb(74, 148, 214);   // 深蓝（下层）
    static readonly Color Surface  = Color.FromArgb(178, 220, 248);  // 水面
    static readonly Color Shine    = Color.FromArgb(255, 255, 255);

    /// <summary>杯身路径：上宽下窄的梯形杯，靠圆角描边收边。</summary>
    static GraphicsPath GlassPath(Func<float, float> U)
    {
        var p = new GraphicsPath();
        float xlT = U(84), xrT = U(172), xlB = U(103), xrB = U(153);
        float yT = U(56), yB = U(204);
        p.AddLine(xlT, yT, xrT, yT);   // 杯口
        p.AddLine(xrT, yT, xrB, yB);   // 右壁
        p.AddLine(xrB, yB, xlB, yB);   // 杯底
        p.CloseFigure();               // 左壁
        return p;
    }

    public static Bitmap Draw(int s, bool withPlate = true)
    {
        var bmp = new Bitmap(s, s);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.Clear(Color.Transparent);

        float k = s / 256f;
        float U(float v) => v * k;

        // 背景纸色圆角块
        if (withPlate)
        {
            float m = U(8), w = s - 2 * m, rr = U(48);
            using var path = new GraphicsPath();
            path.AddArc(m, m, rr, rr, 180, 90);
            path.AddArc(m + w - rr, m, rr, rr, 270, 90);
            path.AddArc(m + w - rr, m + w - rr, rr, rr, 0, 90);
            path.AddArc(m, m + w - rr, rr, rr, 90, 90);
            path.CloseFigure();
            using var bBg = new SolidBrush(Paper);
            using var pBg = new Pen(PaperBd, U(4));
            g.FillPath(bBg, path);
            g.DrawPath(pBg, path);
        }

        using var glass = GlassPath(U);

        // 水：把渐变色裁进杯身下半部分
        var oldClip = g.Clip;
        g.SetClip(glass, CombineMode.Replace);
        var waterRect = new RectangleF(U(80), U(104), U(96), U(104));
        using (var bWater = new LinearGradientBrush(waterRect, WaterTop, WaterBot, LinearGradientMode.Vertical))
            g.FillRectangle(bWater, U(80), U(104), U(96), U(106));
        // 水面高光条
        using (var bSurf = new SolidBrush(Surface))
            g.FillRectangle(bSurf, U(80), U(104), U(96), U(9));
        // 气泡
        using (var bBub = new SolidBrush(Color.FromArgb(150, 255, 255, 255)))
        {
            g.FillEllipse(bBub, U(116), U(140), U(11), U(11));
            g.FillEllipse(bBub, U(136), U(166), U(8), U(8));
            g.FillEllipse(bBub, U(122), U(184), U(6), U(6));
        }
        g.Clip = oldClip;

        // 杯壁描边
        using (var pGlass = new Pen(GlassBd, U(9)) { LineJoin = LineJoin.Round })
            g.DrawPath(pGlass, glass);

        // 左侧高光
        using (var pShine = new Pen(Color.FromArgb(190, 255, 255, 255), U(8)) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            g.DrawLine(pShine, U(100), U(84), U(107), U(178));

        return bmp;
    }

    /// <summary>组装 PNG-in-ICO 多尺寸图标文件。</summary>
    public static void SaveIco(string path)
    {
        int[] sizes = { 256, 64, 48, 32, 16 };
        var pngs = new List<byte[]>();
        foreach (var sz in sizes)
        {
            using var bmp = Draw(sz);
            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            pngs.Add(ms.ToArray());
        }
        using var fs = new FileStream(path, FileMode.Create);
        using var bw = new BinaryWriter(fs);
        bw.Write((short)0);
        bw.Write((short)1);
        bw.Write((short)sizes.Length);
        int offset = 6 + 16 * sizes.Length;
        for (int i = 0; i < sizes.Length; i++)
        {
            int sz = sizes[i];
            bw.Write((byte)(sz >= 256 ? 0 : sz));
            bw.Write((byte)(sz >= 256 ? 0 : sz));
            bw.Write((byte)0);
            bw.Write((byte)0);
            bw.Write((short)1);
            bw.Write((short)32);
            bw.Write(pngs[i].Length);
            bw.Write(offset);
            offset += pngs[i].Length;
        }
        foreach (var p in pngs) bw.Write(p);
    }
}
