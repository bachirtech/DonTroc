using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DonTroc.Models;
using DonTroc.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DonTroc.Converters
{
    /// <summary>
    /// Convertit un entier en booléan (true si > 0)
    /// </summary>
    public class IntToBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is int intValue)
            {
                return intValue > 0;
            }
            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit un entier en booléan inversé (true si = 0, false si > 0)
    /// </summary>
    public class IntToBoolInverseConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is int intValue)
            {
                return intValue == 0;
            }
            return true;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit un booléen en opacité (1.0 si true, 0.6 si false)
    /// </summary>
    public class BoolToOpacityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? 1.0 : 0.6;
            }
            return 1.0;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit un entier en string formaté pour les badges
    /// </summary>
    public class BadgeCountConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is int count)
            {
                if (count == 0)
                    return string.Empty;
                if (count > 99)
                    return "99+";
                return count.ToString();
            }
            return string.Empty;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit un booléen en FontAttributes
    /// </summary>
    public class BoolToFontAttributesConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is bool boolValue && boolValue)
                return FontAttributes.Bold;
            return FontAttributes.None;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is FontAttributes fontAttributes)
                return fontAttributes == FontAttributes.Bold;
            return false;
        }
    }

    /// <summary>
    /// Vérifie si deux chaînes sont égales
    /// </summary>
    public class StringEqualsToBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value == null && parameter == null) return true;
            if (value == null || parameter == null) return false;
            return value.ToString()!.Equals(parameter.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit une chaîne en booléen (true si non vide)
    /// </summary>
    public class StringToBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            return !string.IsNullOrWhiteSpace(value?.ToString());
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit un objet en booléen (null = false, non-null = true)
    /// </summary>
    public class ObjectToBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            return value != null;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertisseur générique pour tester l'égalité
    /// </summary>
    public class EqualConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            return Equals(value, parameter);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertisseur générique pour tester l'inégalité (inverse de EqualConverter)
    /// </summary>
    public class NotEqualConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            return !Equals(value, parameter);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertisseur pour vérifier que toutes les valeurs sont vraies
    /// </summary>
    public class AllTrueConverter : IMultiValueConverter
    {
        public object Convert(object[]? values, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (values == null || values.Length == 0)
                return false;

            return values.All(v => v is bool b && b);
        }

        public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit un thème en icône
    /// </summary>
    public class ThemeToIconConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is ThemeService.AppTheme theme)
            {
                return theme switch
                {
                    ThemeService.AppTheme.Light => "☀️",
                    ThemeService.AppTheme.Dark => "🌙",
                    _ => "🔄"
                };
            }
            return "🔄";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit un thème en nom d'affichage
    /// </summary>
    public class ThemeToDisplayNameConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is ThemeService.AppTheme theme)
            {
                return theme switch
                {
                    ThemeService.AppTheme.Light => "Clair",
                    ThemeService.AppTheme.Dark => "Sombre",
                    _ => "Automatique"
                };
            }
            return "Automatique";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit un booléen en texte de tri
    /// </summary>
    public class BoolToSortTextConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is bool isDistanceSort && isDistanceSort)
                return "📍 Distance";
            return "📅 Date";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit un booléen en couleur
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is bool boolValue && boolValue)
                return Colors.Green;
            return Colors.Gray;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Inverse un booléen
    /// </summary>
    public class InverseBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            return true;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            return false;
        }
    }

    /// <summary>
    /// Convertit une liste d'images en visibilité pour le carrousel
    /// </summary>
    public class MultiplePicturesConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is List<string> pictures)
                return pictures.Count > 1;
            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit la visibilité de la distance
    /// </summary>
    public class DistanceVisibilityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is double distance)
                return distance > 0;
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit une distance en chaîne formatée
    /// </summary>
    public class DistanceStringConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is double distance && distance > 0)
            {
                if (distance < 1)
                    return $"{Math.Round(distance * 1000, 0)} m";
                return $"{Math.Round(distance, 1)} km";
            }
            return string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertisseur pour gérer l'absence de photos
    /// </summary>
    public class NoPhotosConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is List<string> photos)
                return photos == null || photos.Count == 0;
            if (value is string photo)
                return string.IsNullOrEmpty(photo);
            return true;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }

    // === CONVERTISSEURS POUR LE SYSTÈME DE NOTATION ===

    /// <summary>
    /// Convertit un booléen en couleur d'étoile (jaune/orange si true, gris si false)
    /// </summary>
    public class BoolToStarColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is bool isSelected && isSelected)
                return Colors.Gold;
            return Colors.LightGray;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit une note (1-5) en description textuelle
    /// </summary>
    public class NoteToDescriptionConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is int note)
            {
                return note switch
                {
                    1 => "Très insatisfaisant",
                    2 => "Insatisfaisant",
                    3 => "Correct",
                    4 => "Satisfaisant",
                    5 => "Excellent",
                    _ => "Sélectionnez une note"
                };
            }
            return "Sélectionnez une note";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit un badge de confiance en couleur
    /// </summary>
    public class BadgeConfianceToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is BadgeConfiance badge)
            {
                return badge switch
                {
                    BadgeConfiance.Nouveau => Colors.Gray,
                    BadgeConfiance.Bronze => Color.FromArgb("#CD7F32"),
                    BadgeConfiance.Argent => Colors.Silver,
                    BadgeConfiance.Or => Colors.Gold,
                    BadgeConfiance.Platine => Color.FromArgb("#E5E4E2"),
                    BadgeConfiance.Diamant => Color.FromArgb("#B9F2FF"),
                    _ => Colors.Gray
                };
            }
            return Colors.Gray;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit un badge de confiance en texte
    /// </summary>
    public class BadgeConfianceToTextConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is BadgeConfiance badge)
            {
                return badge switch
                {
                    BadgeConfiance.Nouveau => "🆕 Nouveau",
                    BadgeConfiance.Bronze => "🥉 Bronze",
                    BadgeConfiance.Argent => "🥈 Argent",
                    BadgeConfiance.Or => "🥇 Or",
                    BadgeConfiance.Platine => "💎 Platine",
                    BadgeConfiance.Diamant => "💠 Diamant",
                    _ => "🆕 Nouveau"
                };
            }
            return "🆕 Nouveau";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit une note numérique en étoiles visuelles
    /// </summary>
    public class NoteToStarsConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is double note || value is int)
            {
                var noteValue = System.Convert.ToDouble(value);
                var fullStars = (int)Math.Floor(noteValue);
                var hasHalfStar = noteValue - fullStars >= 0.5;

                var stars = "";
                for (int i = 0; i < fullStars; i++)
                    stars += "⭐";

                if (hasHalfStar)
                    stars += "✨";

                var emptyStars = 5 - fullStars - (hasHalfStar ? 1 : 0);
                for (int i = 0; i < emptyStars; i++)
                    stars += "☆";

                return stars;
            }
            return "☆☆☆☆☆";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit un booléen en icône de cœur (plein ou vide)
    /// </summary>
    public class BoolToHeartConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is bool isFavorite)
            {
                return isFavorite ? "❤️" : "🤍";
            }
            return "🤍";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit un booléen en couleur pour le bouton favori
    /// </summary>
    public class BoolToFavoriteColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is bool isFavorite)
            {
                return isFavorite ? Colors.Red : Colors.Gray;
            }
            return Colors.Gray;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit un booléen en chemin d'icône de favori (fichier image)
    /// </summary>
    public class BoolToFavoriteIconConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is bool isFavorite)
            {
                return isFavorite ? "heart_filled.png" : "heart_outline.png";
            }
            return "heart_outline.png";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit un double en booléen (true si != 0)
    /// </summary>
    public class DoubleNotZeroConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is double doubleValue)
                return doubleValue != 0.0;
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit une chaîne en booléen (true si non null et non vide)
    /// </summary>
    public class StringNotNullOrEmptyConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            return !string.IsNullOrEmpty(value?.ToString());
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertisseur pour afficher l'icône appropriée selon la visibilité du mot de passe
    /// </summary>
    public class PasswordVisibilityIconConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is bool isVisible)
            {
                // Retourne l'icône œil fermé si le mot de passe est visible, œil ouvert si masqué
                return isVisible ? "👁️" : "👁️‍🗨️";
            }
            return "👁️‍🗨️";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertisseur pour inverser une valeur booléenne
    /// </summary>
    public class InvertedBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return true; // Par défaut, le mot de passe est masqué
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }
}
