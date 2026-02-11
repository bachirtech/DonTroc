using System;

namespace DonTroc.Services
{
    /// <summary>
    /// Configuration centralisée pour AdMob
    /// Permet de désactiver temporairement toutes les publicités pendant une suspension
    /// </summary>
    public static class AdMobConfiguration
    {
        /// <summary>
        /// ⚠️ IMPORTANT: Mettre à false pour désactiver TOUTES les publicités AdMob
        /// 
        /// Historique:
        /// - Compte AdMob suspendu le 21 janvier 2026 (29 jours)
        /// - Date estimée de réactivation: ~19 février 2026
        /// - Motif: Détection de trafic incorrect (auto-clics)
        /// 
        /// STATUT ACTUEL: RÉACTIVÉ pour tests
        /// Si vous voyez des erreurs dans les logs, remettez à false
        /// </summary>
        public const bool ADS_ENABLED = false;

        /// <summary>
        /// Date de début de suspension (pour référence)
        /// </summary>
        public static readonly DateTime SuspensionStartDate = new DateTime(2026, 1, 21);

        /// <summary>
        /// Durée de suspension en jours
        /// </summary>
        public const int SuspensionDurationDays = 29;

        /// <summary>
        /// Date estimée de fin de suspension
        /// </summary>
        public static DateTime EstimatedReactivationDate => SuspensionStartDate.AddDays(SuspensionDurationDays);

        /// <summary>
        /// Vérifie si les publicités devraient être activées (basé sur la date)
        /// Note: Même si la date est passée, gardez ADS_ENABLED à false jusqu'à confirmation de Google
        /// </summary>
        public static bool IsSuspensionPeriodOver => DateTime.Now > EstimatedReactivationDate;

        /// <summary>
        /// Message à afficher pour le debugging
        /// </summary>
        public static string GetStatusMessage()
        {
            if (!ADS_ENABLED)
            {
                var daysRemaining = (EstimatedReactivationDate - DateTime.Now).Days;
                if (daysRemaining > 0)
                {
                    return $"⚠️ AdMob désactivé - Suspension en cours. {daysRemaining} jours restants estimés.";
                }
                return "⚠️ AdMob désactivé - Période de suspension terminée. Attendez confirmation de Google avant de réactiver.";
            }
            return "✅ AdMob activé";
        }
    }
}
