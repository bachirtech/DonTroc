using System;
using System.IO;
using System.Text;

namespace DonTroc.Services;

/// <summary>
/// Logger ultra-minimaliste sans aucune dépendance (pas de DI, pas de Maui).
/// Écrit dans Documents/boot.log, accessible via l'app "Fichiers" iOS
/// (UIFileSharingEnabled + LSSupportsOpeningDocumentsInPlace = true).
///
/// But : tracer le démarrage de l'app étape par étape, même si tout crashe
/// ou si on a un écran noir, pour comprendre où on se bloque.
/// </summary>
public static class BootLogger
{
    private static readonly object _lock = new();
    private static string? _path;
    private static bool _initialized;

    private static string Path
    {
        get
        {
            if (_path != null) return _path;
            try
            {
                var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (string.IsNullOrEmpty(docs))
                    docs = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                _path = System.IO.Path.Combine(docs, "boot.log");
            }
            catch
            {
                _path = "/tmp/dontroc_boot.log";
            }
            return _path!;
        }
    }

    public static void Log(string message)
    {
        try
        {
            lock (_lock)
            {
                if (!_initialized)
                {
                    _initialized = true;
                    try
                    {
                        // Rotation : si > 200 Ko on repart à zéro
                        var fi = new FileInfo(Path);
                        if (fi.Exists && fi.Length > 200_000)
                            File.Delete(Path);
                    }
                    catch { }
                    try
                    {
                        File.AppendAllText(Path,
                            $"\n========== BOOT {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} ==========\n",
                            Encoding.UTF8);
                    }
                    catch { }
                }

                var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n";
                File.AppendAllText(Path, line, Encoding.UTF8);
            }

            try { System.Diagnostics.Debug.WriteLine($"[BOOT] {message}"); } catch { }
            try { Console.WriteLine($"[BOOT] {message}"); } catch { }
        }
        catch
        {
            // Ne JAMAIS throw depuis un logger.
        }
    }

    public static void LogException(string where, Exception ex)
    {
        try
        {
            Log($"❌ EXCEPTION @ {where} : {ex.GetType().Name}: {ex.Message}");
            Log($"   StackTrace: {ex.StackTrace}");
            if (ex.InnerException != null)
                Log($"   Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
        }
        catch { }
    }
}

