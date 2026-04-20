using System;
using System.Collections.Generic;

namespace DonTroc.Models;

/// <summary>
/// Statuts possibles d'une proposition de troc.
/// </summary>
public enum TradeProposalStatus
{
    Pending,        // En attente de réponse du destinataire
    Accepted,       // Acceptée → crée une Transaction
    Declined,       // Refusée
    Cancelled,      // Annulée par le proposeur
    CounterOffered, // Une contre-proposition a été faite
    Expired         // Expirée (pas de réponse après 7j)
}

/// <summary>
/// Proposition structurée d'un troc entre deux utilisateurs :
/// un utilisateur offre une de ses annonces en échange d'une annonce cible.
/// </summary>
public class TradeProposal
{
    public string Id { get; set; } = string.Empty;

    // Annonce que le proposeur souhaite obtenir (appartient au OwnerId)
    public string AnnonceCibleId { get; set; } = string.Empty;

    // Annonce offerte en échange par le proposeur (lui appartient)
    public string OfferedAnnonceId { get; set; } = string.Empty;

    // ID du proposeur (celui qui envoie la proposition)
    public string ProposerId { get; set; } = string.Empty;

    // ID du propriétaire de l'annonce cible (destinataire)
    public string OwnerId { get; set; } = string.Empty;

    // Message libre accompagnant la proposition
    public string? Message { get; set; }

    // Statut actuel (stocké en string pour compatibilité Firebase)
    public TradeProposalStatus Statut { get; set; } = TradeProposalStatus.Pending;

    // Timestamps ms pour tri & requêtes Firebase
    public long DateCreationTimestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    public long? DateReponseTimestamp { get; set; }

    // Motif de refus (optionnel)
    public string? MotifRefus { get; set; }

    // ID de la transaction créée si la proposition est acceptée
    public string? TransactionId { get; set; }

    // Numéro du tour de négociation (1-3 max)
    public int RoundNumber { get; set; } = 1;

    // Si cette proposition est une contre-proposition, l'ID de la proposition parente
    public string? ParentProposalId { get; set; }

    // Cache léger des infos d'annonces pour affichage rapide (dénormalisation contrôlée)
    public string? AnnonceCibleTitre { get; set; }
    public string? AnnonceCiblePhoto { get; set; }
    public string? OfferedAnnonceTitre { get; set; }
    public string? OfferedAnnoncePhoto { get; set; }
    public string? ProposerName { get; set; }
    public string? ProposerPhotoUrl { get; set; }

    // ── Propriétés calculées / d'affichage ──

    [Newtonsoft.Json.JsonIgnore]
    public DateTime DateCreation =>
        DateTimeOffset.FromUnixTimeMilliseconds(DateCreationTimestamp).UtcDateTime;

    [Newtonsoft.Json.JsonIgnore]
    public DateTime? DateReponse =>
        DateReponseTimestamp.HasValue
            ? DateTimeOffset.FromUnixTimeMilliseconds(DateReponseTimestamp.Value).UtcDateTime
            : null;

    [Newtonsoft.Json.JsonIgnore]
    public string StatutDisplay => Statut switch
    {
        TradeProposalStatus.Pending => "⏳ En attente",
        TradeProposalStatus.Accepted => "✅ Acceptée",
        TradeProposalStatus.Declined => "❌ Refusée",
        TradeProposalStatus.Cancelled => "🚫 Annulée",
        TradeProposalStatus.CounterOffered => "🔄 Contre-proposition",
        TradeProposalStatus.Expired => "⌛ Expirée",
        _ => "Inconnu"
    };

    [Newtonsoft.Json.JsonIgnore]
    public bool IsPending => Statut == TradeProposalStatus.Pending;

    [Newtonsoft.Json.JsonIgnore]
    public bool CanCounterOffer => Statut == TradeProposalStatus.Pending && RoundNumber < 3;

    [Newtonsoft.Json.JsonIgnore]
    public string DateCreationFormatted => DateCreation.ToLocalTime().ToString("dd MMM yyyy · HH:mm");

    /// <summary>
    /// Retourne true si la proposition a expiré (&gt; 7 jours sans réponse).
    /// </summary>
    public bool IsExpired(int daysBeforeExpiry = 7)
    {
        if (Statut != TradeProposalStatus.Pending) return false;
        return (DateTime.UtcNow - DateCreation).TotalDays > daysBeforeExpiry;
    }
}

