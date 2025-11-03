using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DonTroc.Converters
{
    /// <summary>
    /// Convertisseur pour les couleurs d'ombre basé sur un booléen
    /// </summary>
    public class BoolToShadowColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // Si true, retourne une ombre plus prononcée, sinon plus subtile
                return boolValue ? Color.FromArgb("#40000000") : Color.FromArgb("#20000000");
            }
            
            return Color.FromArgb("#20000000"); // Couleur par défaut
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
