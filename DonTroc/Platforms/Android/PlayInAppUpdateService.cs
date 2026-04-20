#if ANDROID
using System;
using Android.Gms.Tasks;
using Microsoft.Maui.ApplicationModel;
using Xamarin.Google.Android.Play.Core.AppUpdate;
using Xamarin.Google.Android.Play.Core.AppUpdate.Install;
using Xamarin.Google.Android.Play.Core.AppUpdate.Install.Model;

// Alias pour lever l'ambiguïté avec Android.Gms.Tasks.Task
using Task = System.Threading.Tasks.Task;
using TaskCompletionSource = System.Threading.Tasks.TaskCompletionSource<Xamarin.Google.Android.Play.Core.AppUpdate.AppUpdateInfo?>;
using PlayAppUpdateInfo = Xamarin.Google.Android.Play.Core.AppUpdate.AppUpdateInfo;

namespace DonTroc.Services;

/// <summary>
/// Implémentation Android du Google Play In-App Update SDK.
/// Doc officielle : https://developer.android.com/guide/playcore/in-app-updates
/// </summary>
public sealed class PlayInAppUpdateService : Java.Lang.Object,
    IInAppUpdateService,
    IInstallStateUpdatedListener
{
    /// <summary>Code utilisé par MainActivity.OnActivityResult pour identifier le retour Play Store.</summary>
    public const int RequestCode = 7841;

    private readonly IAppUpdateManager _manager;
    private bool _listenerRegistered;

    public PlayInAppUpdateService()
    {
        _manager = AppUpdateManagerFactory.Create(Android.App.Application.Context);
    }

    public bool IsSupported => true;

    public event Action? FlexibleUpdateDownloaded;

    public async Task<InAppUpdateResult> TryStartUpdateAsync(InAppUpdateMode mode)
    {
        try
        {
            var info = await FetchAppUpdateInfoAsync();
            if (info == null) return InAppUpdateResult.Error;

            if (info.UpdateAvailability() != UpdateAvailability.UpdateAvailable
                && info.UpdateAvailability() != UpdateAvailability.DeveloperTriggeredUpdateInProgress)
            {
                return InAppUpdateResult.NoUpdate;
            }

            var androidUpdateType = mode == InAppUpdateMode.Immediate
                ? AppUpdateType.Immediate
                : AppUpdateType.Flexible;

            if (!info.IsUpdateTypeAllowed(androidUpdateType))
            {
                // Fallback : si Immediate non autorisé mais Flexible oui, accepter Flexible
                if (mode == InAppUpdateMode.Immediate && info.IsUpdateTypeAllowed(AppUpdateType.Flexible))
                {
                    androidUpdateType = AppUpdateType.Flexible;
                }
                else if (mode == InAppUpdateMode.Flexible && info.IsUpdateTypeAllowed(AppUpdateType.Immediate))
                {
                    androidUpdateType = AppUpdateType.Immediate;
                }
                else
                {
                    return InAppUpdateResult.UpdateModeNotAllowed;
                }
            }

            var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            if (activity == null) return InAppUpdateResult.Error;

            // Pour le Flexible : enregistrer le listener AVANT de lancer le flow
            if (androidUpdateType == AppUpdateType.Flexible && !_listenerRegistered)
            {
                _manager.RegisterListener(this);
                _listenerRegistered = true;
            }

            bool started = false;
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                started = _manager.StartUpdateFlowForResult(info, androidUpdateType, activity, RequestCode);
            });

            return started ? InAppUpdateResult.FlowStarted : InAppUpdateResult.Error;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PlayInAppUpdate] TryStartUpdateAsync error: {ex}");
            return InAppUpdateResult.Error;
        }
    }

    public async Task ResumeIfImmediateUpdatePendingAsync()
    {
        try
        {
            var info = await FetchAppUpdateInfoAsync();
            if (info == null) return;

            // Cas 1 : un update Immediate était en cours (l'utilisateur a quitté pendant le DL)
            if (info.UpdateAvailability() == UpdateAvailability.DeveloperTriggeredUpdateInProgress
                && info.IsUpdateTypeAllowed(AppUpdateType.Immediate))
            {
                var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
                if (activity == null) return;

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _manager.StartUpdateFlowForResult(info, AppUpdateType.Immediate, activity, RequestCode);
                });
                return;
            }

            // Cas 2 : un update Flexible a fini de télécharger pendant que l'app était en background
            if (info.InstallStatus() == InstallStatus.Downloaded)
            {
                FlexibleUpdateDownloaded?.Invoke();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PlayInAppUpdate] ResumeIfPending error: {ex.Message}");
        }
    }

    public Task CompleteFlexibleUpdateAsync()
    {
        try
        {
            _manager.CompleteUpdate();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PlayInAppUpdate] CompleteUpdate error: {ex.Message}");
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Listener appelé par Play Core quand l'état d'install change (DOWNLOADING → DOWNLOADED → INSTALLED).
    /// </summary>
    public void OnStateUpdate(InstallState? state)
    {
        try
        {
            if (state == null) return;
            var status = state.InstallStatus();
            System.Diagnostics.Debug.WriteLine($"[PlayInAppUpdate] InstallStatus = {status}");

            if (status == InstallStatus.Downloaded)
            {
                FlexibleUpdateDownloaded?.Invoke();
            }
            else if (status == InstallStatus.Installed || status == InstallStatus.Failed
                     || status == InstallStatus.Canceled)
            {
                if (_listenerRegistered)
                {
                    try { _manager.UnregisterListener(this); } catch { }
                    _listenerRegistered = false;
                }
            }
        }
        catch { /* non-bloquant */ }
    }

    /// <summary>Wrapping de la Google Play Task → C# Task.</summary>
    private Task<PlayAppUpdateInfo?> FetchAppUpdateInfoAsync()
    {
        var tcs = new TaskCompletionSource();
        try
        {
            var googleTask = _manager.GetAppUpdateInfo();
            googleTask.AddOnSuccessListener(new SuccessListener(obj =>
            {
                tcs.TrySetResult(obj as PlayAppUpdateInfo);
            }));
            googleTask.AddOnFailureListener(new FailureListener(ex =>
            {
                System.Diagnostics.Debug.WriteLine($"[PlayInAppUpdate] AppUpdateInfo failure: {ex?.Message}");
                tcs.TrySetResult(null);
            }));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PlayInAppUpdate] AppUpdateInfo exception: {ex.Message}");
            tcs.TrySetResult(null);
        }
        return tcs.Task;
    }

    private sealed class SuccessListener : Java.Lang.Object, IOnSuccessListener
    {
        private readonly Action<Java.Lang.Object?> _onSuccess;
        public SuccessListener(Action<Java.Lang.Object?> onSuccess) => _onSuccess = onSuccess;
        public void OnSuccess(Java.Lang.Object? result) => _onSuccess(result);
    }

    private sealed class FailureListener : Java.Lang.Object, IOnFailureListener
    {
        private readonly Action<Java.Lang.Exception?> _onFailure;
        public FailureListener(Action<Java.Lang.Exception?> onFailure) => _onFailure = onFailure;
        public void OnFailure(Java.Lang.Exception ex) => _onFailure(ex);
    }
}
#endif

