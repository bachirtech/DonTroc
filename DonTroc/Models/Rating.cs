using System;

namespace DonTroc.Models;

/// <summary>
/// Modèle de données pour les évaluations/notes entre utilisateurs
/// </summary>
public class Rating
{
    /// <summary>
    /// Identifiant unique de l'évaluation
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// ID de la transaction associée à cette évaluation
    /// </summary>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// ID de l'utilisateur qui donne la note
    /// </summary>
    public string EvaluateurId { get; set; } = string.Empty;

    /// <summary>
    /// ID de l'utilisateur qui reçoit la note
    /// </summary>
    public string EvalueId { get; set; } = string.Empty;

    /// <summary>
    /// Note donnée (1 à 5 étoiles)
    /// </summary>
    public int Note { get; set; }

    /// <summary>
    /// Commentaire optionnel sur l'échange
    /// </summary>
    public string? Commentaire { get; set; }

    /// <summary>
    /// Date de création de l'évaluation
    /// </summary>
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Indique si cette évaluation a été modifiée
    /// </summary>
    public bool EstModifiee { get; set; } = false;

    /// <summary>
    /// Date de dernière modification
    /// </summary>
    public DateTime? DateModification { get; set; }

    /// <summary>
    /// Nom de l'évaluateur (pour l'affichage)
    /// </summary>
    public string? NomEvaluateur { get; set; }

    /// <summary>
    /// Photo de profil de l'évaluateur (pour l'affichage)
    /// </summary>
    public string? PhotoEvaluateur { get; set; }
}
