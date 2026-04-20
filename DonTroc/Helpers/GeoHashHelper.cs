using System;
using System.Collections.Generic;
using System.Text;

namespace DonTroc.Helpers
{
    /// <summary>
    /// Utilitaire GeoHash pour encoder des coordonnées GPS en chaînes préfixées.
    /// Permet des requêtes spatiales efficaces sur Firebase Realtime Database
    /// en filtrant par préfixe GeoHash au lieu de charger tous les profils.
    /// 
    /// Précisions :
    ///   - 4 caractères → ~39km × 20km (idéal pour requête 50km)
    ///   - 5 caractères → ~4.9km × 4.9km
    ///   - 6 caractères → ~1.2km × 0.6km
    /// </summary>
    public static class GeoHashHelper
    {
        private const string Base32Chars = "0123456789bcdefghjkmnpqrstuvwxyz";
        
        /// <summary>
        /// Encode une coordonnée GPS en GeoHash.
        /// </summary>
        /// <param name="latitude">Latitude (-90 à 90)</param>
        /// <param name="longitude">Longitude (-180 à 180)</param>
        /// <param name="precision">Nombre de caractères (1-12, défaut 6)</param>
        /// <returns>Chaîne GeoHash</returns>
        public static string Encode(double latitude, double longitude, int precision = 6)
        {
            if (precision < 1 || precision > 12)
                precision = 6;

            double minLat = -90.0, maxLat = 90.0;
            double minLon = -180.0, maxLon = 180.0;
            
            var sb = new StringBuilder(precision);
            var isEven = true; // alternance longitude/latitude
            int bit = 0;
            int ch = 0;

            while (sb.Length < precision)
            {
                if (isEven)
                {
                    // Longitude
                    var mid = (minLon + maxLon) / 2;
                    if (longitude >= mid)
                    {
                        ch |= 1 << (4 - bit);
                        minLon = mid;
                    }
                    else
                    {
                        maxLon = mid;
                    }
                }
                else
                {
                    // Latitude
                    var mid = (minLat + maxLat) / 2;
                    if (latitude >= mid)
                    {
                        ch |= 1 << (4 - bit);
                        minLat = mid;
                    }
                    else
                    {
                        maxLat = mid;
                    }
                }

                isEven = !isEven;
                bit++;

                if (bit == 5)
                {
                    sb.Append(Base32Chars[ch]);
                    bit = 0;
                    ch = 0;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Retourne les 8 GeoHash voisins + le centre pour un GeoHash donné.
        /// Nécessaire pour couvrir entièrement un rayon autour d'un point.
        /// </summary>
        public static HashSet<string> GetNeighborsAndSelf(double latitude, double longitude, int precision = 4)
        {
            var centerHash = Encode(latitude, longitude, precision);
            var neighbors = new HashSet<string> { centerHash };

            // Calculer le delta approximatif pour la précision donnée
            // Un GeoHash de précision N couvre environ :
            //   Précision 4 → lat ~0.18°, lon ~0.35°
            //   Précision 5 → lat ~0.022°, lon ~0.044°
            var (latDelta, lonDelta) = GetDeltaForPrecision(precision);

            // 8 directions : N, NE, E, SE, S, SW, W, NW
            double[] latOffsets = { latDelta, latDelta, 0, -latDelta, -latDelta, -latDelta, 0, latDelta };
            double[] lonOffsets = { 0, lonDelta, lonDelta, lonDelta, 0, -lonDelta, -lonDelta, -lonDelta };

            for (int i = 0; i < 8; i++)
            {
                var newLat = Math.Clamp(latitude + latOffsets[i], -90, 90);
                var newLon = longitude + lonOffsets[i];
                
                // Wrap longitude autour de ±180
                if (newLon > 180) newLon -= 360;
                if (newLon < -180) newLon += 360;
                
                neighbors.Add(Encode(newLat, newLon, precision));
            }

            return neighbors;
        }

        /// <summary>
        /// Retourne les deltas lat/lon approximatifs pour une précision GeoHash.
        /// </summary>
        private static (double latDelta, double lonDelta) GetDeltaForPrecision(int precision)
        {
            return precision switch
            {
                1 => (45.0, 45.0),
                2 => (5.625, 11.25),
                3 => (1.40625, 1.40625),
                4 => (0.17578, 0.35156),
                5 => (0.02197, 0.04395),
                6 => (0.00549, 0.00549),
                _ => (0.17578, 0.35156) // Défaut : précision 4
            };
        }

        /// <summary>
        /// Retourne le préfixe GeoHash à utiliser pour les requêtes Firebase.
        /// Utilise une précision qui couvre efficacement le rayon demandé.
        /// </summary>
        /// <param name="radiusKm">Rayon en km</param>
        /// <returns>Précision GeoHash recommandée</returns>
        public static int GetPrecisionForRadius(double radiusKm)
        {
            // Adapter la précision au rayon pour minimiser les faux positifs
            return radiusKm switch
            {
                <= 1 => 6,    // ~1.2km × 0.6km
                <= 5 => 5,    // ~4.9km × 4.9km
                <= 50 => 4,   // ~39km × 20km
                <= 200 => 3,  // ~156km × 156km
                _ => 2        // Très large
            };
        }
    }
}

