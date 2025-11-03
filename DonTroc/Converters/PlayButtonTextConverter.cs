using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace DonTroc.Converters
{
    public class PlayButtonTextConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            var currentPlayingId = value?.ToString();
            var messageId = parameter?.ToString();
            
            if (!string.IsNullOrEmpty(currentPlayingId) && currentPlayingId == messageId)
            {
                return "⏸️"; // Pause si ce message est en cours de lecture
            }
            
            return "▶️"; // Play par défaut
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }
}
