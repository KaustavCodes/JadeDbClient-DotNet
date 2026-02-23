using JadeDbClient.Interfaces;

namespace JadeDbClient;

/// <summary>
/// Default implementation of <see cref="IJadeDbServiceFactory"/> that resolves named database services.
/// </summary>
public class JadeDbServiceFactory : IJadeDbServiceFactory
{
    private readonly IReadOnlyDictionary<string, IDatabaseService> _services;
    private readonly IDatabaseService? _defaultService;

    public JadeDbServiceFactory(IReadOnlyDictionary<string, IDatabaseService> services, IDatabaseService? defaultService = null)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _defaultService = defaultService;
    }

    /// <inheritdoc/>
    public IDatabaseService GetService(string name)
    {
        if (_services.TryGetValue(name, out var service))
            return service;

        throw new KeyNotFoundException($"No database connection registered with name '{name}'. Registered connections: {string.Join(", ", _services.Keys)}");
    }

    /// <inheritdoc/>
    public IDatabaseService GetService()
    {
        if (_defaultService != null)
            return _defaultService;

        throw new InvalidOperationException(
            "No default database connection has been designated. " +
            "Call SetDefaultConnection(name) on the builder, or set 'JadeDb:DefaultConnection' in configuration.");
    }
}
