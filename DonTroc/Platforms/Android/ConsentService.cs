#if ANDROID
using Android.App;
using Microsoft.Maui.ApplicationModel;
using Xamarin.Google.UserMesssagingPlatform;

namespace DonTroc.Services;

/// <summary>
/// Implémentation Android du consentement RGPD via Google User Messaging Platform (UMP).
/// Doc officielle : https://developers.google.com/admob/android/privacy/gdpr
///
/// Flow recommandé par Google :
/// 1. Au démarrage de l'app : RequestConsentInfoUpdate
/// 2. Si formulaire requis : LoadAndShowConsentFormIfRequired
/// 3. Quand CanRequestAds() = true → MobileAds.Initialize()
/// 4. Bouton "Confidentialité publicitaire" → ShowPrivacyOptionsForm
/// </summary>
public sealed class ConsentService : IConsentService
{
    private IConsentInformation? _consentInformation;

    public bool CanRequestAds()
    {
        try
        {
            return GetOrCreateConsentInfo()?.CanRequestAds() ?? true;
        }
        catch
        {
            // En cas d'erreur, on autorise (fallback safe — sinon plus aucune pub ne s'affiche)
            return true;
        }
    }

    public bool IsPrivacyOptionsRequired()
    {
        try
        {
            var info = GetOrCreateConsentInfo();
            if (info == null) return false;
            // PrivacyOptionsRequirementStatus est une propriété (pas méthode)
            // dont la valeur "Required" indique que l'utilisateur est en EEE/UK
            var status = info.PrivacyOptionsRequirementStatus;
            return status?.Equals(ConsentInformationPrivacyOptionsRequirementStatus.Required) ?? false;
        }
        catch
        {
            return false;
        }
    }

    public Task<bool> GatherConsentAsync()
    {
        var tcs = new TaskCompletionSource<bool>();
        try
        {
            var activity = Platform.CurrentActivity;
            if (activity == null)
            {
                tcs.TrySetResult(true); // pas d'activité → on autorise par défaut
                return tcs.Task;
            }

            var info = GetOrCreateConsentInfo(activity);
            if (info == null)
            {
                tcs.TrySetResult(true);
                return tcs.Task;
            }

            // Paramètres : pas d'option "underage" car app grand public, pas debug en release
            var paramsBuilder = new ConsentRequestParameters.Builder()
                .SetTagForUnderAgeOfConsent(false);

#if DEBUG
            // En DEBUG : forcer le formulaire à s'afficher en mode EEE pour tester
            // Constante DebugGeography.Eea = 1 (cf SDK Java officiel UMP)
            // ⚠️ Récupère ton hashed device ID dans Logcat au 1er run
            const int DEBUG_GEOGRAPHY_EEA = 1;
            var debugSettings = new ConsentDebugSettings.Builder(activity)
                .SetDebugGeography(DEBUG_GEOGRAPHY_EEA)
                .AddTestDeviceHashedId("c91e98ae-8285-4cd6-bc6c-0f687f7b2584")
                .Build();
            paramsBuilder.SetConsentDebugSettings(debugSettings);
#endif

            var requestParams = paramsBuilder.Build();

            info.RequestConsentInfoUpdate(
                activity,
                requestParams,
                new ConsentInfoUpdateSuccessListener(() =>
                {
                    // Une fois l'info à jour, charger + afficher le form si requis
                    UserMessagingPlatform.LoadAndShowConsentFormIfRequired(
                        activity,
                        new ConsentFormDismissedListener(error =>
                        {
                            if (error != null)
                            {
                                System.Diagnostics.Debug.WriteLine(
                                    $"[Consent] Form dismissed with error code={error.ErrorCodeData}: {error.Message}");
                            }
                            // Que le form ait réussi ou échoué, on continue avec ce qu'on a
                            tcs.TrySetResult(info.CanRequestAds());
                        }));
                }),
                new ConsentInfoUpdateFailureListener(error =>
                {
                    var msg = error == null ? "(no error)" : $"code={error.ErrorCodeData} msg={error.Message}";
                    System.Diagnostics.Debug.WriteLine($"[Consent] RequestConsentInfoUpdate failed: {msg}");
                    // Échec → on autorise les pubs (fallback safe pour pas perdre 100% de revenus)
                    tcs.TrySetResult(true);
                }));
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Consent] GatherConsentAsync exception: {ex.Message}");
            tcs.TrySetResult(true);
        }
        return tcs.Task;
    }

    public Task ShowPrivacyOptionsFormAsync()
    {
        var tcs = new TaskCompletionSource<bool>();
        try
        {
            var activity = Platform.CurrentActivity;
            if (activity == null) { tcs.TrySetResult(false); return tcs.Task; }

            UserMessagingPlatform.ShowPrivacyOptionsForm(
                activity,
                new ConsentFormDismissedListener(error =>
                {
                    if (error != null)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[Consent] PrivacyOptions dismissed error: {error.Message}");
                    }
                    tcs.TrySetResult(true);
                }));
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Consent] ShowPrivacyOptions exception: {ex.Message}");
            tcs.TrySetResult(false);
        }
        return tcs.Task;
    }

    public void ResetConsent()
    {
        try
        {
            GetOrCreateConsentInfo()?.Reset();
            System.Diagnostics.Debug.WriteLine("[Consent] Consent reset");
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Consent] Reset error: {ex.Message}");
        }
    }

    private IConsentInformation? GetOrCreateConsentInfo(Activity? activity = null)
    {
        if (_consentInformation != null) return _consentInformation;
        try
        {
            var ctx = (Android.Content.Context?)activity ?? Android.App.Application.Context;
            _consentInformation = UserMessagingPlatform.GetConsentInformation(ctx);
            return _consentInformation;
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Consent] GetConsentInformation error: {ex.Message}");
            return null;
        }
    }

    // ────────────────────────────────────────────────────────────
    // Listeners JNI (callbacks Java → C#)
    // ────────────────────────────────────────────────────────────

    private sealed class ConsentInfoUpdateSuccessListener
        : Java.Lang.Object, IConsentInformationOnConsentInfoUpdateSuccessListener
    {
        private readonly System.Action _onSuccess;
        public ConsentInfoUpdateSuccessListener(System.Action onSuccess) => _onSuccess = onSuccess;
        public void OnConsentInfoUpdateSuccess() => _onSuccess();
    }

    private sealed class ConsentInfoUpdateFailureListener
        : Java.Lang.Object, IConsentInformationOnConsentInfoUpdateFailureListener
    {
        private readonly System.Action<FormError?> _onFailure;
        public ConsentInfoUpdateFailureListener(System.Action<FormError?> onFailure) => _onFailure = onFailure;
        public void OnConsentInfoUpdateFailure(FormError? error) => _onFailure(error);
    }

    private sealed class ConsentFormDismissedListener
        : Java.Lang.Object, IConsentFormOnConsentFormDismissedListener
    {
        private readonly System.Action<FormError?> _onDismissed;
        public ConsentFormDismissedListener(System.Action<FormError?> onDismissed) => _onDismissed = onDismissed;
        public void OnConsentFormDismissed(FormError? error) => _onDismissed(error);
    }
}
#endif

