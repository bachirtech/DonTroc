using System;
using DonTroc.Models;
using Microsoft.Maui.Controls;

namespace DonTroc.Views;

/// <summary>
/// Ce sélecteur de modèle de données choisit une mise en page différente
/// pour les messages en fonction de s'ils ont été envoyés ou reçus par l'utilisateur actuel.
/// </summary>
public class MessageTemplateSelector : DataTemplateSelector
{
    // Ajout d'un constructeur sans paramètre pour l'utilisation en XAML
    public MessageTemplateSelector() { }

    public MessageTemplateSelector(DataTemplate sentMessageTemplate, DataTemplate receivedMessageTemplate)
    {
        SentMessageTemplate = sentMessageTemplate;
        ReceivedMessageTemplate = receivedMessageTemplate;
    }

    // Modèle pour les messages envoyés (bulles à droite)
    public DataTemplate SentMessageTemplate { get; set; } = null!;

    // Modèle pour les messages reçus (bulles à gauche)
    public DataTemplate ReceivedMessageTemplate { get; set; } = null!;

    /// <summary>
    /// Logique pour choisir le bon modèle.
    /// </summary>
    /// <param name="item">L'objet de données (ici, un Message).</param>
    /// <param name="container">Le conteneur de l'élément.</param>
    /// <returns>Le DataTemplate approprié.</returns>
    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        ArgumentNullException.ThrowIfNull(item);
        // Vérifie la propriété IsSentByUser du message pour décider quel modèle utiliser.
        return ((Message)item).IsSentByUser ? SentMessageTemplate : ReceivedMessageTemplate;
    }
}
