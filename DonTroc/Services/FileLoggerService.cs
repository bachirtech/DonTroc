using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace DonTroc.Services
{
    public class FileLoggerService
    {
        private readonly string? _externalLogFilePath;
        private readonly string _logFilePath;

        public FileLoggerService()
        {
            var appData = FileSystem.AppDataDirectory;
            _logFilePath = Path.Combine(appData, "dontroc_crash.log");

#if ANDROID
            try
            {
                // Tenter d'écrire également dans le dossier public Téléchargements pour faciliter adb pull
                var downloads = Android.OS.Environment
                    .GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads)?.AbsolutePath;
                if (!string.IsNullOrEmpty(downloads))
                {
                    _externalLogFilePath = Path.Combine(downloads, "dontroc_crash.log");
                }
            }
            catch
            {
                _externalLogFilePath = null;
            }
#else
            _externalLogFilePath = null;
#endif
        }

        public string LogFilePath => _logFilePath;
        public string? ExternalLogFilePath => _externalLogFilePath;

        public void Log(string message)
        {
            try
            {
                var line = $"[{DateTime.UtcNow:O}] {message}{Environment.NewLine}";
                File.AppendAllText(_logFilePath, line, Encoding.UTF8);

#if ANDROID
                try
                {
                    if (!string.IsNullOrEmpty(_externalLogFilePath))
                    {
                        File.AppendAllText(_externalLogFilePath, line, Encoding.UTF8);
                    }
                }
                catch
                {
                    // Ne pas échouer si l'écriture externe échoue
                }
#endif
            }
            catch
            {
                // Ne doit pas laisser l'application planter pendant la tentative de log
            }
        }

        public void LogException(Exception? ex)
        {
            if (ex == null) return;
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("---- EXCEPTION ----");
                sb.AppendLine($"Time: {DateTime.UtcNow:O}");
                sb.AppendLine($"Message: {ex.Message}");
                sb.AppendLine($"Type: {ex.GetType().FullName}");
                sb.AppendLine("StackTrace:");
                sb.AppendLine(ex.StackTrace ?? "(no stack)");

                if (ex.InnerException != null)
                {
                    sb.AppendLine("-- InnerException --");
                    sb.AppendLine(ex.InnerException.ToString());
                }

                sb.AppendLine("--------------------");

                var text = sb.ToString();
                File.AppendAllText(_logFilePath, text, Encoding.UTF8);

#if ANDROID
                try
                {
                    if (!string.IsNullOrEmpty(_externalLogFilePath))
                    {
                        File.AppendAllText(_externalLogFilePath, text, Encoding.UTF8);
                    }
                }
                catch
                {
                    // Swallow
                }
#endif
            }
            catch
            {
                // Swallow
            }
        }

        public string ReadAllLogs()
        {
            try
            {
                return File.Exists(_logFilePath) ? File.ReadAllText(_logFilePath, Encoding.UTF8) : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}