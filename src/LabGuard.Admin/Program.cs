namespace LabGuard.Admin;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        var form = new DashboardForm();

        if (args.Length >= 2 && args[0].Equals("--screenshot", StringComparison.OrdinalIgnoreCase))
        {
            string targetPath = args[1];
            form.Show();
            var timer = new System.Windows.Forms.Timer();
            timer.Interval = 5000;
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                using var bmp = new Bitmap(form.Width, form.Height);
                form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
                string? dir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                bmp.Save(targetPath, System.Drawing.Imaging.ImageFormat.Png);
                Application.Exit();
            };
            timer.Start();
            Application.Run(form);
            return;
        }

        Application.Run(form);
    }    
}