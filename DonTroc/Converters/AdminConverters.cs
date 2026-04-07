// Fichier: DonTroc/Converters/AdminConverters.cs
// Convertisseurs pour le panneau d'administration

using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace DonTroc.Converters
{
    /// <summary>
    /// Convertit un booléen en texte pour le bouton de suspension
    /// </summary>
    public class BoolToSuspendTextConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isSuspended)
            {
                return isSuspended ? "Réactiver" : "Suspendre";
            }
            return "Suspendre";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit un booléen en texte pour le mode de sélection
    /// </summary>
    public class BoolToSelectionModeTextConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isSelectionMode)
            {
                return isSelectionMode ? "Annuler" : "Sélection";
            }
            return "Sélection";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Vérifie si une chaîne n'est pas vide
    /// </summary>
    public class StringNotEmptyConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                return !string.IsNullOrWhiteSpace(str);
            }
            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    /// <summary>
    /// Convertit un rôle en couleur
    /// </summary>
    public class RoleToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DonTroc.Models.UserRole role)
            {
                return role switch
                {
                    DonTroc.Models.UserRole.Admin => Color.FromArgb("#FFD700"),
                    DonTroc.Models.UserRole.Moderator => Color.FromArgb("#87CEEB"),
                    _ => Colors.Transparent
                };
            }
            return Colors.Transparent;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit un statut de signalement en couleur
    /// </summary>
    public class ReportStatusToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status switch
                {
                    "Pending" => Color.FromArgb("#FF9800"),
                    "Reviewed" => Color.FromArgb("#2196F3"),
                    "ActionTaken" => Color.FromArgb("#4CAF50"),
                    "Dismissed" => Color.FromArgb("#F44336"),
                    _ => Color.FromArgb("#9E9E9E")
                };
            }
            return Color.FromArgb("#9E9E9E");
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

