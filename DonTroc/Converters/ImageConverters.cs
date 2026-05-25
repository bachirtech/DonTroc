using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace DonTroc.Converters;

/// <summary>
/// Convertit une URL (string) en <see cref="UriImageSource"/> avec cache disque
/// activé pendant 7 jours. Résout le bug des images d'annonces qui ne
/// s'affichaient pas de manière fiable :
///   - MAUI Image avec Source=string redownload à chaque apparition (cache
///     mémoire court, pas de cache disque), et échoue silencieusement sur
///     les URLs lentes (Cloudinary, Firebase Storage) sans retry.
///   - UriImageSource avec CachingEnabled stocke l'image sur disque → les
///     prochaines apparitions sont instantanées + survivent au redémarrage.
/// Retourne null si l'URL est vide / invalide (le placeholder XAML reste alors
/// visible derrière l'Image).
/// </summary>
public class UrlToCachedImageSourceConverter : IValueConverter
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromDays(7);

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var url = value as string;
        if (string.IsNullOrWhiteSpace(url))
            return null;

        // Valider que c'est bien une URL HTTP(S) absolue — évite les tentatives
        // de download sur des chemins relatifs ou des strings invalides qui
        // déclenchent des exceptions natives iOS et bloquent l'UI thread.
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return null;

        return new UriImageSource
        {
            Uri = uri,
            CachingEnabled = true,
            CacheValidity = CacheDuration
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

