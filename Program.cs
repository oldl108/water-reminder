namespace WaterReminder;

static class Program
{
    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, "WaterReminder_SingleInstance", out bool isNew);
        if (!isNew)
        {
            MessageBox.Show("喝水提醒已经在运行了，看看任务栏右下角的水滴图标。", "喝水提醒",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayContext());
    }
}
