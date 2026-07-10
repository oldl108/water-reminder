namespace WaterReminder;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
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
