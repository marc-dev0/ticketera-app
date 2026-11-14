using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace TicketeraApp.Services
{
    /// <summary>
    /// Log de texto plano, un archivo por día, en AppData/Roaming/TicketeraApp/logs.
    /// Nunca lanza excepciones: un fallo al loguear no debe romper la aplicación.
    /// </summary>
    public static class LogService
    {
        private const int RetentionDays = 14;
        private static readonly object _lock = new object();

        public static string LogFolder { get; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TicketeraApp", "logs");

        public static void Info(string message) => Write("INFO ", message);
        public static void Warn(string message) => Write("WARN ", message);

        public static void Error(string message, Exception? ex = null) =>
            Write("ERROR", ex == null ? message : $"{message} | {ex.GetType().Name}: {ex.Message}{Environment.NewLine}{ex.StackTrace}");

        /// <summary>Representación legible de bytes de control (ESC = &lt;ESC&gt;) para el log.</summary>
        public static string Printable(string text) =>
            text.Replace("\u001b", "<ESC>").Replace("\r", "\\r").Replace("\n", "\\n");

        public static void CleanOldLogs()
        {
            try
            {
                if (!Directory.Exists(LogFolder)) return;
                var limit = DateTime.Now.AddDays(-RetentionDays);
                foreach (var file in Directory.GetFiles(LogFolder, "ticketera-*.log").Where(f => File.GetLastWriteTime(f) < limit))
                    File.Delete(file);
            }
            catch { }
        }

        public static void OpenLogFolder()
        {
            try
            {
                Directory.CreateDirectory(LogFolder);
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{LogFolder}\"") { UseShellExecute = true });
            }
            catch { }
        }

        private static void Write(string level, string message)
        {
            try
            {
                lock (_lock)
                {
                    Directory.CreateDirectory(LogFolder);
                    string path = Path.Combine(LogFolder, $"ticketera-{DateTime.Now:yyyyMMdd}.log");
                    File.AppendAllText(path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}", Encoding.UTF8);
                }
            }
            catch { }
        }
    }
}
