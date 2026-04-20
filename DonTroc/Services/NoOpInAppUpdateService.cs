using System;
using System.Threading.Tasks;

namespace DonTroc.Services;

/// <summary>
/// Implémentation no-op pour les plateformes non-Android (iOS, MacCatalyst, Windows).
/// Le service Android natif est utilisé via PlayInAppUpdateService.
/// </summary>
public sealed class NoOpInAppUpdateService : IInAppUpdateService
{
    public bool IsSupported => false;

    public Task<InAppUpdateResult> TryStartUpdateAsync(InAppUpdateMode mode)
        => Task.FromResult(InAppUpdateResult.NotSupported);

    public Task ResumeIfImmediateUpdatePendingAsync() => Task.CompletedTask;

    public Task CompleteFlexibleUpdateAsync() => Task.CompletedTask;

#pragma warning disable CS0067
    public event Action? FlexibleUpdateDownloaded;
#pragma warning restore CS0067
}

