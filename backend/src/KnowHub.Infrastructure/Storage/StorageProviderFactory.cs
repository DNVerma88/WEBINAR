using KnowHub.Application.Contracts.Talent;

namespace KnowHub.Infrastructure.Storage;

public sealed class StorageProviderFactory
{
    private readonly IEnumerable<IStorageProvider> _providers;

    public StorageProviderFactory(IEnumerable<IStorageProvider> providers) => _providers = providers;

    public IStorageProvider GetProvider(string providerType) =>
        _providers.FirstOrDefault(p => p.ProviderType == providerType)
        ?? throw new InvalidOperationException($"Storage provider '{providerType}' is not configured.");
}
