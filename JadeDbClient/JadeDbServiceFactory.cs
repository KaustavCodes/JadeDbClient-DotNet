using JadeDbClient.Interfaces;

namespace JadeDbClient;

/// <summary>
/// Default implementation of <see cref="IJadeDbServiceFactory"/> that resolves named database services.
/// </summary>
public class JadeDbServiceFactory : IJadeDbServiceFactory
{
    private readonly IReadOnlyDictionary<string, IDatabaseService> _services;

    public JadeDbServiceFactory(IReadOnlyDictionary<string, IDatabaseService> services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <inheritdoc/>
    public IDatabaseService GetService(string name)
    {
        if (_services.TryGetValue(name, out var service))
            return service;

        throw new KeyNotFoundException($"No database connection registered with name '{name}'. Registered connections: {string.Join(", ", _services.Keys)}");
    }
}
