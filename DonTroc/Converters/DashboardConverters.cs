// Fichier: DonTroc/Converters/DashboardConverters.cs

using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace DonTroc.Converters
{
    /// <summary>
    /// Retourne true si la DateTime est inférieure à 24h (annonce "Nouveau").
    /// </summary>
    public class DateToIsNewConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                return (DateTime.UtcNow - dateTime).TotalHours < 24;
            }
            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Formate une distance double en texte lisible (ex: "1.2 km", "800 m").
    /// </summary>
    public class DistanceToTextConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double distance && distance > 0 && distance < double.MaxValue)
            {
                if (distance < 1)
                    return $"{(int)(distance * 1000)} m";
                return $"{distance:F1} km";
            }
            return "";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Retourne true si un double? a une valeur positive (pour la visibilité de la distance).
    /// </summary>
    public class NullableDoubleHasValueConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double d && d > 0 && d < double.MaxValue)
                return true;
            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}

