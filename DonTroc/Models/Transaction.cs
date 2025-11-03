using System;

namespace DonTroc.Models;

// Énumération pour les statuts de transaction
public enum StatutTransaction
{
    EnAttente,         // Transaction proposée, en attente de confirmation
    Confirmee,         // Transaction confirmée par les deux parties
    EnCours,           // Transaction en cours de réalisation
    Terminee,          // Transaction terminée avec succès
    Annulee,           // Transaction annulée
    Litigieuse         // Transaction en litige
}

// Énumération pour les types de transaction
public enum TypeTransaction
{
    Don,               // Don simple (pas d'échange)
    Troc               // Échange entre deux objets
}

// Modèle de données pour une transaction
public class Transaction
{
    // Identifiant unique de la transaction
    public string Id { get; set; } = string.Empty;

    // Type de transaction
    public TypeTransaction Type { get; set; }

    // Statut actuel de la transaction
    public StatutTransaction Statut { get; set; } = StatutTransaction.EnAttente;

    // ID de l'annonce principale (celle qui est demandée)
    public string AnnonceId { get; set; } = string.Empty;

    // ID du propriétaire de l'annonce
    public string ProprietaireId { get; set; } = string.Empty;

    // ID du demandeur
    public string DemandeurId { get; set; } = string.Empty;

    // ID de l'annonce proposée en échange (pour les trocs uniquement)
    public string? AnnonceEchangeId { get; set; }

    // Date de création de la transaction
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    // Date de confirmation (quand les deux parties ont accepté)
    public DateTime? DateConfirmation { get; set; }

    // Date de finalisation de la transaction
    public DateTime? DateFinalisation { get; set; }

    // Message du demandeur
    public string? MessageDemandeur { get; set; }

    // Message de réponse du propriétaire
    public string? MessageProprietaire { get; set; }

    // Lieu de rendez-vous convenu
    public string? LieuRendezVous { get; set; }

    // Date et heure du rendez-vous
    public DateTime? DateRendezVous { get; set; }

    // Notes additionnelles
    public string? Notes { get; set; }

    // Évaluation du propriétaire (1-5 étoiles)
    public int? EvaluationProprietaire { get; set; }

    // Évaluation du demandeur (1-5 étoiles)
    public int? EvaluationDemandeur { get; set; }

    // Commentaire d'évaluation du propriétaire
    public string? CommentaireProprietaire { get; set; }

    // Commentaire d'évaluation du demandeur
    public string? CommentaireDemandeur { get; set; }

    // Propriétés calculées pour l'affichage
    public string StatutDisplay => Statut switch
    {
        StatutTransaction.EnAttente => "En attente",
        StatutTransaction.Confirmee => "Confirmée",
        StatutTransaction.EnCours => "En cours",
        StatutTransaction.Terminee => "Terminée",
        StatutTransaction.Annulee => "Annulée",
        StatutTransaction.Litigieuse => "Litigieuse",
        _ => "Inconnu"
    };

    public string TypeDisplay => Type switch
    {
        TypeTransaction.Don => "Don",
        TypeTransaction.Troc => "Troc",
        _ => "Inconnu"
    };

    // Propriétés pour l'évaluation
    /// <summary>
    /// Indique si cette transaction peut être évaluée par l'utilisateur actuel
    /// </summary>
    public bool PeutEtreEvaluee { get; set; }

    /// <summary>
    /// Indique si cette transaction a déjà été évaluée par l'utilisateur actuel
    /// </summary>
    public bool DejaEvaluee { get; set; }

    public bool PeutEtreConfirmee => Statut == StatutTransaction.EnAttente;
    public bool PeutEtreAnnulee => Statut is StatutTransaction.EnAttente or StatutTransaction.Confirmee;
    public bool PeutEtreFinalisee => Statut == StatutTransaction.EnCours;
    public bool EstTerminee => Statut is StatutTransaction.Terminee or StatutTransaction.Annulee;
}
