namespace AnomaliesDetector
{
    using System;
    using System.IO;
    using System.Windows.Forms;

    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Default to resources/data.csv next to the exe
            var defaultCsv = Path.Combine(AppContext.BaseDirectory, "resources", "sample_logs_no_status.csv");
            var csvPath = (args.Length > 0 && File.Exists(args[0])) ? args[0] : defaultCsv;

            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm(csvPath));
        }
    }

}