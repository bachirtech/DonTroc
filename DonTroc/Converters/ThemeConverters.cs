using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DonTroc.Converters
{
    /// <summary>
    /// Convertit un booléen (sélectionné) en couleur de bordure pour le sélecteur de thème.
    /// ConverterParameter: "light", "dark" ou "system"
    /// </summary>
    public class BoolToThemeColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var isSelected = value is true;
            if (!isSelected)
                return Colors.Transparent;

            return parameter?.ToString() switch
            {
                "light" => Color.FromArgb("#D98C6A"),  // Terracotta
                "dark" => Color.FromArgb("#6B7A8F"),   // Ardoise
                "system" => Color.FromArgb("#A8C686"),  // VertSauge
                _ => Color.FromArgb("#D98C6A")
            };
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Convertit un booléen (sélectionné) en couleur de fond pour l'icône du thème.
    /// ConverterParameter: "light", "dark" ou "system"
    /// </summary>
    public class BoolToThemeBgConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var isSelected = value is true;

            return (parameter?.ToString(), isSelected) switch
            {
                ("light", true) => Color.FromArgb("#D98C6A"),   // Terracotta plein
                ("light", false) => Color.FromArgb("#30D98C6A"), // Terracotta très léger
                ("dark", true) => Color.FromArgb("#6B7A8F"),    // Ardoise plein
                ("dark", false) => Color.FromArgb("#306B7A8F"),  // Ardoise très léger
                ("system", true) => Color.FromArgb("#A8C686"),   // VertSauge plein
                ("system", false) => Color.FromArgb("#30A8C686"), // VertSauge très léger
                _ => Color.FromArgb("#30D98C6A")
            };
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Convertit un booléen en FontAttributes (Bold si sélectionné, None sinon)
    /// </summary>
    public class BoolToFontAttrConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is true ? FontAttributes.Bold : FontAttributes.None;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}

