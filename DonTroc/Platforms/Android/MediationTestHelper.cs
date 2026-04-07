using System.Diagnostics;

namespace DonTroc.Platforms.Android
{
    /// <summary>
    /// Helper pour activer le mode test des réseaux de médiation partenaires.
    /// 
    /// Problème résolu :
    /// setTestDeviceIds() d'AdMob ne s'applique QU'à AdMob lui-même.
    /// Chaque réseau partenaire (Facebook, Unity, Pangle, Vungle, IronSource)
    /// a son propre mécanisme de test qu'il faut activer séparément.
    /// 
    /// Comme les SDK partenaires sont en Bind="false" (pas de bindings C#),
    /// on utilise la réflexion Java (JNI) pour appeler leurs APIs statiques.
    /// 
    /// NOTE TECHNIQUE : Les méthodes Java prennent des primitifs (boolean),
    /// il faut utiliser Java.Lang.Boolean.Type (= boolean.class) et NON
    /// Java.Lang.Class.FromType(typeof(Java.Lang.Boolean)) (= Boolean.class).
    /// </summary>
    public static class MediationTestHelper
    {
        // Type primitif boolean Java — nécessaire pour la réflexion sur les méthodes
        // qui prennent un 'boolean' (primitif) et non 'java.lang.Boolean' (objet).
        private static readonly Java.Lang.Class BooleanPrimitiveType = Java.Lang.Boolean.Type!;

        /// <summary>
        /// Active le mode test pour tous les réseaux de médiation partenaires.
        /// À appeler UNIQUEMENT en Debug, AVANT le chargement des pubs.
        /// </summary>
        public static void EnablePartnerTestModes(string? deviceHash = null)
        {
#if DEBUG
            EnableFacebookTestMode(deviceHash);
            EnableUnityTestMode();
            EnablePangleTestMode();
            EnableVungleTestMode();
            // IronSource n'a pas de mode test global activable par code —
            // il se configure via le dashboard IronSource.
            Debug.WriteLine("[MediationTest] ✅ Modes test partenaires activés (Debug uniquement)");
#endif
        }

        /// <summary>
        /// Facebook Audience Network : AdSettings.setTestMode(true)
        /// Permet à Facebook de servir des pubs de test sur cet appareil.
        /// Également appelle addTestDevice() si un hash est fourni.
        /// </summary>
        private static void EnableFacebookTestMode(string? deviceHash)
        {
            try
            {
                // com.facebook.ads.AdSettings
                var adSettingsClass = Java.Lang.Class.ForName("com.facebook.ads.AdSettings");

                // ── setTestMode(boolean) ──
                // boolean.class (primitif), pas Boolean.class (objet wrapper)
                var setTestModeMethod = adSettingsClass.GetMethod("setTestMode", BooleanPrimitiveType);
                setTestModeMethod!.Invoke(null, new Java.Lang.Boolean(true));

                Debug.WriteLine("[MediationTest] ✅ Facebook Audience Network → setTestMode(true)");

                // ── addTestDevice(String) si un hash est fourni ──
                if (!string.IsNullOrEmpty(deviceHash))
                {
                    try
                    {
                        var stringType = Java.Lang.Class.FromType(typeof(Java.Lang.String));
                        var addTestDeviceMethod = adSettingsClass.GetMethod("addTestDevice", stringType);
                        addTestDeviceMethod!.Invoke(null, new Java.Lang.String(deviceHash));
                        Debug.WriteLine($"[MediationTest] ✅ Facebook → addTestDevice({deviceHash})");
                    }
                    catch (Exception ex2)
                    {
                        Debug.WriteLine($"[MediationTest] ⚠️ Facebook addTestDevice échoué: {ex2.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MediationTest] ❌ Facebook test mode non activé: {ex.Message}");
                // Afficher l'exception interne (cause Java) pour le diagnostic
                if (ex.InnerException != null)
                    Debug.WriteLine($"[MediationTest]    Cause: {ex.InnerException.Message}");
            }
        }

        /// <summary>
        /// Unity Ads : UnityAds.setDebugMode(true)
        /// Active les logs de debug Unity Ads.
        /// </summary>
        private static void EnableUnityTestMode()
        {
            try
            {
                var unityAdsClass = Java.Lang.Class.ForName("com.unity3d.ads.UnityAds");
                var setDebugMethod = unityAdsClass.GetMethod("setDebugMode", BooleanPrimitiveType);
                setDebugMethod!.Invoke(null, new Java.Lang.Boolean(true));

                Debug.WriteLine("[MediationTest] ✅ Unity Ads → setDebugMode(true)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MediationTest] ⚠️ Unity Ads debug mode non activé: {ex.Message}");
            }
        }

        /// <summary>
        /// Pangle (TikTok) : PAGConfig.setDebugLog(true)
        /// Active les logs de debug Pangle pour le diagnostic.
        /// </summary>
        private static void EnablePangleTestMode()
        {
            try
            {
                var pagConfigClass = Java.Lang.Class.ForName("com.pangle.sdk.PAGConfig");
                var setDebugLogMethod = pagConfigClass.GetMethod("setDebugLog", BooleanPrimitiveType);
                setDebugLogMethod!.Invoke(null, new Java.Lang.Boolean(true));

                Debug.WriteLine("[MediationTest] ✅ Pangle → setDebugLog(true)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MediationTest] ⚠️ Pangle debug mode non activé: {ex.Message}");
            }
        }

        /// <summary>
        /// Vungle (Liftoff) : Active la vérification d'intégration.
        /// </summary>
        private static void EnableVungleTestMode()
        {
            try
            {
                var vungleAdsClass = Java.Lang.Class.ForName("com.vungle.ads.VungleAds");

                try
                {
                    var setVerificationMethod = vungleAdsClass.GetMethod(
                        "setIntegrationVerificationEnabled", BooleanPrimitiveType);
                    setVerificationMethod!.Invoke(null, new Java.Lang.Boolean(true));
                    Debug.WriteLine("[MediationTest] ✅ Vungle → setIntegrationVerificationEnabled(true)");
                }
                catch
                {
                    Debug.WriteLine("[MediationTest] ⚠️ Vungle setIntegrationVerificationEnabled non disponible");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MediationTest] ⚠️ Vungle debug mode non activé: {ex.Message}");
            }
        }
    }
}
