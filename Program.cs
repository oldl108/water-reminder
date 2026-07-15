namespace WaterReminder;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        // 开发用：生成 icon.ico 和预览图
        if (args.Contains("--genicon"))
        {
            string dir = AppContext.BaseDirectory;
            IconFactory.SaveIco(Path.Combine(dir, "icon.ico"));
            using (var png = IconFactory.Draw(256))
                png.Save(Path.Combine(dir, "icon-preview.png"));
            return;
        }

        // 开发用：把各窗口渲染成干净的 PNG（演示数据，不碰真实记录）
        if (args.Contains("--shots"))
        {
            ApplicationConfiguration.Initialize();
            RunShots();
            return;
        }

        // 开发用陈列模式：一次摆出所有窗口，方便检查布局和截图
        if (args.Contains("--showcase"))
        {
            ApplicationConfiguration.Initialize();
            RunShowcase();
            return;
        }

        using var mutex = new Mutex(true, "WaterReminder_SingleInstance", out bool isNew);
        if (!isNew)
        {
            MessageBox.Show("「早睡早起多喝水」已经在运行了，看看任务栏右下角的水滴图标。", "早睡早起多喝水",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayContext());
    }

    /// <summary>用演示数据把每个窗口渲染成 PNG，供官网使用。数据写在临时目录，不影响真实记录。</summary>
    static void RunShots()
    {
        string outDir = Path.Combine(AppContext.BaseDirectory, "shots");
        Directory.CreateDirectory(outDir);
        string demoDir = Path.Combine(Path.GetTempPath(), "wr-demo-" + Guid.NewGuid().ToString("N")[..6]);
        var store = new DataStore(demoDir);

        // 演示数据：今天喝了 5 杯，最近一周有起伏
        var today = store.Today();
        foreach (var (t, ml) in new[] { ("09:20", 250), ("10:40", 250), ("12:05", 300), ("14:30", 250), ("16:10", 200) })
            today.Water.Add(new WaterEntry { Time = t, Ml = ml });
        today.Stands = 6;
        today.Meds.Add(new MedEntry { Name = "维生素 D", Sched = "08:00", TakenAt = "08:12" });
        int[] past = { 2100, 1750, 2300, 1500, 2050, 1250 };
        for (int i = 0; i < past.Length; i++)
        {
            var rec = new DayRecord();
            rec.Water.Add(new WaterEntry { Time = "12:00", Ml = past[i] });
            store.Days[DateTime.Today.AddDays(-(past.Length - i)).ToString("yyyy-MM-dd")] = rec;
        }
        store.Config.Meds.Add(new MedSchedule { Name = "维生素 D", Times = new List<string> { "08:00" } });
        store.Config.Meds.Add(new MedSchedule { Name = "钙片", Times = new List<string> { "12:30", "20:00" } });

        void Capture(Form f, string name)
        {
            Application.DoEvents();
            Thread.Sleep(400);
            Application.DoEvents();
            using var bmp = new Bitmap(f.Width, f.Height);
            f.DrawToBitmap(bmp, new Rectangle(0, 0, f.Width, f.Height));
            bmp.Save(Path.Combine(outDir, name + ".png"), System.Drawing.Imaging.ImageFormat.Png);
            f.Close();
        }

        void Shot(Form f, string name)
        {
            f.StartPosition = FormStartPosition.Manual;
            f.Location = new Point(60, 60);
            f.Show();
            Capture(f, name);
        }

        var toast = new ToastForm("💧 该喝口水了", "已经坐了 52 分钟，顺便站起来动一动", "已喝 +250ml");
        Shot(toast, "01-toast");

        var bed = new ToastForm("🌙 该睡觉了", "已经 22:00 了，放下手头的事，早点休息", "知道了，这就睡");
        Shot(bed, "02-bedtime");

        var med = new ToastForm("💊 该吃药了", "维生素 D（08:00 这一顿）", "已吃了");
        Shot(med, "03-med-toast");

        // 面板的数据刷新在 ShowAt 里，必须走它，否则文字是空的
        var panel = new PanelForm(store, () => { }) { KeepOpen = true };
        panel.ShowAt(new Point(60, 60));
        Capture(panel, "04-panel");

        Shot(new StatsForm(store), "05-stats");
        Shot(new MedsForm(store), "06-meds");
        Shot(new SettingsForm(store), "07-settings");

        try { Directory.Delete(demoDir, true); } catch { }
        Console.WriteLine("shots saved to " + outDir);
    }

    static void RunShowcase()
    {
        var store = new DataStore();

        var toast = new ToastForm("💧 该喝口水了", "已经坐了 128 分钟，顺便站起来动一动", "已喝 +250ml")
        {
            Location = new Point(20, 20),
            Opacity = 1,
        };
        toast.Show();

        var panel = new PanelForm(store, () => { }) { KeepOpen = true };
        panel.ShowAt(new Point(580, 20));

        var meds = new MedsForm(store) { StartPosition = FormStartPosition.Manual, Location = new Point(1120, 20), TopMost = true };
        meds.Show();

        var stats = new StatsForm(store) { StartPosition = FormStartPosition.Manual, Location = new Point(20, 340), TopMost = true };
        stats.Show();

        var settings = new SettingsForm(store) { StartPosition = FormStartPosition.Manual, Location = new Point(720, 340), TopMost = true };
        settings.Show();

        Application.Run(stats);
    }
}
