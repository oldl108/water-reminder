using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace WaterReminder;

/// <summary>用 GDI+ 绘制卡通水豚（卡皮巴拉）端水杯图标，并打包成多尺寸 .ico。</summary>
static class IconFactory
{
    static readonly Color Paper   = Color.FromArgb(250, 246, 238);
    static readonly Color PaperBd = Color.FromArgb(214, 204, 185);
    static readonly Color Fur     = Color.FromArgb(178, 138, 92);
    static readonly Color FurDark = Color.FromArgb(138, 106, 69);
    static readonly Color Ink     = Color.FromArgb(62, 53, 41);
    static readonly Color Cup     = Color.FromArgb(231, 237, 244);
    static readonly Color CupBd   = Color.FromArgb(91, 125, 163);
    static readonly Color Water   = Color.FromArgb(133, 183, 235);
    static readonly Color Straw   = Color.FromArgb(216, 90, 48);

    /// <summary>在 256 基准坐标系上绘制，按尺寸等比缩放。</summary>
    public static Bitmap Draw(int s, bool withPlate = true)
    {
        var bmp = new Bitmap(s, s);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.Clear(Color.Transparent);

        float k = s / 256f;
        float U(float v) => v * k;
        RectangleF R(float x, float y, float w, float h) => new(U(x), U(y), U(w), U(h));

        using var bFur = new SolidBrush(Fur);
        using var bFurDark = new SolidBrush(FurDark);
        using var bInk = new SolidBrush(Ink);
        using var bWhite = new SolidBrush(Paper);
        using var bCup = new SolidBrush(Cup);
        using var bWater = new SolidBrush(Water);

        if (withPlate)
        {
            float m = U(8), w = s - 2 * m, rr = U(48);
            using var path = new GraphicsPath();
            path.AddArc(m, m, rr, rr, 180, 90);
            path.AddArc(m + w - rr, m, rr, rr, 270, 90);
            path.AddArc(m + w - rr, m + w - rr, rr, rr, 0, 90);
            path.AddArc(m, m + w - rr, rr, rr, 90, 90);
            path.CloseFigure();
            using var pBg = new Pen(PaperBd, U(4));
            g.FillPath(bWhite, path);
            g.DrawPath(pBg, path);
        }

        // 耳朵
        g.FillEllipse(bFur, R(88, 67, 48, 42));
        g.FillEllipse(bFur, R(120, 67, 48, 42));
        g.FillEllipse(bFurDark, R(101, 71, 22, 18));
        g.FillEllipse(bFurDark, R(133, 71, 22, 18));

        // 身体（坐姿）+ 头（连成敦实一体）
        g.FillEllipse(bFur, R(64, 118, 128, 130));
        g.FillEllipse(bFur, R(68, 64, 120, 150));

        // 眼睛（带高光）
        g.FillEllipse(bInk, R(111, 130, 22, 24));
        g.FillEllipse(bInk, R(155, 130, 22, 24));
        g.FillEllipse(bWhite, R(116, 134, 7, 8));
        g.FillEllipse(bWhite, R(160, 134, 7, 8));

        // 吻部 + 鼻孔
        g.FillEllipse(bFurDark, R(114, 158, 92, 50));
        g.FillEllipse(bInk, R(132, 173, 10, 10));
        g.FillEllipse(bInk, R(178, 173, 10, 10));

        // 前爪扶杯
        g.FillEllipse(bFurDark, R(94, 210, 44, 32));
        g.FillEllipse(bFurDark, R(182, 210, 44, 32));

        // 水杯 + 水 + 吸管
        float cx = U(114), cy = U(200), cw = U(52), ch = U(50);
        g.FillRectangle(bCup, cx, cy, cw, ch);
        g.FillRectangle(bWater, cx, cy + U(18), cw, ch - U(18));
        using (var pCupBd = new Pen(CupBd, U(6)))
            g.DrawRectangle(pCupBd, cx, cy, cw, ch);
        using (var pStraw = new Pen(Straw, U(9)) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            g.DrawLine(pStraw, cx + U(34), cy - U(16), cx + U(18), cy + U(22));

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
        bw.Write((short)0);           // reserved
        bw.Write((short)1);           // type = icon
        bw.Write((short)sizes.Length);
        int offset = 6 + 16 * sizes.Length;
        for (int i = 0; i < sizes.Length; i++)
        {
            int sz = sizes[i];
            bw.Write((byte)(sz >= 256 ? 0 : sz));
            bw.Write((byte)(sz >= 256 ? 0 : sz));
            bw.Write((byte)0);        // palette
            bw.Write((byte)0);        // reserved
            bw.Write((short)1);       // planes
            bw.Write((short)32);      // bpp
            bw.Write(pngs[i].Length);
            bw.Write(offset);
            offset += pngs[i].Length;
        }
        foreach (var p in pngs) bw.Write(p);
    }
}
