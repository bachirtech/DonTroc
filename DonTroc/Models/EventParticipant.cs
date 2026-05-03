using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DonTroc.Models;

/// <summary>
/// Rôle d'un participant à un événement.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum RoleParticipant
{
    Createur,
    Invite,
    Confirme
}

/// <summary>
/// Représente la participation d'un utilisateur à un événement.
/// Stocké dans <c>EventParticipants/{eventId}/{userId}</c>.
/// </summary>
public class EventParticipant
{
    public string EventId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatarUrl { get; set; }

    public RoleParticipant Role { get; set; } = RoleParticipant.Confirme;

    public long DateInscriptionTimestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    /// <summary>IDs des annonces que ce participant ajoute au marché virtuel de l'événement.</summary>
    public List<string> AnnoncesPartagees { get; set; } = new();
}

