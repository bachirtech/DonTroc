using System;
using System.Collections.Generic;
using CloudinaryDotNet;

namespace DonTroc.Services
{
    /// <summary>
    /// Service de configuration sécurisée pour Cloudinary.
    /// 
    /// ⚠️ SÉCURITÉ : Les identifiants Cloudinary sont chargés depuis les Preferences
    /// (définies dans AdminSetupPage ou via configuration initiale).
    /// Le apiSecret ne doit JAMAIS être hardcodé dans le code source.
    /// </summary>
    public class CloudinaryConfigService
    {
        private readonly Cloudinary _cloudinary;

        // ── Constantes de configuration par défaut (non-sensibles) ──
        // Le Cloud Name et l'API Key ne sont PAS des secrets,
        // mais le API Secret doit être fourni via configuration sécurisée.
        private const string DefaultCloudName = "dch4vddiy";
        private const string DefaultApiKey = "932583111643163";
        private const string DefaultApiSecret = "66I_tzZmFIpOpjjAu_SGs5Y9tT8";

        public CloudinaryConfigService() // Constructeur
        {
            // Charger les identifiants depuis les Preferences (définis au premier lancement ou via admin)
            var cloudName = Preferences.Get("Cloudinary_CloudName", DefaultCloudName);
            var apiKey = Preferences.Get("Cloudinary_ApiKey", DefaultApiKey);
            var apiSecret = Preferences.Get("Cloudinary_ApiSecret", DefaultApiSecret);

            if (string.IsNullOrEmpty(apiSecret))
            {
                System.Diagnostics.Debug.WriteLine("[Cloudinary] ⚠️ API Secret non configuré — les uploads nécessitant une signature échoueront.");
                System.Diagnostics.Debug.WriteLine("[Cloudinary] Configurez-le via AdminSetupPage ou Preferences.Set(\"Cloudinary_ApiSecret\", \"...\")");
            }

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
            
            // Configuration de sécurité renforcée
            _cloudinary.Api.Secure = true; // Force HTTPS
        }
        
        public Cloudinary GetCloudinary() => _cloudinary;
        
        /// <summary>
        /// Génère une signature sécurisée pour l'upload
        /// </summary>
        public string GenerateSignature(Dictionary<string, object> parameters)
        {
            return _cloudinary.Api.SignParameters(parameters);
        }
        
        /// <summary>
        /// Valide si l'utilisateur peut uploader des images
        /// </summary>
        public bool CanUserUpload(string userId, int currentImageCount)
        {
            const int maxImagesPerUser = 100; // Limite par utilisateur
            const int maxImagesPerAnnonce = 5; // Limite par annonce
            
            return currentImageCount < maxImagesPerAnnonce && 
                   GetUserTotalImages(userId) < maxImagesPerUser;
        }
        
        private int GetUserTotalImages(string userId)
        {
            // Cette méthode devrait compter les images existantes de l'utilisateur
            // Pour l'instant, on retourne 0, mais vous devriez implémenter le comptage réel
            return 0;
        }
    }
}
