using System;
using System.Windows.Forms;

namespace GorstakBenchmark
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.ThreadException += (s, e) =>
            {
                try
                {
                    MessageBox.Show(e.Exception.ToString(), "Gorstak Benchmark - Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch { }
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try
                {
                    var ex = e.ExceptionObject as Exception;
                    MessageBox.Show(ex != null ? ex.ToString() : e.ExceptionObject.ToString(),
                        "Gorstak Benchmark - Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch { }
            };

            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Gorstak Benchmark - Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
