using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DonTroc.Utils
{
    /// <summary>
    /// Helpers to safely fire-and-forget tasks while observing exceptions.
    /// Usage: someTask.Forget(_logger);
    /// </summary>
    public static class TaskExtensions
    {
        public static void Forget(this Task task, ILogger? logger = null)
        {
            if (task == null) return;

            _ = task.ContinueWith(t =>
            {
                if (t.IsFaulted && t.Exception != null)
                {
                    var ex = t.Exception.Flatten();
                    try
                    {
                        logger?.LogWarning(ex, "Fire-and-forget task failed");
                    }
                    catch { }
                    System.Diagnostics.Debug.WriteLine($"Fire-and-forget task failed: {ex}");
                }
                else if (t.IsCanceled)
                {
                    try { logger?.LogDebug("Fire-and-forget task canceled"); } catch { }
                }
            }, TaskScheduler.Default);
        }

        /// <summary>
        /// Alternative pattern which awaits the task and catches exceptions.
        /// Call as: Task.Run(...).FireAndForgetSafeAsync(_logger);
        /// </summary>
        public static async void FireAndForgetSafeAsync(this Task task, ILogger? logger = null)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                try { logger?.LogDebug("Fire-and-forget task cancelled (OperationCanceledException)"); } catch { }
            }
            catch (Exception ex)
            {
                try { logger?.LogWarning(ex, "Fire-and-forget task exception"); } catch { }
            }
        }
    }
}

