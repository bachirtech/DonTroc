using System;
using System.Collections.Generic;
using CloudinaryDotNet;

namespace DonTroc.Services
{
    /// <summary>
    /// Service de configuration sécurisée pour Cloudinary
    /// </summary>
    public class CloudinaryConfigService
    {
        private readonly Cloudinary _cloudinary;
        
        public CloudinaryConfigService() // Constructeur
        {
            // Configuration sécurisée avec variables d'environnement
            var cloudName = Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME") ?? "dch4vddiy";
            var apiKey = Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY") ?? "855444651663357";
            var apiSecret = Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET") ?? "sTJPlG_CzFHNZseO40HTJSR7npI";
            
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
