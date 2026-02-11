using System;

namespace DonTroc.Services
{
    /// <summary>
    /// Configuration centralisée pour AppLovin MAX
    /// Plateforme de médiation publicitaire qui combine plusieurs réseaux pour maximiser les revenus
    /// </summary>
    public static class AppLovinConfiguration
    {
        /// <summary>
        /// ⚠️ IMPORTANT: Activer/désactiver AppLovin MAX
        /// Mettre à TRUE une fois que vous avez créé votre compte AppLovin et obtenu vos IDs
        /// </summary>
        public const bool APPLOVIN_ENABLED = false;

        /// <summary>
        /// Clé SDK AppLovin (à obtenir sur https://dash.applovin.com)
        /// Allez dans: Account > Keys > SDK Key
        /// </summary>
        public const string SDK_KEY = "VOTRE_SDK_KEY_APPLOVIN_ICI";

        /// <summary>
        /// ID de bannière AppLovin MAX
        /// Créez une Ad Unit sur: https://dash.applovin.com > Monetize > Ad Units
        /// Type: Banner
        /// </summary>
        public const string BANNER_AD_UNIT_ID = "VOTRE_BANNER_AD_UNIT_ID";

        /// <summary>
        /// ID d'interstitiel AppLovin MAX
        /// Type: Interstitial
        /// </summary>
        public const string INTERSTITIAL_AD_UNIT_ID = "VOTRE_INTERSTITIAL_AD_UNIT_ID";

        /// <summary>
        /// ID de publicité récompensée AppLovin MAX
        /// Type: Rewarded
        /// </summary>
        public const string REWARDED_AD_UNIT_ID = "VOTRE_REWARDED_AD_UNIT_ID";

        /// <summary>
        /// Mode test - Affiche des publicités de test
        /// ⚠️ IMPORTANT: Mettre à FALSE avant la mise en production
        /// </summary>
        public const bool TEST_MODE = true;

        /// <summary>
        /// Vérifie si la configuration est valide
        /// </summary>
        public static bool IsConfigurationValid()
        {
            return APPLOVIN_ENABLED &&
                   !string.IsNullOrEmpty(SDK_KEY) &&
                   SDK_KEY != "VOTRE_SDK_KEY_APPLOVIN_ICI" &&
                   !string.IsNullOrEmpty(BANNER_AD_UNIT_ID) &&
                   BANNER_AD_UNIT_ID != "VOTRE_BANNER_AD_UNIT_ID";
        }

        /// <summary>
        /// Message de statut pour le debugging
        /// </summary>
        public static string GetStatusMessage()
        {
            if (!APPLOVIN_ENABLED)
            {
                return "⚠️ AppLovin désactivé - Configurez vos IDs dans AppLovinConfiguration.cs";
            }
            if (!IsConfigurationValid())
            {
                return "❌ AppLovin mal configuré - Vérifiez SDK_KEY et AD_UNIT_IDs";
            }
            return TEST_MODE ? "🧪 AppLovin en mode TEST" : "✅ AppLovin en mode PRODUCTION";
        }
    }
}
