namespace DonTroc.Services;

/// <summary>
/// Implémentation par défaut (iOS, Windows, MacCatalyst) — autorise toujours les pubs.
/// Le UMP officiel n'a pas (encore) de binding C# stable pour iOS dans ce projet.
/// </summary>
public sealed class NoOpConsentService : IConsentService
{
    public Task<bool> GatherConsentAsync() => Task.FromResult(true);
    public bool CanRequestAds() => true;
    public bool IsPrivacyOptionsRequired() => false;
    public Task ShowPrivacyOptionsFormAsync() => Task.CompletedTask;
    public void ResetConsent() { }
}

